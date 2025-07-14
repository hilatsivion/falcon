using Microsoft.Graph;
using Microsoft.Extensions.Configuration;
using FalconBackend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Abstractions;
using Microsoft.Graph.Models;

namespace FalconBackend.Services
{
    public class OutlookService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OutlookService> _logger;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _tenantId;
        private readonly string _graphApiUrl;

        public OutlookService(IConfiguration configuration, ILogger<OutlookService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _clientId = _configuration["MicrosoftGraph:ClientId"] ?? throw new ArgumentNullException("MicrosoftGraph:ClientId not found in configuration");
            _clientSecret = _configuration["MicrosoftGraph:ClientSecret"] ?? throw new ArgumentNullException("MicrosoftGraph:ClientSecret not found in configuration");
            _tenantId = _configuration["MicrosoftGraph:TenantId"] ?? "common";
            _graphApiUrl = _configuration["MicrosoftGraph:GraphApiUrl"] ?? "https://graph.microsoft.com/";
        }

        /// <summary>
        /// Creates a Graph service client using the stored access token from MailAccount
        /// </summary>
        private GraphServiceClient CreateGraphServiceClient(string accessToken)
        {
            var authenticationProvider = new BaseBearerTokenAuthenticationProvider(new AccessTokenProvider(accessToken));
            return new GraphServiceClient(authenticationProvider);
        }

        /// <summary>
        /// Access token provider for Microsoft Graph
        /// </summary>
        private class AccessTokenProvider : IAccessTokenProvider
        {
            private readonly string _accessToken;

            public AccessTokenProvider(string accessToken)
            {
                _accessToken = accessToken;
            }

