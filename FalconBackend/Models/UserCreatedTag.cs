using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FalconBackend.Models
{
    public class UserCreatedTag : Tag
    {
        [ForeignKey("CreatedByUser")]
        [MaxLength(255)]
        [EmailAddress]
        public string CreatedByUserEmail { get; set; }

        public AppUser CreatedByUser { get; set; }
    }
}
