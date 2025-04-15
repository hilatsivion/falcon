using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace FalconBackend.Models
{
    public class FilterFolderTag
    {
        [Key, Column(Order = 0)] 
        [ForeignKey("FilterFolder")]
        public int FilterFolderId { get; set; }
        public virtual FilterFolder FilterFolder { get; set; }

        [Key, Column(Order = 1)] 
        [ForeignKey("Tag")]
        public int TagId { get; set; }
        public virtual Tag Tag { get; set; }
    }
}