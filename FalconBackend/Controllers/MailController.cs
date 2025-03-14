using FalconBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

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

        [HttpGet("received")]
        public async Task<IActionResult> GetReceivedEmailsAsync()
        {
            try
            {
                var userEmail = GetUserEmailFromToken();
                var emails = await _mailService.GetReceivedEmailsAsync(userEmail);
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

        [HttpGet("sent")]
        public async Task<IActionResult> GetSentEmailsAsync()
        {
            try
            {
                var userEmail = GetUserEmailFromToken();
                var emails = await _mailService.GetSentEmailsAsync(userEmail);
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

        [HttpGet("drafts")]
        public async Task<IActionResult> GetDraftEmailsAsync()
        {
            try
            {
                var userEmail = GetUserEmailFromToken();
                var drafts = await _mailService.GetDraftEmailsAsync(userEmail);
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

        [HttpPost("received")]
        public async Task<IActionResult> AddReceivedEmailAsync(string sender, string subject, string body, List<IFormFile> attachments)
        {
            try
            {
                var userEmail = GetUserEmailFromToken();
                if (string.IsNullOrEmpty(sender) || string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(body))
                    return BadRequest("Sender, subject, and body are required.");

                var result = await _mailService.AddReceivedEmailAsync(userEmail, sender, subject, body, attachments);
                if (result == null)
                    return Conflict("Duplicate email detected.");

                return CreatedAtAction(nameof(GetReceivedEmailsAsync), new { userEmail }, result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to save received email. Error: {ex.Message}");
            }
        }

        [HttpPost("sent")]
        public async Task<IActionResult> AddSentEmailAsync(string subject, string body, List<IFormFile> attachments)
        {
            try
            {
                var userEmail = GetUserEmailFromToken();
                if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(body))
                    return BadRequest("Subject and body are required.");

                var result = await _mailService.AddSentEmailAsync(userEmail, subject, body, attachments);
                return CreatedAtAction(nameof(GetSentEmailsAsync), new { userEmail }, result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to save sent email. Error: {ex.Message}");
            }
        }

        [HttpPost("draft")]
        public async Task<IActionResult> AddDraftEmailAsync(string subject, string body, List<IFormFile> attachments)
        {
            try
            {
                var userEmail = GetUserEmailFromToken();
                if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(body))
                    return BadRequest("Subject and body are required.");

                var result = await _mailService.AddDraftEmailAsync(userEmail, subject, body, attachments);
                return CreatedAtAction(nameof(GetDraftEmailsAsync), new { userEmail }, result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to save draft email. Error: {ex.Message}");
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
