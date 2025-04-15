using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FalconBackend.Models
{
    public class FilterFolder
    {
        [Key] 
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] 
        public int FilterFolderId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)] 
        public string? FolderColor { get; set; }

        public List<string> Keywords { get; set; } = new List<string>();
        public List<string> SenderEmails { get; set; } = new List<string>();

        [Required] 
        [ForeignKey("AppUser")] 
        [MaxLength(255)]
        [EmailAddress] 
        public string AppUserEmail { get; set; } 

        public virtual AppUser AppUser { get; set; } 

        public virtual ICollection<FilterFolderTag> FilterFolderTags { get; set; } = new List<FilterFolderTag>();
    }
}