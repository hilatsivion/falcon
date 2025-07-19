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
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace FalconBackend.Services
{
    public class OutlookService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OutlookService> _logger;
        private readonly AiTaggingService _aiTaggingService;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _tenantId;
        private readonly string _graphApiUrl;

        public OutlookService(IConfiguration configuration, ILogger<OutlookService> logger, AiTaggingService aiTaggingService)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aiTaggingService = aiTaggingService ?? throw new ArgumentNullException(nameof(aiTaggingService));
            
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
            var accessTokenProvider = new AccessTokenProvider(accessToken);
            var authenticationProvider = new BaseBearerTokenAuthenticationProvider(accessTokenProvider);
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

                // Apply AI-powered tagging to the emails
                if (emailList.Any())
                {
                    try
                    {
                        _logger.LogInformation($"Applying AI tagging to {emailList.Count} emails");
                        var aiTags = await _aiTaggingService.GetAiTagsAsync(emailList);
                        
                        // Apply the AI tags to the emails
                        foreach (var aiTag in aiTags)
                        {
                            var email = emailList.FirstOrDefault(e => e.MailId == aiTag.MailReceivedId);
                            if (email != null)
                            {
                                email.MailTags.Add(aiTag);
                            }
                        }
                        
                        _logger.LogInformation($"Successfully applied {aiTags.Count} AI-generated tags");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"AI tagging failed, continuing without AI tags: {ex.Message}");
                        // Continue without AI tags if the service fails
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

        /// <summary>
        /// Refreshes an expired access token using the refresh token
        /// Returns new token response with updated access and refresh tokens
        /// </summary>
        public async Task<TokenResponse?> RefreshAccessTokenAsync(string refreshToken)
        {
            try
            {
                _logger.LogInformation("Attempting to refresh access token");

                using var httpClient = new HttpClient();
                var tokenEndpoint = $"{_configuration["MicrosoftGraph:Instance"]}common/oauth2/v2.0/token";

                var tokenRequestData = new List<KeyValuePair<string, string>>
                {
                    new("client_id", _clientId),
                    new("client_secret", _clientSecret),
                    new("grant_type", "refresh_token"),
                    new("refresh_token", refreshToken),
                    new("scope", "https://graph.microsoft.com/Mail.Read https://graph.microsoft.com/Mail.Send")
                };

                var tokenRequest = new FormUrlEncodedContent(tokenRequestData);
                var tokenResponse = await httpClient.PostAsync(tokenEndpoint, tokenRequest);

                if (!tokenResponse.IsSuccessStatusCode)
                {
                    var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                    _logger.LogError($"Token refresh failed: {errorContent}");
                    return null;
                }

                var tokenJsonResponse = await tokenResponse.Content.ReadAsStringAsync();
                var tokenDocument = JsonDocument.Parse(tokenJsonResponse);
                var tokenData = tokenDocument.RootElement;
                
                var result = new TokenResponse
                {
                    AccessToken = tokenData.GetProperty("access_token").GetString()!,
                    RefreshToken = tokenData.TryGetProperty("refresh_token", out var refreshProp) ? 
                                 refreshProp.GetString() : refreshToken, // Use existing if not provided
                    ExpiresIn = tokenData.GetProperty("expires_in").GetInt32(),
                    ExpiresAt = DateTime.UtcNow.AddSeconds(tokenData.GetProperty("expires_in").GetInt32()),
                    TokenType = tokenData.GetProperty("token_type").GetString()!,
                    Scope = tokenData.TryGetProperty("scope", out var scopeProp) ? 
                           scopeProp.GetString() : "https://graph.microsoft.com/Mail.Read https://graph.microsoft.com/Mail.Send"
                };

                _logger.LogInformation("Access token refreshed successfully");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to refresh access token: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets a valid access token, automatically refreshing if needed
        /// This is the key method for seamless token management
        /// </summary>
        public async Task<string?> GetValidAccessTokenAsync(MailAccount mailAccount, Func<MailAccount, Task> updateMailAccountCallback)
        {
            try
            {
                // Check if current token is still valid
                if (!mailAccount.NeedsTokenRefresh() && mailAccount.IsTokenValid)
                {
                    return mailAccount.AccessToken;
                }

                // Token needs refresh - check if we have a valid refresh token
                if (!mailAccount.HasValidRefreshToken())
                {
                    _logger.LogWarning($"No valid refresh token available for account {mailAccount.EmailAddress}");
                    mailAccount.IsTokenValid = false;
                    await updateMailAccountCallback(mailAccount);
                    return null;
                }

                // Refresh the token
                var tokenResponse = await RefreshAccessTokenAsync(mailAccount.RefreshToken!);
                if (tokenResponse == null)
                {
                    _logger.LogError($"Failed to refresh token for account {mailAccount.EmailAddress}");
                    mailAccount.IsTokenValid = false;
                    await updateMailAccountCallback(mailAccount);
                    return null;
                }

                // Update the mail account with new tokens
                mailAccount.AccessToken = tokenResponse.AccessToken;
                if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
                {
                    mailAccount.RefreshToken = tokenResponse.RefreshToken;
                }
                mailAccount.TokenExpiresAt = tokenResponse.ExpiresAt;
                mailAccount.IsTokenValid = true;

                // Save the updated tokens
                await updateMailAccountCallback(mailAccount);

                _logger.LogInformation($"Token refreshed successfully for account {mailAccount.EmailAddress}");
                return mailAccount.AccessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting valid access token for {mailAccount.EmailAddress}: {ex.Message}");
                mailAccount.IsTokenValid = false;
                await updateMailAccountCallback(mailAccount);
                return null;
            }
        }

        /// <summary>
        /// Exchange authorization code for access and refresh tokens
        /// This is called after user completes OAuth flow
        /// </summary>
        public async Task<TokenResponse?> ExchangeCodeForTokensAsync(string authorizationCode, string redirectUri)
        {
            try
            {
                _logger.LogInformation("Exchanging authorization code for tokens");

                using var httpClient = new HttpClient();
                var tokenEndpoint = $"{_configuration["MicrosoftGraph:Instance"]}common/oauth2/v2.0/token";

                var tokenRequestData = new List<KeyValuePair<string, string>>
                {
                    new("client_id", _clientId),
                    new("client_secret", _clientSecret),
                    new("grant_type", "authorization_code"),
                    new("code", authorizationCode),
                    new("redirect_uri", redirectUri),
                    new("scope", "https://graph.microsoft.com/Mail.Read https://graph.microsoft.com/Mail.Send")
                };

                var tokenRequest = new FormUrlEncodedContent(tokenRequestData);
                var tokenResponse = await httpClient.PostAsync(tokenEndpoint, tokenRequest);

                if (!tokenResponse.IsSuccessStatusCode)
                {
                    var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                    _logger.LogError($"Token exchange failed: {errorContent}");
                    return null;
                }

                var tokenJsonResponse = await tokenResponse.Content.ReadAsStringAsync();
                var tokenDocument = JsonDocument.Parse(tokenJsonResponse);
                var tokenData = tokenDocument.RootElement;
                
                var result = new TokenResponse
                {
                    AccessToken = tokenData.GetProperty("access_token").GetString()!,
                    RefreshToken = tokenData.TryGetProperty("refresh_token", out var refreshProp) ? 
                                 refreshProp.GetString() : null,
                    ExpiresIn = tokenData.GetProperty("expires_in").GetInt32(),
                    ExpiresAt = DateTime.UtcNow.AddSeconds(tokenData.GetProperty("expires_in").GetInt32()),
                    TokenType = tokenData.GetProperty("token_type").GetString()!,
                    Scope = tokenData.TryGetProperty("scope", out var scopeProp) ? 
                           scopeProp.GetString() : "https://graph.microsoft.com/Mail.Read https://graph.microsoft.com/Mail.Send"
                };

                _logger.LogInformation("Tokens obtained successfully");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to exchange code for tokens: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generate OAuth2 authorization URL for user to login
        /// </summary>
        public string GetAuthorizationUrl(string redirectUri, string? state = null)
        {
            var baseUrl = $"{_configuration["MicrosoftGraph:Instance"]}common/oauth2/v2.0/authorize";
            var scope = "https://graph.microsoft.com/Mail.Read https://graph.microsoft.com/Mail.Send";
            
            var queryParams = new List<string>
            {
                $"client_id={Uri.EscapeDataString(_clientId)}",
                $"response_type=code",
                $"redirect_uri={Uri.EscapeDataString(redirectUri)}",
                $"scope={Uri.EscapeDataString(scope)}",
                $"response_mode=query"
            };

            if (!string.IsNullOrEmpty(state))
            {
                queryParams.Add($"state={Uri.EscapeDataString(state)}");
            }

            return $"{baseUrl}?{string.Join("&", queryParams)}";
        }

        /// <summary>
        /// Get user profile information from Microsoft Graph API
        /// </summary>
        public async Task<UserProfile?> GetUserProfileAsync(string accessToken)
        {
            try
            {
                var graphServiceClient = CreateGraphServiceClient(accessToken);
                var user = await graphServiceClient.Me.GetAsync();
                
                return new UserProfile
                {
                    Email = user?.Mail ?? user?.UserPrincipalName,
                    DisplayName = user?.DisplayName,
                    GivenName = user?.GivenName,
                    Surname = user?.Surname
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get user profile: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// User profile information from Microsoft Graph
    /// </summary>
    public class UserProfile
    {
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
        public string? GivenName { get; set; }
        public string? Surname { get; set; }
    }
} 