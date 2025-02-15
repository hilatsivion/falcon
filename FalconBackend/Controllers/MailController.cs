using FalconBackend.Services;
using FalconBackend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FalconBackend.Controllers
{
    [Route("api/mail")]
    [ApiController]
    public class MailController : ControllerBase
    {
        private readonly MailService _mailService;

        public MailController(MailService mailService)
        {
            _mailService = mailService;
        }

        // Get all received emails for a user
        [HttpGet("received/{userId}")]
        public async Task<IActionResult> GetReceivedEmails(int userId)
        {
            try
            {
                var emails = await _mailService.GetReceivedEmailsAsync(userId);
                return Ok(emails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching received emails.", error = ex.Message });
            }
        }

        // Get all sent emails for a user
        [HttpGet("sent/{userId}")]
        public async Task<IActionResult> GetSentEmails(int userId)
        {
            try
            {
                var emails = await _mailService.GetSentEmailsAsync(userId);
                return Ok(emails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching sent emails.", error = ex.Message });
            }
        }

        // Get all draft emails for a user
        [HttpGet("drafts/{userId}")]
        public async Task<IActionResult> GetDraftEmails(int userId)
        {
            try
            {
                var emails = await _mailService.GetDraftEmailsAsync(userId);
                return Ok(emails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching draft emails.", error = ex.Message });
            }
        }

        // Add a new received email with optional attachments
        [HttpPost("received")]
        public async Task<IActionResult> AddReceivedEmail(
            [FromForm] int mailAccountId,
            [FromForm] string sender,
            [FromForm] string subject,
            [FromForm] string body,
            [FromForm] List<IFormFile> attachments)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
                {
                    return BadRequest(new { message = "Subject and body are required." });
                }

                var newMail = await _mailService.AddReceivedEmailAsync(mailAccountId, sender, subject, body, attachments);
                return CreatedAtAction(nameof(GetReceivedEmails), new { userId = mailAccountId }, newMail);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while saving the received email.", error = ex.Message });
            }
        }

        // Add a new sent email with optional attachments
        [HttpPost("sent")]
        public async Task<IActionResult> AddSentEmail(
            [FromForm] int mailAccountId,
            [FromForm] string subject,
            [FromForm] string body,
            [FromForm] List<IFormFile> attachments)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
                {
                    return BadRequest(new { message = "Subject and body are required." });
                }

                var newMail = await _mailService.AddSentEmailAsync(mailAccountId, subject, body, attachments);
                return CreatedAtAction(nameof(GetSentEmails), new { userId = mailAccountId }, newMail);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while sending the email.", error = ex.Message });
            }
        }

        // Add a new draft email with optional attachments
        [HttpPost("draft")]
        public async Task<IActionResult> AddDraftEmail(
            [FromForm] int mailAccountId,
            [FromForm] string subject,
            [FromForm] string body,
            [FromForm] List<IFormFile> attachments)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
                {
                    return BadRequest(new { message = "Subject and body are required." });
                }

                var newMail = await _mailService.AddDraftEmailAsync(mailAccountId, subject, body, attachments);
                return CreatedAtAction(nameof(GetDraftEmails), new { userId = mailAccountId }, newMail);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while saving the draft email.", error = ex.Message });
            }
        }
    }
}
