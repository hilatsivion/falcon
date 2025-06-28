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

        [HttpGet("favorites/preview")]
        [Authorize]
        public async Task<IActionResult> GetFavoritePreviews([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User email claim not found in token.");
                }

                var favoriteEmails = await _mailService.GetFavoriteEmailPreviewsAsync(userEmail, page, pageSize);
                return Ok(favoriteEmails); // Returns FavoriteEmailsDto { ReceivedFavorites: [...], SentFavorites: [...] }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- Error fetching favorite previews for user {User.FindFirstValue(ClaimTypes.Email)}: {ex.Message} ---");
                return StatusCode(500, "An error occurred while retrieving favorite emails.");
            }
        }


        [HttpGet("unread/preview")]
        [Authorize]
        public async Task<IActionResult> GetUnreadPreviews([FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User email claim not found in token.");
                }

                var unreadEmails = await _mailService.GetUnreadEmailPreviewsAsync(userEmail, page, pageSize);
                return Ok(unreadEmails); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- Error fetching unread previews for user {User.FindFirstValue(ClaimTypes.Email)}: {ex.Message} ---");
                return StatusCode(500, "An error occurred while retrieving unread emails.");
            }
        }

        [HttpGet("trash/preview")]
        [Authorize]
        public async Task<IActionResult> GetTrashPreviews([FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User email claim not found in token.");
                }

                var trashEmails = await _mailService.GetTrashEmailPreviewsAsync(userEmail, page, pageSize);
                return Ok(trashEmails);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- Error fetching trash previews for user {User.FindFirstValue(ClaimTypes.Email)}: {ex.Message} ---");
                return StatusCode(500, "An error occurred while retrieving trash emails.");
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

                if (!await _mailService.DoesUserOwnMailAccountAsync(userEmail, request.MailAccountId))
                {
                    return Unauthorized("User does not own this mail account.");
                }

                await _mailService.SendMailAsync(request, attachments);

                return Ok("Mail sent successfully.");
            }
            catch (KeyNotFoundException ex) 
            {
                return NotFound(new { message = ex.Message }); 
            }
            catch (ArgumentException ex) 
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex) 
            {
                return Unauthorized(ex.Message);
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
                    return NotFound("No emails found to delete.");

                return Ok("Selected emails deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting emails: {ex.Message}");
            }
        }

        [HttpPut("restore")]
        public async Task<IActionResult> RestoreMails([FromBody] List<MailDeleteDto> mailsToRestore)
        {
            try
            {
                var userEmail = GetUserEmailFromToken();
                var result = await _mailService.RestoreMailsAsync(mailsToRestore, userEmail);
                if (!result)
                    return NotFound("No emails found to restore.");

                return Ok("Selected emails restored successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error restoring emails: {ex.Message}");
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

        [HttpPost("search")]
        [Authorize]
        public async Task<IActionResult> SearchEmails([FromBody] MailSearchRequestDto request)
        {
            const string PLACEHOLDER = "-";

            if (request == null)
            {
                return BadRequest(new { message = "Invalid search request." });
            }

            string effectiveKeywords = request.Keywords == PLACEHOLDER ? null : request.Keywords;
            string effectiveSender = request.Sender == PLACEHOLDER ? null : request.Sender;
            string effectiveRecipient = request.Recipient == PLACEHOLDER ? null : request.Recipient;

            if (string.IsNullOrWhiteSpace(effectiveKeywords) && string.IsNullOrWhiteSpace(effectiveSender) && string.IsNullOrWhiteSpace(effectiveRecipient))
            {
                return BadRequest(new { message = "Please provide at least one search criterion (keywords, sender, or recipient)." });
            }

            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User email claim not found in token.");
                }

                var searchResults = await _mailService.SearchEmailsAsync(
                    userEmail,
                    effectiveKeywords,
                    effectiveSender,
                    effectiveRecipient
                );

                return Ok(searchResults);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- Error during email search for user {User.FindFirstValue(ClaimTypes.Email)}: {ex.Message} ---");
                return StatusCode(500, "An error occurred while searching emails.");
            }
        }


    }
}
