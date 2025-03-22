using FalconBackend.Data;
using FalconBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FalconBackend.Controllers
{
    [ApiController]
    [Route("api/test")]
    public class TestController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TestController(AppDbContext context)
        {
            _context = context;
        }

        // POST: /api/test/create-mailaccount/{userEmail}
        [HttpPost("create-mailaccount/{userEmail}")]
        public async Task<IActionResult> CreateRandomMailAccount(string userEmail)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null)
                return NotFound("AppUser not found.");

            var randomEmail = $"test_{Guid.NewGuid():N}@example.com";

            var newAccount = new MailAccount
            {
                AppUserEmail = userEmail,
                EmailAddress = randomEmail,
                Token = Guid.NewGuid().ToString(),
                Provider = MailAccount.MailProvider.Gmail,
                LastMailSync = DateTime.UtcNow,
                IsDefault = false
            };

            _context.MailAccounts.Add(newAccount);
            await _context.SaveChangesAsync();

            return Ok(new { message = "MailAccount created", mailAccountId = newAccount.MailAccountId });
        }

        // POST: /api/test/generate-mails/{mailAccountId}
        [HttpPost("generate-mails/{mailAccountId}")]
        public async Task<IActionResult> GenerateMixedMailsForAccount(string mailAccountId)
        {
            var account = await _context.MailAccounts.FirstOrDefaultAsync(m => m.MailAccountId == mailAccountId);
            if (account == null)
                return NotFound("MailAccount not found.");

            var sent = new List<MailSent>();
            var received = new List<MailReceived>();
            var now = DateTime.UtcNow;

            for (int i = 0; i < 70; i++)
            {
                var subject = $"Received Subject {i}";
                var body = $"This is a test body for received email {i}.";
                var senderEmail = $"sender{i}@domain.com";

                received.Add(new MailReceived
                {
                    MailAccountId = mailAccountId,
                    Sender = senderEmail,
                    Subject = subject,
                    Body = body,
                    TimeReceived = now.AddMinutes(-i),
                    IsRead = i % 2 == 0,
                    IsFavorite = i % 5 == 0,
                    Attachments = new List<Attachments>(),
                    Recipients = new List<Recipient>
            {
                new Recipient
                {
                    Email = $"user{i}@test.com"
                }
            },
                    MailTags = new List<MailTag>() // Optional placeholder if tags are needed later
                });
            }

            for (int i = 0; i < 30; i++)
            {
                var subject = $"Sent Subject {i}";
                var body = $"This is a test body for sent email {i}.";

                sent.Add(new MailSent
                {
                    MailAccountId = mailAccountId,
                    Subject = subject,
                    Body = body,
                    TimeSent = now.AddMinutes(-i),
                    IsFavorite = i % 4 == 0,
                    Attachments = new List<Attachments>(),
                    Recipients = new List<Recipient>
            {
                new Recipient
                {
                    Email = $"recipient{i}@test.com"
                }
            }
                });
            }

            _context.MailReceived.AddRange(received);
            _context.MailSent.AddRange(sent);
            await _context.SaveChangesAsync();

            return Ok("70 received and 30 sent mails created with recipients.");
        }






        // POST: /api/test/tag-mails/{mailAccountId}
        [HttpPost("tag-mails/{mailAccountId}")]
        public async Task<IActionResult> TagMailsWithRandomTags(string mailAccountId)
        {
            var mails = await _context.MailReceived
                .Where(m => m.MailAccountId == mailAccountId)
                .ToListAsync();

            if (mails.Count == 0)
                return NotFound("No mails found for this mail account.");

            var tags = await _context.Tags.ToListAsync();
            if (tags.Count == 0)
                return BadRequest("No tags exist in the system.");

            var rng = new Random();
            var mailTags = new List<MailTag>();

            foreach (var mail in mails)
            {
                int numberOfTags = rng.Next(1, 4); // 1 to 3 tags per mail
                var selectedTags = tags.OrderBy(_ => rng.Next()).Take(numberOfTags).ToList();

                foreach (var tag in selectedTags)
                {
                    mailTags.Add(new MailTag
                    {
                        MailReceivedId = mail.MailId,
                        TagId = tag.Id
                    });
                }
            }

            _context.MailTags.AddRange(mailTags);
            await _context.SaveChangesAsync();

            return Ok($"{mails.Count} received mails tagged with random tags.");
        }
    }
}
