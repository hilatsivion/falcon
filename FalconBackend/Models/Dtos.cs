using System.ComponentModel.DataAnnotations;

namespace FalconBackend.Models
{
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class SignUpRequest
    {
        public string FullName { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class AttachmentDto
    {
        public int AttachmentId { get; set; }
        public string Name { get; set; }
        public string FileType { get; set; }
        public float FileSize { get; set; }
        public string FilePath { get; set; }
    }

    public class RecipientDto
    {
        public int RecipientId { get; set; }
        public string Email { get; set; }
    }

    public class TagDto
    {
        public int TagId { get; set; }
        public string Name { get; set; }
        public string TagType { get; set; }
    }

    public class MailReceivedDto
    {
        public int MailId { get; set; }
        public string MailAccountId { get; set; }
        public string Sender { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public DateTime TimeReceived { get; set; }
        public bool IsRead { get; set; }
        public bool IsFavorite { get; set; }
        public List<TagDto> Tags { get; set; }
        public List<AttachmentDto> Attachments { get; set; }
        public List<RecipientDto> Recipients { get; set; }
    }

    public class MailSentDto
    {
        public int MailId { get; set; }
        public string MailAccountId { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public DateTime TimeSent { get; set; }
        public bool IsFavorite { get; set; }
        public List<AttachmentDto> Attachments { get; set; }
        public List<RecipientDto> Recipients { get; set; }
    }

    public class DraftDto
    {
        public int MailId { get; set; }
        public string MailAccountId { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public DateTime TimeCreated { get; set; }
        public bool IsSent { get; set; }
        public bool IsFavorite { get; set; }
        public List<AttachmentDto> Attachments { get; set; }
        public List<RecipientDto> Recipients { get; set; }
    }

    public class SaveUserTagsRequest
    {
        public List<string> Tags { get; set; }
    }

    public class CreateTagRequest
    {
        public string TagName { get; set; }
    }
    public class MailDeleteDto
    {
        public int MailId { get; set; }
        public string MailAccountId { get; set; }
    }

    public class MailReceivedPreviewDto
    {
        public int MailId { get; set; }
        public string MailAccountId { get; set; }
        public string Subject { get; set; }

        // Who sent the mail
        public string Sender { get; set; }

        public DateTime TimeReceived { get; set; }

        public List<string> Tags { get; set; } = new();
        public string BodySnippet { get; set; }
        public bool IsRead { get; set; }
        public bool IsFavorite { get; set; }
    }

    public class MailSentPreviewDto
    {
        public int MailId { get; set; }
        public string MailAccountId { get; set; }
        public string Subject { get; set; }

        // Comma-separated recipient emails
        public List<string> Recipients { get; set; } = new();

        public DateTime TimeSent { get; set; }

        public string BodySnippet { get; set; } 
        public bool IsFavorite { get; set; }
    }

    public class DraftPreviewDto
    {
        public int MailId { get; set; }
        public string MailAccountId { get; set; }
        public string Subject { get; set; }
        public DateTime TimeCreated { get; set; }
        public List<string> Recipients { get; set; } = new();
    }

    public class ToggleReadDto
    {
        public int MailId { get; set; }
        public bool IsRead { get; set; }
    }

    public class SendMailRequest
    {
        public string MailAccountId { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public List<string> Recipients { get; set; } = new();
    }

    public class UserMailAccountDto
    {
        public string MailAccountId { get; set; }
        public string EmailAddress { get; set; } 
        public bool IsDefault { get; set; }
    }

    public class MailSearchRequestDto
    {
        public string Keywords { get; set; }
        public string Sender { get; set; }
        public string Recipient { get; set; }
    }

    // --- Result DTO ---
    public class MailSearchResultDto
    {
        public int MailId { get; set; }
        public string MailAccountId { get; set; }
        public string Subject { get; set; }
        public string BodySnippet { get; set; }
        public DateTime Date { get; set; }
        public bool IsFavorite { get; set; }
        public string Type { get; set; } 
        public bool IsRead { get; set; }
        public string Sender { get; set; } 
        public List<string> Recipients { get; set; } = new(); 
        public List<string> Tags { get; set; } = new(); 
    }

    public class FavoriteEmailsDto
    {
        public List<MailReceivedPreviewDto> ReceivedFavorites { get; set; } = new List<MailReceivedPreviewDto>();
        public List<MailSentPreviewDto> SentFavorites { get; set; } = new List<MailSentPreviewDto>();
    }

    public class FilterFolderCreateDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(100)]
        public string? FolderColor { get; set; }
        public List<string> Keywords { get; set; } = new List<string>();
        public List<string> SenderEmails { get; set; } = new List<string>();
        public List<int>? TagIds { get; set; }
    }

    public class FilterFolderUpdateDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(100)]
        public string? FolderColor { get; set; }
        public List<string> Keywords { get; set; } = new List<string>();
        public List<string> SenderEmails { get; set; } = new List<string>();
        public List<int>? TagIds { get; set; }
    }

    public class FilterFolderDto
    {
        public int FilterFolderId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? FolderColor { get; set; }
        public List<string> Keywords { get; set; } = new List<string>();
        public List<string> SenderEmails { get; set; } = new List<string>();
        public List<TagDto> Tags { get; set; } = new List<TagDto>();
        public int TotalEmails { get; set; }
        public int NewEmailsCount { get; set; }
    }

    public class EmailSummaryDto
    {
        public int MailId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public DateTime TimeReceived { get; set; }
        public string BodyPreview { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public List<TagDto> Tags { get; set; } = new List<TagDto>();
    }

    public class LoginResponseDto
    {
        public string Token { get; set; }
        public string AiKey { get; set; }
    }

}
