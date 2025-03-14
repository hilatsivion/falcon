using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FalconBackend.Services
{
    /// <summary>
    /// Manages storing and retrieving email attachments in a structured file system.
    /// </summary>
    public class FileStorageService
    {
        private readonly string _basePath = "Storage"; // Root directory for storing files

        public FileStorageService()
        {
            if (!Directory.Exists(_basePath))
                Directory.CreateDirectory(_basePath);
        }

        // Saves an attachment with Mail ID and hash in the file name
        public async Task<string> SaveAttachmentAsync(IFormFile file, int userId, int mailAccountId, int mailId, string emailType)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null.");

            if (emailType != "Sent" && emailType != "Received" && emailType != "Drafts")
                throw new ArgumentException("Invalid email type. Use 'Sent', 'Received', or 'Drafts'.");

            string userFolder = Path.Combine(_basePath, $"User_{userId}");
            string accountFolder = Path.Combine(userFolder, $"Account_{mailAccountId}");
            string emailTypeFolder = Path.Combine(accountFolder, emailType, "Attachments");

            Directory.CreateDirectory(emailTypeFolder);

            // Generate a unique hash for each file
            string fileHash = GenerateFileHash(file.FileName + DateTime.UtcNow.ToString());

            // Append Mail ID and hash to the filename
            string fileName = $"{mailId}_{fileHash}_{file.FileName}";
            string filePath = Path.Combine(emailTypeFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return filePath; // Return path for database storage
        }

        // Generates a unique short hash for filenames
        private string GenerateFileHash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 8); // Use first 8 characters
            }
        }
    }
}
