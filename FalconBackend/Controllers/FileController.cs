using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using FalconBackend.Services;
using System.IO;
using FalconBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace FalconBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileController : ControllerBase
    {
        private readonly FileStorageService _fileStorageService;
        private readonly IWebHostEnvironment _environment;
        private readonly AppDbContext _context;

        public FileController(FileStorageService fileStorageService, IWebHostEnvironment environment, AppDbContext context)
        {
            _fileStorageService = fileStorageService;
            _environment = environment;
            _context = context;
        }

        [HttpGet("attachment/{mailId}/{attachmentIndex}")]
        public async Task<IActionResult> GetAttachmentByMailId(string mailId, int attachmentIndex)
        {
            try
            {
                // Get the current user's email from the JWT token
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User not authenticated");
                }

                // Find the mail by mailId and include attachments
                var mail = await _context.Mails
                    .Include(m => m.Attachments)
                    .Include(m => m.MailAccount)
                    .Where(m => m.MailId.ToString() == mailId && m.MailAccount.AppUserEmail == userEmail)
                    .FirstOrDefaultAsync();

                if (mail == null)
                {
                    return NotFound("Mail not found or access denied");
                }

                if (attachmentIndex < 0 || attachmentIndex >= mail.Attachments.Count)
                {
                    return NotFound("Attachment not found");
                }

                var attachment = mail.Attachments.Skip(attachmentIndex).First();
                
                // Get the file path from the attachment
                var filePath = attachment.FilePath;
                var fileName = attachment.Name;

                if (string.IsNullOrEmpty(filePath))
                {
                    return NotFound("File path not found");
                }

                // Construct the full file path
                var storagePath = Path.Combine(_environment.ContentRootPath, "Storage");
                var fullFilePath = Path.Combine(storagePath, filePath);

                // Check if file exists
                if (!System.IO.File.Exists(fullFilePath))
                {
                    return NotFound($"File not found: {filePath}");
                }

                // Get file info
                var fileInfo = new FileInfo(fullFilePath);
                var contentType = GetContentType(fileInfo.Extension);

                // Read and return the file
                var fileBytes = await System.IO.File.ReadAllBytesAsync(fullFilePath);
                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAttachmentByMailId: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        

        private string GetContentType(string extension)
        {
            return extension.ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                ".zip" => "application/zip",
                ".rar" => "application/vnd.rar",
                ".7z" => "application/x-7z-compressed",
                _ => "application/octet-stream"
            };
        }
    }
} 