using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using FalconBackend.Services;
using System.IO;

namespace FalconBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileController : ControllerBase
    {
        private readonly FileStorageService _fileStorageService;
        private readonly IWebHostEnvironment _environment;

        public FileController(FileStorageService fileStorageService, IWebHostEnvironment environment)
        {
            _fileStorageService = fileStorageService;
            _environment = environment;
        }

        [HttpGet("attachments/{*filePath}")]
        public IActionResult GetAttachment(string filePath)
        {
            try
            {
                // Get the current user's email from the JWT token
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User not authenticated");
                }

                // Validate that the file path belongs to the authenticated user
                if (!filePath.StartsWith($"User_{userEmail}/"))
                {
                    return Forbid("Access denied to this file");
                }

                // Construct the full file path
                var storagePath = Path.Combine(_environment.ContentRootPath, "Storage");
                var fullFilePath = Path.Combine(storagePath, filePath);

                // Check if file exists
                if (!System.IO.File.Exists(fullFilePath))
                {
                    return NotFound("File not found");
                }

                // Get file info
                var fileInfo = new FileInfo(fullFilePath);
                var contentType = GetContentType(fileInfo.Extension);

                // Read and return the file
                var fileBytes = System.IO.File.ReadAllBytes(fullFilePath);
                return File(fileBytes, contentType, fileInfo.Name);
            }
            catch (Exception ex)
            {
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