using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FalconBackend.Models;
using System.ComponentModel.DataAnnotations.Schema;

public class Tag
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [MaxLength(100)]
    public string TagName { get; set; }

    public ICollection<FavoriteTag> FavoriteTags { get; set; } = new List<FavoriteTag>();
    public ICollection<MailTag> MailTags { get; set; } = new List<MailTag>();
    public virtual ICollection<FilterFolderTag> FilterFolderTags { get; set; } = new List<FilterFolderTag>();
}
