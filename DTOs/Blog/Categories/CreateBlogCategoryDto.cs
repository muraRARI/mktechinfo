using System.ComponentModel.DataAnnotations;

namespace mkinfotech.DTOs.Blog.Categories
{
    public class BlogCategoryCreateDto
    {
        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        public string? Slug { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}