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
            _pipelineServerUrl = configuration["PipelineServer:BaseUrl"] ?? "http://localhost:8000";
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

                // Get available tags from database
                var availableTags = await _context.Tags
                    .Where(t => !(t is UserCreatedTag))
                    .ToListAsync();

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
        /// </summary>
        private async Task<List<MailTag>> MapLabelsToTags(List<string> labels, List<Tag> availableTags, MailReceived email)
        {
            var mailTags = new List<MailTag>();

            if (labels == null || !labels.Any())
                return mailTags;

            // Create a mapping from pipeline labels to database tag names
            var labelMapping = new Dictionary<string, string>
            {
                { "work", "Work" },
                { "personal", "Personal" },
                { "finance", "Finance" },
                { "health", "Health" },
                { "school", "School" },
                { "news", "News" },
                { "discounts", "Discounts" },
                { "social network", "Social network" },
                { "family and friends", "Family & friends" },
                { "spam", "Spam" } // Handle spam classification
            };

            foreach (var label in labels) // Apply all predicted tags
            {
                if (labelMapping.TryGetValue(label.ToLowerInvariant(), out var tagName))
                {
                    var tag = availableTags.FirstOrDefault(t => t.TagName.Equals(tagName, StringComparison.OrdinalIgnoreCase));
                    
                    if (tag != null)
                    {
                        mailTags.Add(new MailTag
                        {
                            MailReceived = email,
                            Tag = tag,
                            TagId = tag.Id,
                            MailReceivedId = email.MailId
                        });
                    }
                    else if (label.ToLowerInvariant() == "spam")
                    {
                        // Handle spam emails by marking them as spam
                        email.IsSpam = true;
                        _logger.LogInformation($"Email '{email.Subject}' marked as spam by AI");
                    }
                }
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