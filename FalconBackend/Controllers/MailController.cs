using FalconBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace FalconBackend.Controllers
{
    [ApiController]
    [Route("api/mail")]
    [Authorize] // Require authentication for all endpoints
    public class MailController : ControllerBase
    {
        private readonly MailService _mailService;

        public MailController(MailService mailService)
        {
            _mailService = mailService ?? throw new ArgumentNullException(nameof(mailService));
        }

        private string GetUserEmailFromToken()
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                throw new UnauthorizedAccessException("Missing or invalid authentication token.");

            var token = authorizationHeader.Replace("Bearer ", "").Trim();
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

            return jwtToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        }

        // Fetch received emails for a specific mail account
        [HttpGet("received/byMailAccount/{mailAccountId}")]
        public async Task<IActionResult> GetReceivedEmailsByMailAccountAsync(string mailAccountId)
        {
            try
            {
                var emails = await _mailService.GetReceivedEmailsByMailAccountAsync(mailAccountId);
                if (!emails.Any())
                    return NotFound("No received emails found for this mail account.");
                return Ok(emails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to retrieve received emails. Error: {ex.Message}");
            }
        }

        // Fetch sent emails for a specific mail account
        [HttpGet("sent/byMailAccount/{mailAccountId}")]
        public async Task<IActionResult> GetSentEmailsByMailAccountAsync(string mailAccountId)
        {
            try
            {
                var emails = await _mailService.GetSentEmailsByMailAccountAsync(mailAccountId);
                if (!emails.Any())
                    return NotFound("No sent emails found for this mail account.");
                return Ok(emails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to retrieve sent emails. Error: {ex.Message}");
            }
        }

        // Fetch draft emails for a specific mail account
        [HttpGet("drafts/byMailAccount/{mailAccountId}")]
        public async Task<IActionResult> GetDraftEmailsByMailAccountAsync(string mailAccountId)
        {
            try
            {
                var drafts = await _mailService.GetDraftEmailsByMailAccountAsync(mailAccountId);
                if (!drafts.Any())
                    return NotFound("No drafts found for this mail account.");
                return Ok(drafts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to retrieve drafts. Error: {ex.Message}");
            }
        }

        // Fetch all received emails across all mail accounts of the user
        [HttpGet("received")]
        public async Task<IActionResult> GetAllReceivedEmailsByUserAsync()
        {
            try
            {
                var userEmail = GetUserEmailFromToken();
                var emails = await _mailService.GetAllReceivedEmailsByUserAsync(userEmail);
                if (!emails.Any())
                    return NotFound("No received emails found for this user.");
                return Ok(emails);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to retrieve received emails. Error: {ex.Message}");
            }
        }

        // Fetch all sent emails across all mail accounts of the user
        [HttpGet("sent")]
        public async Task<IActionResult> GetAllSentEmailsByUserAsync()
        {
            try
            {
                var userEmail = GetUserEmailFromToken();
                var emails = await _mailService.GetAllSentEmailsByUserAsync(userEmail);
                if (!emails.Any())
                    return NotFound("No sent emails found for this user.");
                return Ok(emails);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to retrieve sent emails. Error: {ex.Message}");
            }
        }

        // Fetch all draft emails across all mail accounts of the user
        [HttpGet("drafts")]
        public async Task<IActionResult> GetAllDraftEmailsByUserAsync()
        {
            try
            {
                var userEmail = GetUserEmailFromToken();
                var drafts = await _mailService.GetAllDraftEmailsByUserAsync(userEmail);
                if (!drafts.Any())
                    return NotFound("No drafts found for this user.");
                return Ok(drafts);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to retrieve drafts. Error: {ex.Message}");
            }
        }

        [HttpPut("favorite/{mailId}/{isFavorite}")]
        public async Task<IActionResult> ToggleFavorite(int mailId, bool isFavorite)
        {
            try
            {
                var result = await _mailService.ToggleFavoriteAsync(mailId, isFavorite);
                if (!result)
                    return NotFound("Email not found.");
                return Ok(isFavorite ? "Email marked as favorite." : "Email unmarked as favorite.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to update favorite status. Error: {ex.Message}");
            }
        }

        [HttpPut("read/{mailId}/{isRead}")]
        public async Task<IActionResult> ToggleRead(int mailId, bool isRead)
        {
            try
            {
                var result = await _mailService.ToggleReadAsync(mailId, isRead);
                if (!result)
                    return NotFound("Email not found.");
                return Ok(isRead ? "Email marked as read." : "Email marked as unread.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to update read status. Error: {ex.Message}");
            }
        }
    }
}
