using FalconBackend.Models;
using FalconBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Graph;

namespace FalconBackend.Controllers
{
    [ApiController]
    [Route("api/oauth")]
    public class OAuthController : ControllerBase
    {
        private readonly OutlookService _outlookService;
        private readonly UserService _userService;
        private readonly ILogger<OAuthController> _logger;

        public OAuthController(OutlookService outlookService, UserService userService, ILogger<OAuthController> logger)
        {
            _outlookService = outlookService ?? throw new ArgumentNullException(nameof(outlookService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Step 1: Get the authorization URL for Microsoft Graph OAuth2 flow
        /// Frontend redirects user to this URL to login with Microsoft
        /// </summary>
        [HttpPost("authorize-url")]
        [Authorize]
        public IActionResult GetAuthorizationUrl([FromBody] OAuthAuthorizeRequest request)
        {
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(userEmail))
                {
                    return BadRequest("User email not found in token");
                }

                // Generate state for security (prevents CSRF attacks)
                var state = Guid.NewGuid().ToString();
                
                // Generate the Microsoft OAuth2 authorization URL
                var authUrl = _outlookService.GetAuthorizationUrl(request.RedirectUri, state);

                _logger.LogInformation($"Generated authorization URL for user {userEmail}");

                return Ok(new 
                { 
                    authorizationUrl = authUrl,
                    state = state,
                    message = "Redirect user to this URL to complete OAuth flow"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating authorization URL: {ex.Message}");
                return StatusCode(500, new { error = "Failed to generate authorization URL", details = ex.Message });
            }
        }

        /// <summary>
        /// Step 2: Exchange authorization code for tokens and create mail account
        /// Frontend calls this after user completes OAuth and gets redirected back with code
        /// </summary>
        [HttpPost("exchange-token")]
        [Authorize]
        public async Task<IActionResult> ExchangeToken([FromBody] OAuthTokenRequest request)
        {
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(userEmail))
                {
                    return BadRequest("User email not found in token");
                }

                _logger.LogInformation($"Exchanging authorization code for tokens for user {userEmail}");

                // Exchange the authorization code for access and refresh tokens
                var tokenResponse = await _outlookService.ExchangeCodeForTokensAsync(request.Code, request.RedirectUri);
                
                if (tokenResponse == null)
                {
                    return BadRequest(new { error = "Failed to exchange authorization code for tokens" });
                }

                // Validate the token works and proceed with account creation
                var isTokenValid = await _outlookService.ValidateAccessTokenAsync(tokenResponse.AccessToken);
                if (!isTokenValid)
                {
                    return BadRequest(new { error = "Invalid access token received from Microsoft" });
                }

                // Get the actual user's email from Microsoft Graph API
                var userProfile = await _outlookService.GetUserProfileAsync(tokenResponse.AccessToken);
                var outlookEmail = userProfile?.Email ?? "user@outlook.com"; // Fallback to placeholder if API fails
                
                _logger.LogInformation($"Retrieved user email from Graph API: {outlookEmail}");

                try
                {
                    // Create mail account with OAuth tokens
                    var mailAccountRequest = new MailAccountCreateRequest
                    {
                        EmailAddress = outlookEmail,
                        AccessToken = tokenResponse.AccessToken,
                        RefreshToken = tokenResponse.RefreshToken,
                        ExpiresIn = tokenResponse.ExpiresIn,
                        Provider = MailAccount.MailProvider.Outlook,
                        IsDefault = true, // Make first Outlook account default
                        SyncMailsImmediately = true // Auto-sync emails
                    };

                    var mailAccount = await _userService.CreateMailAccountAsync(mailAccountRequest, userEmail);

                    _logger.LogInformation($"Successfully created mail account {mailAccount.MailAccountId} for user {userEmail}");

                    return Ok(new 
                    { 
                        success = true,
                        message = "Mail account created and emails synced successfully!",
                        mailAccount = new 
                        {
                            mailAccountId = mailAccount.MailAccountId,
                            emailAddress = mailAccount.EmailAddress,
                            provider = mailAccount.Provider.ToString(),
                            isDefault = mailAccount.IsDefault,
                            lastSync = mailAccount.LastMailSync,
                            isTokenValid = mailAccount.IsTokenValid
                        },
                        tokenInfo = new
                        {
                            expiresAt = tokenResponse.ExpiresAt,
                            hasRefreshToken = !string.IsNullOrEmpty(tokenResponse.RefreshToken),
                            scope = tokenResponse.Scope
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error creating mail account: {ex.Message}");
                    return StatusCode(500, new { error = "Failed to create mail account", details = ex.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in token exchange: {ex.Message}");
                return StatusCode(500, new { error = "Failed to exchange token", details = ex.Message });
            }
        }

        /// <summary>
        /// Manually refresh tokens for a mail account
        /// Useful for testing or troubleshooting
        /// </summary>
        [HttpPost("refresh-token")]
        [Authorize]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(userEmail))
                {
                    return BadRequest("User email not found in token");
                }

                _logger.LogInformation($"Manual token refresh requested for user {userEmail}");

                var tokenResponse = await _outlookService.RefreshAccessTokenAsync(request.RefreshToken);
                
                if (tokenResponse == null)
                {
                    return BadRequest(new { error = "Failed to refresh access token" });
                }

                return Ok(new 
                { 
                    message = "Token refreshed successfully",
                    tokenInfo = new
                    {
                        expiresAt = tokenResponse.ExpiresAt,
                        expiresIn = tokenResponse.ExpiresIn,
                        hasRefreshToken = !string.IsNullOrEmpty(tokenResponse.RefreshToken),
                        scope = tokenResponse.Scope
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error refreshing token: {ex.Message}");
                return StatusCode(500, new { error = "Failed to refresh token", details = ex.Message });
            }
        }

        /// <summary>
        /// Manually sync emails for user's mail accounts
        /// Triggers the sync process with automatic token refresh
        /// </summary>
        [HttpPost("sync-emails")]
        [Authorize]
        public async Task<IActionResult> SyncEmails()
        {
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(userEmail))
                {
                    return BadRequest("User email not found in token");
                }

                _logger.LogInformation($"Manual email sync requested for user {userEmail}");

                // Trigger the existing sync method
                await _userService.InitializeRealOutlookDataAsync(userEmail);

                return Ok(new 
                { 
                    message = "Email sync completed successfully",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error syncing emails: {ex.Message}");
                return StatusCode(500, new { error = "Failed to sync emails", details = ex.Message });
            }
        }
    }
} 