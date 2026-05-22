namespace mkinfotech.Models.Blog
{
    public class BlogCategory
    {
        public int CategoryId { get; set; }   // 👈 THIS IS PRIMARY KEY

        public string CategoryName { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}