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

        public AiTaggingService(HttpClient httpClient, AppDbContext context, ILogger<AiTaggingService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pipelineServerUrl = configuration["PipelineServer:BaseUrl"] ?? "https://localhost:8000";
        }

        /// <summary>
        /// Classifies emails using the AI pipeline server and assigns appropriate tags
        /// </summary>
        public async Task<List<MailTag>> GetAiTagsAsync(List<MailReceived> emails)
        {
            try
            {
                if (!emails.Any())
                {
                    return new List<MailTag>();
                }

                _logger.LogInformation($"Classifying {emails.Count} emails using AI pipeline");

                // Prepare payload for pipeline server - use actual MailId instead of array index
                var messages = emails.Select(email => new PipelineMessage
                {
                    Id = email.MailId, // ✅ Use actual database MailId
                    Content = email.Body ?? string.Empty
                }).ToList();

                _logger.LogDebug($"Sending emails to AI pipeline: {string.Join(", ", messages.Select(m => $"Id={m.Id}"))}");

                var payload = new PipelineBatchRequest { Messages = messages };
                var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                // Call pipeline server
                var response = await _httpClient.PostAsync(
                    $"{_pipelineServerUrl}/classify",
                    new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json")
                );

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Pipeline server returned error {response.StatusCode}: {errorContent}");
                    return new List<MailTag>();
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var results = JsonSerializer.Deserialize<List<PipelineResult>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (results == null || !results.Any())
                {
                    _logger.LogWarning("Pipeline server returned no results");
                    return new List<MailTag>();
                }
                
                _logger.LogInformation($"Pipeline server returned {results.Count} results");
                foreach (var result in results)
                {
                    _logger.LogDebug($"Pipeline result: MailId={result.Id}, Labels=[{string.Join(", ", result.Labels)}]");
                }

                // Get available tags from database
                var availableTags = await _context.Tags
                    .Where(t => !(t is UserCreatedTag))
                    .ToListAsync();
                
                _logger.LogInformation($"Found {availableTags.Count} system tags in database");

                // Create a dictionary for faster email lookup by MailId
                var emailDict = emails.ToDictionary(e => e.MailId, e => e);

                // Create MailTag entities based on AI predictions
                var mailTags = new List<MailTag>();
                foreach (var result in results)
                {
                    if (emailDict.TryGetValue(result.Id, out var email))
                    {
                        _logger.LogDebug($"Processing AI result for email MailId={result.Id}");
                        var aiTags = await MapLabelsToTags(result.Labels, availableTags, email);
                        mailTags.AddRange(aiTags);
                    }
                    else
                    {
                        _logger.LogWarning($"Could not find email with MailId={result.Id} in provided emails list");
                    }
                }

                _logger.LogInformation($"Successfully classified emails and created {mailTags.Count} AI-generated tags");
                
                return mailTags;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during AI tagging: {ex.Message}");
                return new List<MailTag>();
            }
        }

        /// <summary>
        /// Maps pipeline server labels to database tags and creates MailTag entities
        /// Creates missing tags in database if they don't exist
        /// </summary>
        private async Task<List<MailTag>> MapLabelsToTags(List<string> labels, List<Tag> availableTags, MailReceived email)
        {
            var mailTags = new List<MailTag>();
            bool isSpam = false;

            if (labels == null || !labels.Any())
            {
                _logger.LogDebug($"No labels provided for email MailId={email.MailId}");
                return mailTags;
            }

            _logger.LogDebug($"Processing {labels.Count} labels for email MailId={email.MailId}: [{string.Join(", ", labels)}]");

            // Create a mapping from pipeline labels to database tag names
            var labelMapping = new Dictionary<string, string>
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

            foreach (var label in labels)
            {
                var normalizedLabel = label.ToLowerInvariant();
                _logger.LogDebug($"Processing label: '{label}' (normalized: '{normalizedLabel}') for email MailId={email.MailId}");
                
                if (labelMapping.TryGetValue(normalizedLabel, out var tagName))
                {
                    if (tagName.Equals("Spam", StringComparison.OrdinalIgnoreCase))
                    {
                        // Mark email as spam but don't create a tag for it
                        isSpam = true;
                        _logger.LogInformation($"Email MailId={email.MailId} detected as spam by AI");
                        continue;
                    }

                    // Find or create the tag
                    var tag = availableTags.FirstOrDefault(t => t.TagName.Equals(tagName, StringComparison.OrdinalIgnoreCase));

                    if (tag == null)
                    {
                        // Create new tag
                        tag = new Tag { TagName = tagName };
                        _context.Tags.Add(tag);
                        
                        // Save immediately to get the ID
                        await _context.SaveChangesAsync();
                        
                        // Add to available tags to prevent duplicate creation
                        availableTags.Add(tag);
                        
                        _logger.LogInformation($"Created new system tag: '{tagName}' with Id={tag.Id}");
                    }

                    // Check if MailTag already exists to prevent duplicates
                    var existingMailTag = await _context.MailTags
                        .FirstOrDefaultAsync(mt => mt.MailReceivedId == email.MailId && mt.TagId == tag.Id);

                    if (existingMailTag == null)
                    {
                        // Create new MailTag
                        var mailTag = new MailTag
                        {
                            MailReceivedId = email.MailId,
                            TagId = tag.Id
                        };

                        _context.MailTags.Add(mailTag);
                        mailTags.Add(mailTag);
                        
                        _logger.LogInformation($"Created MailTag: EmailId={email.MailId}, TagId={tag.Id}, TagName='{tagName}'");
                    }
                    else
                    {
                        _logger.LogDebug($"MailTag already exists: EmailId={email.MailId}, TagId={tag.Id}, TagName='{tagName}'");
                    }
                }
                else
                {
                    _logger.LogWarning($"Unknown label '{label}' received from AI pipeline for email MailId={email.MailId}");
                }
            }

            // Handle spam detection
            if (isSpam)
            {
                email.IsSpam = true;
                _context.MailReceived.Update(email);
                _logger.LogInformation($"Email MailId={email.MailId} marked as spam in database");
            }

            // Save all changes
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Successfully saved {mailTags.Count} MailTags and spam status for email MailId={email.MailId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save MailTags for email MailId={email.MailId}: {ex.Message}");
                throw;
            }

            return mailTags;
        }

        /// <summary>
        /// Classifies a single email for immediate tagging
        /// </summary>
        public async Task<List<MailTag>> GetAiTagsForSingleEmailAsync(MailReceived email)
        {
            return await GetAiTagsAsync(new List<MailReceived> { email });
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
