using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FalconBackend.Services
{
    /// <summary>
    /// Handles storing email attachments in a structured file system.
    /// </summary>
    public class FileStorageService
    {
        private readonly string _basePath = "Storage"; // Root directory for storing files

        public FileStorageService()
        {
            if (!Directory.Exists(_basePath))
                Directory.CreateDirectory(_basePath);
        }

        /// <summary>
        /// Saves an attachment in the correct folder for a user's email.
        /// </summary>
        public async Task<string> SaveAttachmentAsync(IFormFile file, int userId, int mailAccountId, string emailType)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null.");

            if (emailType != "Sent" && emailType != "Received" && emailType != "Drafts")
                throw new ArgumentException("Invalid email type. Use 'Sent', 'Received', or 'Drafts'.");

            string userFolder = Path.Combine(_basePath, $"User_{userId}");
            string accountFolder = Path.Combine(userFolder, $"Account_{mailAccountId}");
            string emailTypeFolder = Path.Combine(accountFolder, emailType, "Attachments");

            Directory.CreateDirectory(emailTypeFolder);

            string fileName = $"{Guid.NewGuid()}_{file.FileName}";
            string filePath = Path.Combine(emailTypeFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return filePath; // Return the file path for database storage
        }
    }
}
