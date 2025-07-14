using FalconBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FalconBackend.Controllers
{
    [ApiController]
    [Route("api/outlook-test")]
    public class OutlookTestController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly OutlookService _outlookService;

        public OutlookTestController(UserService userService, OutlookService outlookService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _outlookService = outlookService ?? throw new ArgumentNullException(nameof(outlookService));
        }

        /// <summary>
        /// Test endpoint to fetch real Outlook emails for the authenticated user
        /// This replaces the old dummy data functionality
        /// </summary>
        [HttpPost("sync-emails")]
        [Authorize]
        public async Task<IActionResult> SyncOutlookEmails()
        {
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User email claim not found in token.");
                }

                await _userService.InitializeRealOutlookDataAsync(userEmail);
                
                return Ok(new 
                { 
                    message = "Outlook email sync completed successfully", 
                    userEmail = userEmail,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    error = "Failed to sync Outlook emails", 
                    details = ex.Message 
                });
            }
        }

        /// <summary>
        /// Public endpoint to get emails with a provided access token - No authorization required
        /// Useful for testing without authentication
        /// </summary>
        [HttpPost("get-emails")]
        public async Task<IActionResult> GetEmailsWithToken([FromBody] GetEmailsRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.AccessToken))
                {
                    return BadRequest("Access token is required");
                }

                var emails = await _outlookService.GetUserEmailsAsync(
                    request.AccessToken, 
                    "public-test-account", 
                    request.MaxEmails ?? 20
                );
                
                return Ok(new 
                { 
                    success = true,
                    emailCount = emails.Count,
                    emails = emails.Select(e => new 
                    {
                        subject = e.Subject,
                        sender = e.Sender,
                        timeReceived = e.TimeReceived,
                        isRead = e.IsRead,
                        isFavorite = e.IsFavorite,
                        bodyPreview = e.Body?.Length > 100 ? e.Body.Substring(0, 100) + "..." : e.Body,
                        recipientCount = e.Recipients?.Count ?? 0
                    }),
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    success = false,
                    error = "Failed to fetch emails", 
                    details = ex.Message 
                });
            }
        }

        /// <summary>
        /// Test endpoint to validate if the user's access token is still valid
        /// </summary>
        [HttpPost("validate-token")]
        public async Task<IActionResult> ValidateOutlookToken([FromBody] string accessToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    return BadRequest("Access token is required");
                }

                bool isValid = await _outlookService.ValidateAccessTokenAsync(accessToken);
                
                return Ok(new 
                { 
                    isValid = isValid,
                    message = isValid ? "Token is valid" : "Token is invalid or expired",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    error = "Failed to validate token", 
                    details = ex.Message 
                });
            }
        }

        /// <summary>
        /// Test endpoint to manually fetch emails with a provided access token
        /// Useful for testing without having it stored in MailAccount
        /// </summary>
        [HttpPost("fetch-emails-test")]
        public async Task<IActionResult> FetchEmailsTest([FromBody] FetchEmailsTestRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.AccessToken))
                {
                    return BadRequest("Access token is required");
                }

                var emails = await _outlookService.GetUserEmailsAsync(
                    request.AccessToken, 
                    "test-account", 
                    request.MaxEmails ?? 10
                );
                
                return Ok(new 
                { 
                    emailCount = emails.Count,
                    emails = emails.Select(e => new 
                    {
                        subject = e.Subject,
                        sender = e.Sender,
                        timeReceived = e.TimeReceived,
                        isRead = e.IsRead,
                        isFavorite = e.IsFavorite,
                        recipientCount = e.Recipients?.Count ?? 0
                    }),
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    error = "Failed to fetch emails", 
                    details = ex.Message 
                });
            }
        }

        /// <summary>
        /// Public endpoint to send email with a provided access token - No authorization required
        /// Useful for testing email sending without authentication
        /// </summary>
        [HttpPost("send-email")]
        public async Task<IActionResult> SendEmail([FromBody] SendEmailRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.AccessToken))
                {
                    return BadRequest("Access token is required");
                }

                if (string.IsNullOrWhiteSpace(request.Subject))
                {
                    return BadRequest("Subject is required");
                }

                if (request.Recipients == null || !request.Recipients.Any())
                {
                    return BadRequest("At least one recipient is required");
                }

                // Convert to internal SendMailRequest format
                var sendMailRequest = new FalconBackend.Models.SendMailRequest
                {
                    Subject = request.Subject,
                    Body = request.Body ?? "",
                    Recipients = request.Recipients
                };

                bool success = await _outlookService.SendEmailAsync(request.AccessToken, sendMailRequest);

                if (success)
                {
                    return Ok(new { 
                        success = true, 
                        message = "Email sent successfully",
                        subject = request.Subject,
                        recipientCount = request.Recipients.Count
                    });
                }
                else
                {
                    return StatusCode(500, new { 
                        success = false, 
                        message = "Failed to send email" 
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error sending email: {ex.Message}");
            }
        }

        /// <summary>
        /// Public endpoint to send detailed email with CC/BCC - No authorization required
        /// </summary>
        [HttpPost("send-detailed-email")]
        public async Task<IActionResult> SendDetailedEmail([FromBody] SendDetailedEmailRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.AccessToken))
                {
                    return BadRequest("Access token is required");
                }

                if (string.IsNullOrWhiteSpace(request.Subject))
                {
                    return BadRequest("Subject is required");
                }

                if (request.ToRecipients == null || !request.ToRecipients.Any())
                {
                    return BadRequest("At least one To recipient is required");
                }

                bool success = await _outlookService.SendEmailWithDetailsAsync(
                    request.AccessToken,
                    request.Subject,
                    request.Body ?? "",
                    request.ToRecipients,
                    request.CcRecipients?.Any() == true ? request.CcRecipients : null,
                    request.BccRecipients?.Any() == true ? request.BccRecipients : null
                );

                if (success)
                {
                    return Ok(new { 
                        success = true, 
                        message = "Detailed email sent successfully",
                        subject = request.Subject,
                        toCount = request.ToRecipients.Count,
                        ccCount = request.CcRecipients?.Count ?? 0,
                        bccCount = request.BccRecipients?.Count ?? 0
                    });
                }
                else
                {
                    return StatusCode(500, new { 
                        success = false, 
                        message = "Failed to send detailed email" 
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error sending detailed email: {ex.Message}");
            }
        }
    }

    public class FetchEmailsTestRequest
    {
        public string AccessToken { get; set; }
        public int? MaxEmails { get; set; }
    }

    public class GetEmailsRequest
    {
        public string AccessToken { get; set; }
        public int? MaxEmails { get; set; }
    }

    public class SendEmailRequest
    {
        public string AccessToken { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public List<string> Recipients { get; set; } = new();
    }

    public class SendDetailedEmailRequest
    {
        public string AccessToken { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public List<string> ToRecipients { get; set; } = new();
        public List<string> CcRecipients { get; set; } = new();
        public List<string> BccRecipients { get; set; } = new();
    }
} 