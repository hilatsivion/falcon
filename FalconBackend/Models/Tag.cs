namespace FalconBackend.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public string TagName { get; set; }

        // Relationships
        public ICollection<FavoriteTag> FavoriteTags { get; set; }
    }
}
