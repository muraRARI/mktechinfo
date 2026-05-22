namespace mkinfotech.DTOs.Blog
{
    public class BlogDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public string Slug { get; set; }
        public DateTime CreatedAt { get; set; }

        public string CategoryName { get; set; }
        public string AuthorName { get; set; }
        public int LikesCount { get; set; }
        public int ViewsCount { get; set; }
    }
    
}
