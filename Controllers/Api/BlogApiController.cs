using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mkinfotech.DTOs.Blog;
using Npgsql;
using System.Data;
using System.Text.RegularExpressions;

namespace mkinfotech.Controllers.Api
{
    // FIX 1: Use an explicit string route to guarantee matching without case-sensitivity or naming bugs
    [Route("api/BlogApi")]
    [ApiController]
    public class BlogApiController : BaseApiController
    {
        private readonly IConfiguration _config;

        public BlogApiController(IConfiguration config)
        {
            _config = config;
        }

        private IDbConnection Connection =>
            new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));

        // ==========================
        // SLUG GENERATOR (BACKEND)
        // ==========================
        private string GenerateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Guid.NewGuid().ToString("N")[..10];

            text = text.ToLower().Trim();
            text = Regex.Replace(text, @"\s+", "-");
            text = Regex.Replace(text, @"[^a-z0-9\-]", "");
            text = Regex.Replace(text, @"\-{2,}", "-");

            return text.Trim('-');
        }

        // ==========================
        // CREATE BLOG API
        // ==========================
        // FIX 2: Renamed param to break any automated "req" property binding conflicts


        // ==========================
        // CREATE BLOG API
        // ==========================
        [HttpPost("create")]
        public async Task<IActionResult> CreateBlog([FromBody] CreateBlogRequestDto model)
        {
            int authorId = GetUserId(); // FROM BASE CONTROLLER

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Validation failed",
                    errors = ModelState.Keys.SelectMany(k => ModelState[k].Errors.Select(e => e.ErrorMessage))
                });
            }

            using var conn = Connection;
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                // 1. Generate slug from title if slug not provided
                var slug = GenerateSlug(model.Slug ?? model.Title);

                // 2. Check duplicate slug
                var exists = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM mktech_blog_posts WHERE slug = @slug",
                    new { slug },
                    transaction);

                if (exists > 0)
                {
                    slug = slug + "-" + Guid.NewGuid().ToString("N")[..6];
                }

                // 3. Insert blog post (Anonymous property names match the @ variables exactly)
                var postId = await conn.ExecuteScalarAsync<int>(
                    @"INSERT INTO mktech_blog_posts
            (title, slug, short_description, content, category_id, author_id, image_url, status, created_at)
            VALUES
            (@Title, @Slug, @ShortDescription, @Content, @CategoryId, @AuthorId, @ImageUrl, 'DRAFT', NOW())
            RETURNING post_id;",
                    new
                    {
                        Title = model.Title,
                        Slug = slug,
                        ShortDescription = model.ShortDescription,
                        Content = model.Content,
                        CategoryId = model.CategoryId,
                        AuthorId = authorId,
                        ImageUrl = model.ImageUrl
                    },
                    transaction);

                // 4. Insert tags
                if (model.Tags != null && model.Tags.Count > 0)
                {
                    foreach (var tag in model.Tags)
                    {
                        var tagId = await conn.ExecuteScalarAsync<int?>(@"
                    SELECT tag_id FROM mktech_tags WHERE tag_name = @tag;
                ", new { tag }, transaction);

                        if (tagId == null)
                        {
                            tagId = await conn.ExecuteScalarAsync<int>(@"
                        INSERT INTO mktech_tags(tag_name)
                        VALUES (@tag)
                        RETURNING tag_id;
                    ", new { tag }, transaction);
                        }

                        await conn.ExecuteAsync(@"
                    INSERT INTO mktech_blog_post_tags(post_id, tag_id)
                    VALUES (@postId, @tagId)
                    ON CONFLICT DO NOTHING;
                ", new { postId, tagId }, transaction);
                    }
                }

                transaction.Commit();

                return Ok(new
                {
                    success = true,
                    message = "Blog created successfully",
                    postId,
                    slug
                });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }










        // ==========================
        // ADMIN BLOG LIST
        // ==========================






        [HttpGet("admin/list")]
        public async Task<IActionResult> GetAdminBlogs(
    int page = 1,
    int pageSize = 10,
    string? search = null,
    string? status = null)
        {
            try
            {
                using var conn = Connection;
                conn.Open();

                int userId = GetUserId(); // ✅ HERE YOU USE IT

                int offset = (page - 1) * pageSize;

                string sql = @"
            SELECT 
                p.post_id,
                p.title,
                p.slug,
                p.short_description,
                p.image_url,
                p.status,
                p.created_at,

                c.category_name,

                CONCAT(u.first_name, ' ', u.last_name) AS author_name

            FROM mktech_blog_posts p

            LEFT JOIN mktech_blog_categories c
                ON p.category_id = c.category_id

            LEFT JOIN mktech_user_register u
                ON p.author_id = u.user_id

            WHERE p.author_id = @userId   -- ✅ FILTER BY LOGIN USER

            AND (
                @search IS NULL
                OR p.title ILIKE '%' || @search || '%'
                OR p.slug ILIKE '%' || @search || '%'
            )

            AND (
                @status IS NULL
                OR p.status = @status
            )

            ORDER BY p.post_id DESC
            LIMIT @pageSize OFFSET @offset;
        ";

                var blogs = await conn.QueryAsync(sql, new
                {
                    userId,
                    search,
                    status,
                    pageSize,
                    offset
                });

                return Ok(new
                {
                    success = true,
                    message = "Admin blog list fetched successfully",
                    data = blogs
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("change-status/{id}")]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeBlogStatusDto model)
        {
            try
            {
                using var conn = Connection;

                await conn.ExecuteAsync(@"
            UPDATE mktech_blog_posts
            SET status = @Status,
                updated_at = NOW()
            WHERE post_id = @Id
        ", new { Id = id, Status = model.Status });

                return Ok(new
                {
                    success = true,
                    message = $"Blog moved to {model.Status}"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        // ==========================
        // PUBLIC BLOG LIST
        // ==========================
        [HttpGet("edit/{id}")]
        public async Task<IActionResult> GetBlogById(int id)
        {
            try
            {
                using var conn = Connection;
                conn.Open();

                int userId = GetUserId(); // Restrict to logged-in author for security

                // 1. Fetch the main blog post data
                string sql = @"
                    SELECT 
                        post_id AS PostId,
                        title AS Title,
                        slug AS Slug,
                        short_description AS ShortDescription,
                        content AS Content,
                        category_id AS CategoryId,
                        image_url AS ImageUrl,
                        status AS Status
                    FROM mktech_blog_posts 
                    WHERE post_id = @Id AND author_id = @UserId;";

                var blog = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id, UserId = userId });

                if (blog == null)
                {
                    return NotFound(new { success = false, message = "Blog post not found or unauthorized access." });
                }

                // 2. Fetch the tags associated with this blog post
                string tagsSql = @"
                    SELECT t.tag_name 
                    FROM mktech_tags t
                    JOIN mktech_blog_post_tags pt ON t.tag_id = pt.tag_id
                    WHERE pt.post_id = @Id;";

                var tags = (await conn.QueryAsync<string>(tagsSql, new { Id = id })).ToList();

                return Ok(new
                {
                    success = true,
                    message = "Blog details retrieved successfully",
                    data = new
                    {
                        blog.PostId,
                        blog.Title,
                        blog.Slug,
                        blog.ShortDescription,
                        blog.Content,
                        blog.CategoryId,
                        blog.ImageUrl,
                        blog.Status,
                        Tags = tags
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

    }
}