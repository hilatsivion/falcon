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
using System.IO; // Added for Path.GetExtension

namespace FalconBackend.Services
{
    public class OutlookService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OutlookService> _logger;
        private readonly FileStorageService _fileStorageService;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _tenantId;
        private readonly string _graphApiUrl;

        public OutlookService(IConfiguration configuration, ILogger<OutlookService> logger, FileStorageService fileStorageService)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
            
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
        public async Task<List<MailReceived>> GetUserEmailsAsync(string accessToken, string mailAccountId, string userEmail, int maxEmails = 50)
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
                            var mailReceived = await ConvertToMailReceivedAsync(message, mailAccountId, accessToken, userEmail);
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
        /// Fetches sent emails from user's Outlook Sent Items folder
        /// </summary>
        public async Task<List<MailSent>> GetUserSentEmailsAsync(string accessToken, string mailAccountId, string userEmail, int maxEmails = 50)
        {
            try
            {
                _logger.LogInformation($"Fetching sent emails for account {mailAccountId}");
                
                var graphServiceClient = CreateGraphServiceClient(accessToken);
                
                // Get messages from the user's Sent Items folder using Microsoft Graph API
                var messages = await graphServiceClient.Me.MailFolders["SentItems"].Messages.GetAsync((requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Top = maxEmails;
                    requestConfiguration.QueryParameters.Orderby = new[] { "sentDateTime desc" };
                });

                var sentEmailList = new List<MailSent>();

                if (messages?.Value != null)
                {
                    foreach (var message in messages.Value)
                    {
                        try
                        {
                            var mailSent = await ConvertToMailSentAsync(message, mailAccountId, accessToken, userEmail);
                            sentEmailList.Add(mailSent);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Failed to convert sent message {message.Id}: {ex.Message}");
                            continue; // Skip this email and continue with others
                        }
                    }
                }

                _logger.LogInformation($"Successfully fetched {sentEmailList.Count} sent emails for account {mailAccountId}");
                return sentEmailList;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching sent emails for account {mailAccountId}: {ex.Message}");
                throw new Exception($"Failed to fetch sent emails from Outlook: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Converts Microsoft Graph Message to MailReceived entity with attachments
        /// </summary>
        private async Task<MailReceived> ConvertToMailReceivedAsync(Message message, string mailAccountId, string accessToken, string userEmail)
        {
            var mailReceived = new MailReceived
            {
                MailAccountId = mailAccountId,
                Subject = message.Subject ?? "No Subject",
                Body = GetEmailBody(message),
                Sender = GetSenderString(message),
                TimeReceived = ConvertToUtc(message.ReceivedDateTime),
                IsRead = message.IsRead ?? false,
                IsFavorite = message.Flag?.FlagStatus == Microsoft.Graph.Models.FollowupFlagStatus.Flagged,
                Recipients = GetRecipients(message),
                MailTags = new List<MailTag>() // Tags can be assigned later based on content analysis
            };

            // Process attachments if the message has any
            if (message.HasAttachments == true && !string.IsNullOrEmpty(message.Id))
            {
                try
                {
                    mailReceived.Attachments = await ProcessEmailAttachmentsAsync(
                        accessToken, message.Id, userEmail, mailAccountId, "Received");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to process attachments for received email {message.Id}: {ex.Message}");
                    mailReceived.Attachments = new List<Attachments>(); // Fallback to empty list
                }
            }
            else
            {
                mailReceived.Attachments = new List<Attachments>();
            }

            return mailReceived;
        }

        /// <summary>
        /// Converts Microsoft Graph Message to MailSent entity with attachments
        /// </summary>
        private async Task<MailSent> ConvertToMailSentAsync(Message message, string mailAccountId, string accessToken, string userEmail)
        {
            var mailSent = new MailSent
            {
                MailAccountId = mailAccountId,
                Subject = message.Subject ?? "No Subject",
                Body = GetEmailBody(message),
                TimeSent = ConvertToUtc(message.SentDateTime),
                IsFavorite = message.Flag?.FlagStatus == Microsoft.Graph.Models.FollowupFlagStatus.Flagged,
                Recipients = GetRecipients(message)
            };

            // Process attachments if the message has any
            if (message.HasAttachments == true && !string.IsNullOrEmpty(message.Id))
            {
                try
                {
                    mailSent.Attachments = await ProcessEmailAttachmentsAsync(
                        accessToken, message.Id, userEmail, mailAccountId, "Sent");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to process attachments for sent email {message.Id}: {ex.Message}");
                    mailSent.Attachments = new List<Attachments>(); // Fallback to empty list
                }
            }
            else
            {
                mailSent.Attachments = new List<Attachments>();
            }

            return mailSent;
        }

        /// <summary>
        /// Converts DateTimeOffset from Microsoft Graph API to UTC DateTime for consistent storage
        /// This ensures all email timestamps are in UTC, matching what analytics expects
        /// </summary>
        private DateTime ConvertToUtc(DateTimeOffset? dateTimeOffset)
        {
            if (dateTimeOffset.HasValue)
            {
                var originalOffset = dateTimeOffset.Value;
                var utcDateTime = originalOffset.UtcDateTime;
                
                // Log timezone conversion for debugging (only if there's a significant offset)
                if (Math.Abs(originalOffset.Offset.TotalHours) > 0.1) // More than ~6 minutes offset
                {
                    _logger.LogDebug($"Converted email timestamp from {originalOffset} to {utcDateTime:yyyy-MM-dd HH:mm:ss} UTC");
                }
                
                return utcDateTime;
            }
            
            // Fallback to current UTC time if no timestamp provided
            var fallbackTime = DateTime.UtcNow;
            _logger.LogWarning($"No timestamp provided for email, using fallback: {fallbackTime:yyyy-MM-dd HH:mm:ss} UTC");
            return fallbackTime;
        }

        /// <summary>
        /// Extracts email body content, preferring HTML over text
        /// Converts HTML to plain text for better AI processing and storage
        /// </summary>
        private string GetEmailBody(Message message)
        {
            if (message.Body?.Content != null)
            {
                var htmlContent = message.Body.Content;
                
                // Check if content is HTML
                if (message.Body.ContentType == Microsoft.Graph.Models.BodyType.Html)
                {
                    return ConvertHtmlToPlainText(htmlContent);
                }
                
                return htmlContent; // Already plain text
            }
            
            if (!string.IsNullOrEmpty(message.BodyPreview))
            {
                return message.BodyPreview;
            }

            return "No body content available";
        }

        /// <summary>
        /// Converts HTML email content to clean plain text
        /// </summary>
        private string ConvertHtmlToPlainText(string htmlContent)
        {
            if (string.IsNullOrEmpty(htmlContent))
                return string.Empty;

            try
            {
                // Remove script and style elements completely
                htmlContent = System.Text.RegularExpressions.Regex.Replace(
                    htmlContent, @"<(script|style)[^>]*?>.*?</\1>", "", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

                // Replace common HTML elements with appropriate spacing/formatting
                htmlContent = System.Text.RegularExpressions.Regex.Replace(htmlContent, @"<br\s*/?> | </ br>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                htmlContent = System.Text.RegularExpressions.Regex.Replace(htmlContent, @"</?(div|p|h[1-6]|li|tr)[^>]*?>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                htmlContent = System.Text.RegularExpressions.Regex.Replace(htmlContent, @"</?(ul|ol|table)[^>]*?>", "\n\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                // Remove all remaining HTML tags
                htmlContent = System.Text.RegularExpressions.Regex.Replace(htmlContent, @"<[^>]+>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                // Decode HTML entities
                htmlContent = System.Net.WebUtility.HtmlDecode(htmlContent);
                
                // Clean up whitespace
                htmlContent = System.Text.RegularExpressions.Regex.Replace(htmlContent, @"\n\s*\n", "\n\n"); // Multiple newlines to double newline
                htmlContent = System.Text.RegularExpressions.Regex.Replace(htmlContent, @"[ \t]+", " "); // Multiple spaces to single space
                htmlContent = htmlContent.Trim();

                return htmlContent;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to convert HTML to plain text: {ex.Message}");
                // Fallback: basic HTML tag removal
                return System.Text.RegularExpressions.Regex.Replace(htmlContent, @"<[^>]+>", " ").Trim();
            }
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
        /// Downloads and processes all attachments for an email message
        /// </summary>
        private async Task<List<Attachments>> ProcessEmailAttachmentsAsync(string accessToken, string messageId, string userEmail, string mailAccountId, string emailType)
        {
            var attachments = new List<Attachments>();

            try
            {
                var graphServiceClient = CreateGraphServiceClient(accessToken);
                
                // Get attachments for the message
                var messageAttachments = await graphServiceClient.Me.Messages[messageId].Attachments.GetAsync();

                if (messageAttachments?.Value == null || !messageAttachments.Value.Any())
                {
                    return attachments; // No attachments
                }

                _logger.LogInformation($"Found {messageAttachments.Value.Count} attachments for message {messageId}");

                foreach (var attachment in messageAttachments.Value)
                {
                    try
                    {
                        // Only handle file attachments (not item attachments or reference attachments)
                        if (attachment is Microsoft.Graph.Models.FileAttachment fileAttachment)
                        {
                            var processedAttachment = await ProcessFileAttachmentAsync(
                                fileAttachment, userEmail, mailAccountId, emailType);
                            
                            if (processedAttachment != null)
                            {
                                attachments.Add(processedAttachment);
                            }
                        }
                        else
                        {
                            _logger.LogInformation($"Skipping non-file attachment: {attachment.Name} (Type: {attachment.OdataType})");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to process attachment {attachment.Name}: {ex.Message}");
                        continue; // Skip this attachment and continue with others
                    }
                }

                _logger.LogInformation($"Successfully processed {attachments.Count} attachments for message {messageId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing attachments for message {messageId}: {ex.Message}");
                // Return empty list if attachment processing fails - don't fail the entire email sync
            }

            return attachments;
        }

        /// <summary>
        /// Processes a single file attachment from Microsoft Graph API
        /// </summary>
        private async Task<Attachments?> ProcessFileAttachmentAsync(Microsoft.Graph.Models.FileAttachment fileAttachment, string userEmail, string mailAccountId, string emailType)
        {
            try
            {
                if (fileAttachment.ContentBytes == null || fileAttachment.ContentBytes.Length == 0)
                {
                    _logger.LogWarning($"Attachment {fileAttachment.Name} has no content");
                    return null;
                }

                // Save the attachment to file system
                var filePath = await _fileStorageService.SaveAttachmentAsync(
                    fileAttachment.ContentBytes,
                    fileAttachment.Name ?? "unnamed_file",
                    userEmail,
                    mailAccountId,
                    emailType
                );

                // Create attachment entity
                var attachment = new Attachments
                {
                    Name = fileAttachment.Name ?? "unnamed_file",
                    FileType = Path.GetExtension(fileAttachment.Name ?? "") ?? "unknown",
                    FileSize = fileAttachment.Size ?? fileAttachment.ContentBytes.Length,
                    FilePath = filePath
                };

                _logger.LogInformation($"Successfully processed attachment: {attachment.Name} ({attachment.FileSize} bytes)");
                return attachment;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to process file attachment {fileAttachment.Name}: {ex.Message}");
                return null;
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
                _logger.LogInformation($"Using redirect URI: {redirectUri}");
                _logger.LogInformation($"Using client ID: {_clientId}");

                using var httpClient = new HttpClient();
                var tokenEndpoint = $"{_configuration["MicrosoftGraph:Instance"]}common/oauth2/v2.0/token";
                _logger.LogInformation($"Token endpoint: {tokenEndpoint}");

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
                    _logger.LogError($"Token exchange failed with status {tokenResponse.StatusCode}");
                    _logger.LogError($"Error response: {errorContent}");
                    _logger.LogError($"Request details - Redirect URI: {redirectUri}, Client ID: {_clientId}");
                    
                    // Try to parse the error for more specific details
                    try
                    {
                        var errorDocument = JsonDocument.Parse(errorContent);
                        var errorData = errorDocument.RootElement;
                        if (errorData.TryGetProperty("error", out var errorProp))
                        {
                            var errorType = errorProp.GetString();
                            _logger.LogError($"OAuth Error Type: {errorType}");
                            
                            if (errorData.TryGetProperty("error_description", out var descProp))
                            {
                                var errorDesc = descProp.GetString();
                                _logger.LogError($"OAuth Error Description: {errorDesc}");
                            }
                        }
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning($"Could not parse error response: {parseEx.Message}");
                    }
                    
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