using FalconBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using FalconBackend.Models;
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

            if (string.IsNullOrWhiteSpace(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                throw new UnauthorizedAccessException("Missing or invalid authentication token.");

            var token = authorizationHeader.Substring("Bearer ".Length).Trim();

            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(token))
                throw new UnauthorizedAccessException("Cannot read JWT token.");

            var jwtToken = handler.ReadJwtToken(token);

            var email = jwtToken.Claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.Email || c.Type == JwtRegisteredClaimNames.Email)?.Value;

            if (string.IsNullOrEmpty(email))
                throw new UnauthorizedAccessException("Email claim not found in token.");

            return email;
        }

        [HttpGet("received/preview")]
        public async Task<IActionResult> GetReceivedEmailPreviews([FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            try
            {
                var userEmail = GetUserEmailFromToken();
                var emails = await _mailService.GetAllReceivedEmailPreviewsAsync(userEmail, page, pageSize);
                return Ok(emails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving received emails: {ex.Message}");
            }
        }

        [HttpGet("sent/preview")]
        public async Task<IActionResult> GetSentEmailPreviews([FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            try
            {
                var userEmail = GetUserEmailFromToken();
                var emails = await _mailService.GetAllSentEmailPreviewsAsync(userEmail, page, pageSize);
                return Ok(emails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving sent emails: {ex.Message}");
            }
        }

        [HttpGet("drafts/preview")]
        public async Task<IActionResult> GetDraftEmailPreviews([FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            try
            {
                var userEmail = GetUserEmailFromToken();
                var drafts = await _mailService.GetAllDraftEmailPreviewsAsync(userEmail, page, pageSize);
                return Ok(drafts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving drafts: {ex.Message}");
            }
        }

        [HttpGet("received/byMailAccount/{mailAccountId}/preview")]
        public async Task<IActionResult> GetReceivedByMailAccountPreview(string mailAccountId, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            try
            {
                var emails = await _mailService.GetReceivedEmailPreviewsByMailAccountAsync(mailAccountId, page, pageSize);
                return Ok(emails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("sent/byMailAccount/{mailAccountId}/preview")]
        public async Task<IActionResult> GetSentByMailAccountPreview(string mailAccountId, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            try
            {
                var emails = await _mailService.GetSentEmailPreviewsByMailAccountAsync(mailAccountId, page, pageSize);
                return Ok(emails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("drafts/byMailAccount/{mailAccountId}/preview")]
        public async Task<IActionResult> GetDraftByMailAccountPreview(string mailAccountId, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            try
            {
                var drafts = await _mailService.GetDraftEmailPreviewsByMailAccountAsync(mailAccountId, page, pageSize);
                return Ok(drafts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }


        [HttpPut("favorite/{mailId}/{isFavorite}")]
        public async Task<IActionResult> ToggleFavorite(int mailId, bool isFavorite)
        {
            try
            {
                var userEmail = GetUserEmailFromToken();

                var isOwner = await _mailService.IsUserOwnerOfMailAsync(mailId, userEmail);
                if (!isOwner)
                    return Forbid("You are not authorized to modify this mail.");

                var result = await _mailService.ToggleFavoriteAsync(mailId, isFavorite);
                if (!result)
                    return NotFound("Email not found.");

                return Ok(isFavorite ? "Email marked as favorite." : "Email unmarked as favorite.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to update favorite status. Error: {ex.Message}");
            }
        }


        [HttpPut("read")]
        public async Task<IActionResult> ToggleReadBatch([FromBody] List<ToggleReadDto> readUpdates)
        {
            try
            {
                var userEmail = GetUserEmailFromToken();
                var result = await _mailService.ToggleReadBatchAsync(readUpdates, userEmail);

                if (!result)
                    return BadRequest("Some emails are not owned by this user or update failed.");

                return Ok("Read status updated for selected emails.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to update read status. Error: {ex.Message}");
            }
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMail([FromForm] SendMailRequest request, [FromForm] List<IFormFile> attachments)
        {
            try
            {
                string userEmail = GetUserEmailFromToken();

                // Validate user owns the mail account
                if (!await _mailService.DoesUserOwnMailAccountAsync(userEmail, request.MailAccountId))
                    return Unauthorized("User does not own this mail account.");

                await _mailService.SendMailAsync(request, attachments);

                return Ok("Mail sent successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to send mail. Error: {ex.Message}");
            }
        }


        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteMails([FromBody] List<MailDeleteDto> mailsToDelete)
        {
            try
            {
                var userEmail = GetUserEmailFromToken();

                var result = await _mailService.DeleteMailsAsync(mailsToDelete, userEmail);

                if (!result)
                    return NotFound("No matching emails owned by the user found to delete.");

                return Ok("Selected emails deleted successfully.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to delete emails. Error: {ex.Message}");
            }
        }


        [HttpGet("received/full/{mailId}")]
        public async Task<IActionResult> GetReceivedMailByIdAsync(int mailId)
        {
            try
            {
                var userEmail = GetUserEmailFromToken();
                var mail = await _mailService.GetReceivedMailByIdAsync(userEmail, mailId);
                if (mail == null)
                    return NotFound("Mail not found or you don't have access.");
                return Ok(mail);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to get received mail. Error: {ex.Message}");
            }
        }

        [HttpGet("sent/full/{mailId}")]
        public async Task<IActionResult> GetSentMailByIdAsync(int mailId)
        {
            try
            {
                var userEmail = GetUserEmailFromToken();
                var mail = await _mailService.GetSentMailByIdAsync(userEmail, mailId);
                if (mail == null)
                    return NotFound("Mail not found or you don't have access.");
                return Ok(mail);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to get sent mail. Error: {ex.Message}");
            }
        }

        [HttpGet("draft/full/{mailId}")]
        public async Task<IActionResult> GetDraftByIdAsync(int mailId)
        {
            try
            {
                var userEmail = GetUserEmailFromToken();
                var mail = await _mailService.GetDraftByIdAsync(userEmail, mailId);
                if (mail == null)
                    return NotFound("Draft not found or you don't have access.");
                return Ok(mail);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to get draft. Error: {ex.Message}");
            }
        }
    }
}
