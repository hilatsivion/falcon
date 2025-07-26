using FalconBackend.Data;
using FalconBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FalconBackend.Services
{
    public class AiTaggingService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;
        private readonly ILogger<AiTaggingService> _logger;
        private readonly string _pipelineServerUrl;

        // Extract label mapping to avoid duplication and recreation
        private static readonly Dictionary<string, string> LabelMapping = new()
        {
            { "work", "Work" },
            { "school", "School" },
            { "social network", "Social network" },
            { "news", "News" },
            { "discounts", "Discounts" },
            { "finance", "Finance" },
            { "family and friends", "Family & friends" },
            { "personal", "Personal" },
            { "health", "Health" },
            { "spam", "Spam" }
        };

        public AiTaggingService(HttpClient httpClient, AppDbContext context, ILogger<AiTaggingService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _httpClient.Timeout = Timeout.InfiniteTimeSpan; // Disable timeout
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pipelineServerUrl = configuration["PipelineServer:BaseUrl"] ?? "https://localhost:8000";
        }

        /// <summary>
        /// Classifies emails using the AI pipeline server and assigns appropriate tags
        /// </summary>
        public async Task<List<MailTag>> GetAiTagsAsync(List<MailReceived> emails, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!emails.Any())
                {
                    _logger.LogInformation("No emails provided for AI classification");
                    return new List<MailTag>();
                }

                _logger.LogInformation($"🚀 Starting AI classification for {emails.Count} emails");

                // Validate email IDs before sending to AI
                var invalidEmails = emails.Where(e => e.MailId <= 0).ToList();
                if (invalidEmails.Any())
                {
                    _logger.LogError($"❌ Found {invalidEmails.Count} emails with invalid MailIds: [{string.Join(", ", invalidEmails.Select(e => e.MailId))}]");
                    throw new InvalidOperationException($"Cannot process emails with invalid MailIds. Found {invalidEmails.Count} emails with MailId <= 0");
                }

                // Prepare payload for pipeline server - use actual MailId instead of array index
                var messages = emails.Select(email => new PipelineMessage
                {
                    Id = email.MailId, // ✅ Use actual database MailId
                    Content = email.Body ?? string.Empty
                }).ToList();

                _logger.LogInformation($"📤 Sending emails to AI pipeline: {string.Join(", ", messages.Select(m => $"Id={m.Id}"))}");

                var payload = new PipelineBatchRequest { Messages = messages };
                var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogDebug($"📝 JSON payload: {jsonPayload}");

                // Call pipeline server
                var response = await _httpClient.PostAsync(
                    $"{_pipelineServerUrl}/classify",
                    new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json")
                );

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Pipeline server returned error {response.StatusCode}: {errorContent}");
                    return new List<MailTag>();
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug($"📥 AI server response: {responseContent}");

                var results = JsonSerializer.Deserialize<List<PipelineResult>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (results == null || !results.Any())
                {
                    _logger.LogWarning("⚠️ Pipeline server returned no results");
                    return new List<MailTag>();
                }
                
                _logger.LogInformation($"📊 Pipeline server returned {results.Count} results");
                foreach (var result in results)
                {
                    _logger.LogInformation($"🏷️ AI Result: MailId={result.Id}, Labels=[{string.Join(", ", result.Labels)}]");
                }

                // Get available tags from database
                var availableTags = await _context.Tags
                    .Where(t => !(t is UserCreatedTag))
                    .ToListAsync(cancellationToken);
                
                _logger.LogInformation($"📋 Found {availableTags.Count} system tags in database: [{string.Join(", ", availableTags.Select(t => $"{t.Id}:{t.TagName}"))}]");

                // Create a dictionary for faster email lookup by MailId
                var emailDict = emails.ToDictionary(e => e.MailId, e => e);

                // Process all results in batch - NO individual saves
                var allMailTags = await ProcessAllTagsInBatch(results, emailDict, availableTags, cancellationToken);

                _logger.LogInformation($"🎉 AI classification completed! Total: {allMailTags.Count} MailTags created across {results.Count} emails");
                
                return allMailTags;
            }
            catch (Exception ex)
            {
                _logger.LogError($"💥 Error during AI tagging: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"💥 Inner exception: {ex.InnerException.Message}");
                }
                _logger.LogError($"💥 Stack trace: {ex.StackTrace}");
                return new List<MailTag>();
            }
        }

        /// <summary>
        /// Process all AI results in a single batch operation
        /// </summary>
        private async Task<List<MailTag>> ProcessAllTagsInBatch(List<PipelineResult> results, Dictionary<int, MailReceived> emailDict, List<Tag> availableTags, CancellationToken cancellationToken)
        {
            var allMailTags = new List<MailTag>();
            var emailsToUpdate = new List<MailReceived>();
            var newTagsNeeded = new HashSet<string>();
            
            _logger.LogInformation($"🚀 Starting batch processing for {results.Count} AI results");
            
            // First pass: Collect all needed tags and identify spam emails
            foreach (var result in results)
            {
                if (!emailDict.TryGetValue(result.Id, out var email))
                {
                    _logger.LogError($"❌ Could not find email with MailId={result.Id} in provided emails list");
                    continue;
                }

                _logger.LogInformation($"🔄 Processing AI result for email MailId={result.Id}");

                bool isSpam = false;
                foreach (var label in result.Labels ?? new List<string>())
                {
                    var normalizedLabel = label.ToLowerInvariant();
                    
                    if (LabelMapping.TryGetValue(normalizedLabel, out var tagName))
                    {
                        if (tagName.Equals("Spam", StringComparison.OrdinalIgnoreCase))
                        {
                            isSpam = true;
                            _logger.LogInformation($"Email MailId={email.MailId} detected as spam by AI");
                        }
                        else
                        {
                            // Check if tag exists, if not add to needed tags
                            if (!availableTags.Any(t => t.TagName.Equals(tagName, StringComparison.OrdinalIgnoreCase)))
                            {
                                newTagsNeeded.Add(tagName);
                            }
                        }
                    }
                }

                if (isSpam)
                {
                    email.IsSpam = true;
                    emailsToUpdate.Add(email);
                }
            }

            // Create any new tags needed
            var newTags = new List<Tag>();
            foreach (var tagName in newTagsNeeded)
            {
                var newTag = new Tag { TagName = tagName };
                newTags.Add(newTag);
                availableTags.Add(newTag);
                _logger.LogInformation($"Queued new system tag for creation: '{tagName}'");
            }

            // Save new tags first if any - use AddRange for better performance
            if (newTags.Any())
            {
                _context.Tags.AddRange(newTags);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation($"✅ Created {newTags.Count} new tags");
            }

            // Get existing MailTags to avoid duplicates
            var emailIds = emailDict.Keys.ToList();
            var existingMailTags = await _context.MailTags
                .Where(mt => emailIds.Contains(mt.MailReceivedId))
                .ToListAsync(cancellationToken);

            var existingTagPairs = new HashSet<(int EmailId, int TagId)>(
                existingMailTags.Select(mt => (mt.MailReceivedId, mt.TagId))
            );

            // Second pass: Create MailTag entities
            var newMailTags = new List<MailTag>();
            foreach (var result in results)
            {
                if (!emailDict.TryGetValue(result.Id, out var email))
                    continue;

                foreach (var label in result.Labels ?? new List<string>())
                {
                    var normalizedLabel = label.ToLowerInvariant();
                    
                    if (LabelMapping.TryGetValue(normalizedLabel, out var tagName) && 
                        !tagName.Equals("Spam", StringComparison.OrdinalIgnoreCase))
                    {
                        var tag = availableTags.FirstOrDefault(t => t.TagName.Equals(tagName, StringComparison.OrdinalIgnoreCase));
                        if (tag != null && !existingTagPairs.Contains((email.MailId, tag.Id)))
                        {
                            var mailTag = new MailTag
                            {
                                MailReceivedId = email.MailId,
                                TagId = tag.Id
                                // DO NOT set Id property - it conflicts with composite key
                            };

                            newMailTags.Add(mailTag);
                            allMailTags.Add(mailTag);
                            _logger.LogInformation($"Queued MailTag: EmailId={email.MailId}, TagId={tag.Id}, TagName='{tagName}'");
                        }
                    }
                }
            }

            // Add all MailTags at once for better performance
            if (newMailTags.Any())
            {
                _logger.LogInformation($"🏷️ About to add {newMailTags.Count} MailTags to context");
                
                // Debug: Log each MailTag being added
                foreach (var mailTag in newMailTags)
                {
                    _logger.LogDebug($"Adding MailTag: MailReceivedId={mailTag.MailReceivedId}, TagId={mailTag.TagId}");
                }
                
                _context.MailTags.AddRange(newMailTags);
                
                // Verify they were added to change tracker
                var addedEntries = _context.ChangeTracker.Entries<MailTag>()
                    .Where(e => e.State == EntityState.Added)
                    .ToList();
                _logger.LogInformation($"✅ {addedEntries.Count} MailTags are now in Added state");
            }
            else
            {
                _logger.LogWarning("⚠️ No new MailTags to add");
            }

            // Update spam emails with proper entity tracking
            foreach (var email in emailsToUpdate)
            {
                // Use proper EF tracking - only mark specific property as modified
                var entry = _context.Entry(email);
                if (entry.State == EntityState.Detached)
                {
                    _context.Attach(email);
                }
                entry.Property(e => e.IsSpam).IsModified = true;
                _logger.LogDebug($"Marked email {email.MailId} IsSpam property as modified");
            }

            // Single save operation for everything with explicit transaction
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Debug: Check what's being tracked before save
                var trackedEntries = _context.ChangeTracker.Entries().ToList();
                _logger.LogInformation($"📊 About to save changes. Tracked entities: {trackedEntries.Count}");
                
                foreach (var entry in trackedEntries)
                {
                    _logger.LogDebug($"Tracked: {entry.Entity.GetType().Name} - State: {entry.State}");
                }

                var changeCount = await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                
                _logger.LogInformation($"✅ Successfully saved {changeCount} database changes - {allMailTags.Count} MailTags and {emailsToUpdate.Count} spam updates");
                
                // Debug: Verify MailTags were actually saved
                if (newMailTags.Any())
                {
                    var firstMailTag = newMailTags.First();
                    var savedMailTag = await _context.MailTags
                        .FirstOrDefaultAsync(mt => mt.MailReceivedId == firstMailTag.MailReceivedId && mt.TagId == firstMailTag.TagId, cancellationToken);
                    
                    if (savedMailTag != null)
                    {
                        _logger.LogInformation($"✅ Verification: MailTag was saved successfully");
                    }
                    else
                    {
                        _logger.LogError($"❌ Verification FAILED: MailTag was NOT saved to database");
                    }
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError($"❌ Failed to save batch changes: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"❌ Inner exception: {ex.InnerException.Message}");
                }
                
                // Debug: Log validation errors if any
                if (ex is Microsoft.EntityFrameworkCore.DbUpdateException)
                {
                    _logger.LogError($"❌ DbUpdateException details: {ex}");
                }
                
                throw;
            }

            return allMailTags;
        }

        /// <summary>
        /// Classifies a single email for immediate tagging
        /// </summary>
        public async Task<List<MailTag>> GetAiTagsForSingleEmailAsync(MailReceived email, CancellationToken cancellationToken = default)
        {
            return await GetAiTagsAsync(new List<MailReceived> { email }, cancellationToken);
        }

        /// <summary>
        /// Checks if the pipeline server is available
        /// </summary>
        public async Task<bool> IsServiceAvailableAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_pipelineServerUrl}/docs");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Pipeline server not available: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test method to verify MailTag creation works
        /// </summary>
        public async Task<bool> TestMailTagCreationAsync(int emailId, int tagId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"🧪 Testing MailTag creation for EmailId={emailId}, TagId={tagId}");
                
                // Check if it already exists
                var exists = await _context.MailTags
                    .AnyAsync(mt => mt.MailReceivedId == emailId && mt.TagId == tagId, cancellationToken);
                
                if (exists)
                {
                    _logger.LogInformation($"✅ MailTag already exists");
                    return true;
                }
                
                var testMailTag = new MailTag
                {
                    MailReceivedId = emailId,
                    TagId = tagId
                };
                
                _context.MailTags.Add(testMailTag);
                var changes = await _context.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation($"✅ Test MailTag created successfully. Changes: {changes}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Test MailTag creation failed: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"❌ Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }
    }

    // DTOs for pipeline server communication
    public class PipelineMessage
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    public class PipelineBatchRequest
    {
        public List<PipelineMessage> Messages { get; set; } = new();
    }

    public class PipelineResult
    {
        public int Id { get; set; }
        public List<string> Labels { get; set; } = new();
    }
} 