            public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_accessToken);
            }

            public AllowedHostsValidator AllowedHostsValidator => new AllowedHostsValidator();
        }

        /// <summary>
        /// Fetches emails from user's Outlook inbox
        /// </summary>
        public async Task<List<MailReceived>> GetUserEmailsAsync(string accessToken, string mailAccountId, int maxEmails = 50)
        {
            try
            {
                _logger.LogInformation($"Fetching emails for account {mailAccountId}");
                
                var graphServiceClient = CreateGraphServiceClient(accessToken);
                
                // Get messages from the user's mailbox using new v5 API
                var messages = await graphServiceClient.Me.Messages.GetAsync((requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Top = maxEmails;
                    requestConfiguration.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
                });

                var emailList = new List<MailReceived>();

                if (messages?.Value != null)
                {
                    foreach (var message in messages.Value)
                    {
                        try
                        {
                            var mailReceived = ConvertToMailReceived(message, mailAccountId);
                            emailList.Add(mailReceived);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Failed to convert message {message.Id}: {ex.Message}");
                            continue; // Skip this email and continue with others
                        }
                    }
                }

                _logger.LogInformation($"Successfully fetched {emailList.Count} emails for account {mailAccountId}");
                return emailList;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching emails for account {mailAccountId}: {ex.Message}");
                throw new Exception($"Failed to fetch emails from Outlook: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Converts Microsoft Graph Message to MailReceived entity
        /// </summary>
        private MailReceived ConvertToMailReceived(Message message, string mailAccountId)
        {
            var mailReceived = new MailReceived
            {
                MailAccountId = mailAccountId,
                Subject = message.Subject ?? "No Subject",
                Body = GetEmailBody(message),
                Sender = GetSenderString(message),
                TimeReceived = message.ReceivedDateTime?.DateTime ?? DateTime.UtcNow,
                IsRead = message.IsRead ?? false,
                IsFavorite = message.Flag?.FlagStatus == Microsoft.Graph.Models.FollowupFlagStatus.Flagged,
                Recipients = GetRecipients(message),
                Attachments = new List<Attachments>(), // TODO: Implement attachment handling if needed
                MailTags = new List<MailTag>() // Tags can be assigned later based on content analysis
            };

            return mailReceived;
        }

        /// <summary>
        /// Extracts email body content, preferring HTML over text
        /// </summary>
        private string GetEmailBody(Message message)
        {
            if (message.Body?.Content != null)
            {
                return message.Body.Content;
            }
            
            if (!string.IsNullOrEmpty(message.BodyPreview))
            {
                return message.BodyPreview;
            }

            return "No body content available";
        }

        /// <summary>
        /// Formats sender information as a string
        /// </summary>
        private string GetSenderString(Message message)
        {
            if (message.From?.EmailAddress != null)
            {
                var senderName = message.From.EmailAddress.Name;
                var senderEmail = message.From.EmailAddress.Address;
                
                if (!string.IsNullOrEmpty(senderName) && !string.IsNullOrEmpty(senderEmail))
                {
                    return $"{senderName} <{senderEmail}>";
                }
                else if (!string.IsNullOrEmpty(senderEmail))
                {
                    return senderEmail;
                }
            }

            return "Unknown Sender";
        }

        /// <summary>
        /// Converts Graph message recipients to Recipient entities
        /// </summary>
        private List<FalconBackend.Models.Recipient> GetRecipients(Message message)
        {
            var recipients = new List<FalconBackend.Models.Recipient>();

            // Add To recipients
            if (message.ToRecipients != null)
            {
                foreach (var recipient in message.ToRecipients)
                {
                    if (recipient.EmailAddress?.Address != null)
                    {
                        recipients.Add(new FalconBackend.Models.Recipient { Email = recipient.EmailAddress.Address });
                    }
                }
            }

            // Add CC recipients
            if (message.CcRecipients != null)
            {
                foreach (var recipient in message.CcRecipients)
                {
                    if (recipient.EmailAddress?.Address != null)
                    {
                        recipients.Add(new FalconBackend.Models.Recipient { Email = recipient.EmailAddress.Address });
                    }
                }
            }

            return recipients;
        }

        /// <summary>
        /// Validates if the access token is still valid by making a test call
        /// </summary>
        public async Task<bool> ValidateAccessTokenAsync(string accessToken)
        {
            try
            {
                var graphServiceClient = CreateGraphServiceClient(accessToken);
                
                // Try to get user profile as a test using new v5 API
                var user = await graphServiceClient.Me.GetAsync();
                return user != null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Access token validation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sends an email via Microsoft Graph API
        /// </summary>
        public async Task<bool> SendEmailAsync(string accessToken, SendMailRequest request)
        {
            try
            {
                _logger.LogInformation($"Sending email via Outlook: {request.Subject}");
                
                var graphServiceClient = CreateGraphServiceClient(accessToken);

                // Create the message
                var message = new Microsoft.Graph.Models.Message
                {
                    Subject = request.Subject,
                    Body = new Microsoft.Graph.Models.ItemBody
                    {
                        ContentType = Microsoft.Graph.Models.BodyType.Html,
                        Content = request.Body
                    },
                    ToRecipients = request.Recipients.Select(email => new Microsoft.Graph.Models.Recipient
                    {
                        EmailAddress = new Microsoft.Graph.Models.EmailAddress
                        {
                            Address = email,
                            Name = email // Use email as name if no display name provided
                        }
                    }).ToList()
                };

                // Send the message
                await graphServiceClient.Me.SendMail.PostAsync(new Microsoft.Graph.Me.SendMail.SendMailPostRequestBody
                {
                    Message = message,
                    SaveToSentItems = true
                });

                _logger.LogInformation($"Email sent successfully via Outlook API");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send email via Outlook: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sends an email via Microsoft Graph API with detailed recipient options
        /// </summary>
        public async Task<bool> SendEmailWithDetailsAsync(string accessToken, string subject, string body, 
            List<string> toRecipients, List<string>? ccRecipients = null, List<string>? bccRecipients = null)
        {
            try
            {
                _logger.LogInformation($"Sending detailed email via Outlook: {subject}");
                
                var graphServiceClient = CreateGraphServiceClient(accessToken);

                var message = new Microsoft.Graph.Models.Message
                {
                    Subject = subject,
                    Body = new Microsoft.Graph.Models.ItemBody
                    {
                        ContentType = Microsoft.Graph.Models.BodyType.Html,
                        Content = body
                    },
                    ToRecipients = toRecipients?.Select(email => new Microsoft.Graph.Models.Recipient
                    {
                        EmailAddress = new Microsoft.Graph.Models.EmailAddress
                        {
                            Address = email,
                            Name = email
                        }
                    }).ToList(),
                    CcRecipients = ccRecipients?.Select(email => new Microsoft.Graph.Models.Recipient
                    {
                        EmailAddress = new Microsoft.Graph.Models.EmailAddress
                        {
                            Address = email,
                            Name = email
                        }
                    }).ToList(),
                    BccRecipients = bccRecipients?.Select(email => new Microsoft.Graph.Models.Recipient
                    {
                        EmailAddress = new Microsoft.Graph.Models.EmailAddress
                        {
                            Address = email,
                            Name = email
                        }
                    }).ToList()
                };

                await graphServiceClient.Me.SendMail.PostAsync(new Microsoft.Graph.Me.SendMail.SendMailPostRequestBody
                {
                    Message = message,
                    SaveToSentItems = true
                });

                _logger.LogInformation($"Detailed email sent successfully via Outlook API");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send detailed email via Outlook: {ex.Message}");
                return false;
            }
        }
    }
} 