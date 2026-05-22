using System.ComponentModel.DataAnnotations;

namespace mkinfotech.DTOs.Blog
{
    public class CreateBlogRequestDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Slug { get; set; }  // optional (generate in backend if null)

        [Required]
        public string ShortDescription { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }

        public string? ImageUrl { get; set; }

        public List<string> Tags { get; set; } = new();
    }
}