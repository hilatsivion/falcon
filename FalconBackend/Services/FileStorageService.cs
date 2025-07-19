using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace FalconBackend.Services
{
    /// Handles storing email attachments in a structured file system.
    public class FileStorageService
    {
        private readonly string _basePath;
        public FileStorageService(IWebHostEnvironment environment) 
        {
            // Use ContentRootPath to get the project's base directory
            _basePath = Path.Combine(environment.ContentRootPath, "Storage"); 

            try
            {
                if (!Directory.Exists(_basePath))
                    Directory.CreateDirectory(_basePath);
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to create storage directory at {_basePath}. Check permissions.", ex);
            }
        }

        /// Saves an attachment in the correct folder for a user's email.
        public async Task<string> SaveAttachmentAsync(IFormFile file, string userEmail, string mailAccountId, string emailType)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null.");

            if (emailType != "Sent" && emailType != "Received" && emailType != "Drafts")
                throw new ArgumentException("Invalid email type. Use 'Sent', 'Received', or 'Drafts'.");

            string userFolder = Path.Combine(_basePath, $"User_{SanitizeFileName(userEmail)}");
            string accountFolder = Path.Combine(userFolder, $"Account_{SanitizeFileName(mailAccountId)}");
            string emailTypeFolder = Path.Combine(accountFolder, emailType, "Attachments");

            try
            {
                Directory.CreateDirectory(emailTypeFolder);

                string fileName = $"{Guid.NewGuid()}_{SanitizeFileName(file.FileName)}";
                string filePath = Path.Combine(emailTypeFolder, fileName);

                // Ensure filename is unique
                while (File.Exists(filePath))
                {
                    fileName = $"{Guid.NewGuid()}_{SanitizeFileName(file.FileName)}";
                    filePath = Path.Combine(emailTypeFolder, fileName);
                }

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return filePath; // Return the file path for database storage
            }
            catch (Exception ex)
            {
                throw new IOException("Failed to save attachment.", ex);
            }
        }

        /// <summary>
        /// Saves an attachment from byte array (for Outlook sync attachments)
        /// </summary>
        public async Task<string> SaveAttachmentAsync(byte[] fileContent, string fileName, string userEmail, string mailAccountId, string emailType)
        {
            if (fileContent == null || fileContent.Length == 0)
                throw new ArgumentException("File content is empty or null.");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name is required.");

            if (emailType != "Sent" && emailType != "Received" && emailType != "Drafts")
                throw new ArgumentException("Invalid email type. Use 'Sent', 'Received', or 'Drafts'.");

            string userFolder = Path.Combine(_basePath, $"User_{SanitizeFileName(userEmail)}");
            string accountFolder = Path.Combine(userFolder, $"Account_{SanitizeFileName(mailAccountId)}");
            string emailTypeFolder = Path.Combine(accountFolder, emailType, "Attachments");

            try
            {
                Directory.CreateDirectory(emailTypeFolder);

                string sanitizedFileName = $"{Guid.NewGuid()}_{SanitizeFileName(fileName)}";
                string filePath = Path.Combine(emailTypeFolder, sanitizedFileName);

                // Ensure filename is unique
                while (File.Exists(filePath))
                {
                    sanitizedFileName = $"{Guid.NewGuid()}_{SanitizeFileName(fileName)}";
                    filePath = Path.Combine(emailTypeFolder, sanitizedFileName);
                }

                await File.WriteAllBytesAsync(filePath, fileContent);

                return filePath; // Return the file path for database storage
            }
            catch (Exception ex)
            {
                throw new IOException("Failed to save attachment from byte array.", ex);
            }
        }

        /// <summary>
        /// Sanitizes file names to prevent issues with special characters.
        /// </summary>
        private string SanitizeFileName(string input)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                input = input.Replace(c, '_');
            }
            return input;
        }

        /// Deletes a previously saved attachment from the file system.
        public async Task<bool> DeleteAttachmentAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    throw new ArgumentException("File path is empty.");

                if (!File.Exists(filePath))
                    return false;

                await Task.Run(() => File.Delete(filePath));
                return true;
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to delete file at {filePath}", ex);
            }
        }
    }
}
