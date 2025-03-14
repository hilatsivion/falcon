using FalconBackend.Data;
using FalconBackend.Models;
using FalconBackend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FalconBackend.Controllers
{
    [ApiController]
    [Route("api/mail")]
    public class MailController : ControllerBase
    {
        private readonly MailService _mailService;

        public MailController(MailService mailService)
        {
            _mailService = mailService ?? throw new ArgumentNullException(nameof(mailService));
        }

        [HttpGet("received/{userEmail}")]
        public async Task<IActionResult> GetReceivedEmailsAsync(string userEmail)
        {
            try
            {
                var emails = await _mailService.GetReceivedEmailsAsync(userEmail);
                if (!emails.Any())
                    return NotFound("No received emails found for this user.");
                return Ok(emails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to retrieve received emails. Error: {ex.Message}");
            }
        }

        [HttpGet("sent/{userEmail}")]
        public async Task<IActionResult> GetSentEmailsAsync(string userEmail)
        {
            try
            {
                var emails = await _mailService.GetSentEmailsAsync(userEmail);
                if (!emails.Any())
                    return NotFound("No sent emails found for this user.");
                return Ok(emails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to retrieve sent emails. Error: {ex.Message}");
            }
        }

        [HttpGet("drafts/{userEmail}")]
        public async Task<IActionResult> GetDraftEmailsAsync(string userEmail)
        {
            try
            {
                var drafts = await _mailService.GetDraftEmailsAsync(userEmail);
                if (!drafts.Any())
                    return NotFound("No drafts found for this user.");
                return Ok(drafts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to retrieve drafts. Error: {ex.Message}");
            }
        }

        [HttpPost("received")]
        public async Task<IActionResult> AddReceivedEmailAsync(string mailAccountToken, string sender, string subject, string body, List<IFormFile> attachments)
        {
            try
            {
                if (string.IsNullOrEmpty(sender) || string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(body))
                    return BadRequest("Sender, subject, and body are required.");

                var result = await _mailService.AddReceivedEmailAsync(mailAccountToken, sender, subject, body, attachments);
                if (result == null)
                    return Conflict("Duplicate email detected.");

                return CreatedAtAction(nameof(GetReceivedEmailsAsync), new { userEmail = mailAccountToken }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to save received email. Error: {ex.Message}");
            }
        }

        [HttpPost("sent")]
        public async Task<IActionResult> AddSentEmailAsync(string mailAccountToken, string subject, string body, List<IFormFile> attachments)
        {
            try
            {
                if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(body))
                    return BadRequest("Subject and body are required.");

                var result = await _mailService.AddSentEmailAsync(mailAccountToken, subject, body, attachments);
                return CreatedAtAction(nameof(GetSentEmailsAsync), new { userEmail = mailAccountToken }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to save sent email. Error: {ex.Message}");
            }
        }

        [HttpPost("draft")]
        public async Task<IActionResult> AddDraftEmailAsync(string mailAccountToken, string subject, string body, List<IFormFile> attachments)
        {
            try
            {
                if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(body))
                    return BadRequest("Subject and body are required.");

                var result = await _mailService.AddDraftEmailAsync(mailAccountToken, subject, body, attachments);
                return CreatedAtAction(nameof(GetDraftEmailsAsync), new { userEmail = mailAccountToken }, result);
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
