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

                // Prepare payload for pipeline server
                var messages = emails.Select((email, index) => new PipelineMessage
                {
                    Id = index,
                    Content = email.Body
                }).ToList();

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
                    _logger.LogDebug($"Pipeline result: Id={result.Id}, Labels=[{string.Join(", ", result.Labels)}]");
                }

                // Get available tags from database
                var availableTags = await _context.Tags
                    .Where(t => !(t is UserCreatedTag))
                    .ToListAsync();
                
                _logger.LogInformation($"Found {availableTags.Count} system tags in database");
                foreach (var tag in availableTags)
                {
                    _logger.LogDebug($"Available tag: Id={tag.Id}, Name={tag.TagName}");
                }

                // Create MailTag entities based on AI predictions
                var mailTags = new List<MailTag>();
                foreach (var result in results)
                {
                    if (result.Id >= 0 && result.Id < emails.Count)
                    {
                        var email = emails[result.Id];
                        var aiTags = await MapLabelsToTags(result.Labels, availableTags, email);
                        mailTags.AddRange(aiTags);
                    }
                }

                _logger.LogInformation($"Successfully classified emails and created {mailTags.Count} AI-generated tags");
                
                // Debug: Log details about created tags
                foreach (var mailTag in mailTags)
                {
                    _logger.LogDebug($"Created MailTag: EmailId={mailTag.MailReceivedId}, TagId={mailTag.TagId}, TagName={mailTag.Tag?.TagName}");
                }
                
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
                return mailTags;

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
                { "spam", "Spam" } // Include "spam" in the mapping
            };

            foreach (var label in labels)
            {
                if (labelMapping.TryGetValue(label.ToLowerInvariant(), out var tagName))
                {
                    if (tagName.Equals("Spam", StringComparison.OrdinalIgnoreCase))
                    {
                        // If the label is "spam", mark the email as spam and skip saving the tag
                        isSpam = true;
                        continue;
                    }

                    // Check if the tag exists in the database
                    var tag = availableTags.FirstOrDefault(t => t.TagName.Equals(tagName, StringComparison.OrdinalIgnoreCase));

                    if (tag == null)
                    {
                        // If the tag does not exist, create it
                        tag = new Tag
                        {
                            TagName = tagName
                        };

                        _context.Tags.Add(tag);
                        // Don't save immediately - we'll save all changes at the end
                        _logger.LogInformation($"Created new system tag: {tagName}");
                    }

                    // Check if MailTag already exists to prevent duplicates
                    var existingMailTag = await _context.MailTags
                        .FirstOrDefaultAsync(mt => mt.MailReceivedId == email.MailId && mt.TagId == tag.Id);

                    if (existingMailTag == null)
                    {
                        // Create the MailTag relationship only if it doesn't exist
                        var mailTag = new MailTag
                        {
                            MailReceivedId = email.MailId,
                            TagId = tag.Id
                        };

                        _context.MailTags.Add(mailTag);
                        mailTags.Add(mailTag);
                        _logger.LogDebug($"Created MailTag: EmailId={email.MailId}, TagId={tag.Id}, TagName={tagName}");
                    }
                    else
                    {
                        _logger.LogDebug($"MailTag already exists: EmailId={email.MailId}, TagId={tag.Id}, TagName={tagName}");
                    }
                }
            }

            // If the email is marked as spam, update its IsSpam property
            if (isSpam)
            {
                email.IsSpam = true;
                // Don't call Update() if the entity is already tracked
                _logger.LogInformation($"Email {email.MailId} marked as spam.");
            }

            // Save all changes at once
            await _context.SaveChangesAsync();
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
