using Dapper;
using Microsoft.AspNetCore.Mvc;
using mkinfotech.DTOs.Blog;
using Npgsql;
using System.Data;

namespace mkinfotech.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogPublicController : ControllerBase
    {
        private readonly IConfiguration _config;
   
        
        public BlogPublicController(IConfiguration config)
        {
            _config = config;
        }
        

        private IDbConnection Connection =>
            new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));


      
        // =========================
        // 1. CATEGORIES
        // =========================
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            using var conn = Connection;

            var sql = @"
                SELECT 
                    category_id,
                    category_name
                FROM mktech_blog_categories
                WHERE is_active = true
                ORDER BY category_name ASC";

            var data = await conn.QueryAsync(sql);

            return Ok(new
            {
                success = true,
                data
            });
        }

        // =========================
        // 2. BLOG LIST (MAIN UI)
        // =========================
        [HttpGet("published/list")]
        public async Task<IActionResult> GetBlogs(
            int page = 1,
            int pageSize = 6,
            string? search = null,
            int? categoryId = null)
        {
            try
            {
                using var db = Connection;

                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 6;
                if (pageSize > 50) pageSize = 50;

                int offset = (page - 1) * pageSize;

                var parameters = new DynamicParameters();
                parameters.Add("Limit", pageSize);
                parameters.Add("Offset", offset);

                var conditions = new List<string>
                {
                    "b.status = 'PUBLISHED'"
                };

                if (categoryId.HasValue)
                {
                    conditions.Add("b.category_id = @CategoryId");
                    parameters.Add("CategoryId", categoryId.Value);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    conditions.Add(@"
                        (b.title ILIKE @Search 
                        OR b.short_description ILIKE @Search
                        OR c.category_name ILIKE @Search)");

                    parameters.Add("Search", "%" + search.Trim() + "%");
                }

                string whereClause = $"WHERE {string.Join(" AND ", conditions)}";

                var sql = $@"
                    SELECT 
                        b.post_id AS id,
                        b.title,
                        b.short_description,
                        b.image_url,
                        b.slug,
                        b.created_at,
                        c.category_name,
                        u.first_name || ' ' || u.last_name AS author_name,

                        -- ✅ REAL LIKE COUNT FROM TABLE
                        (SELECT COUNT(*) 
                         FROM mktech_blog_likes l 
                         WHERE l.post_id = b.post_id) AS likes_count,

                        COUNT(*) OVER() AS total_count
                    FROM mktech_blog_posts b
                    LEFT JOIN mktech_blog_categories c 
                        ON b.category_id = c.category_id
                    LEFT JOIN mktech_user_register u 
                        ON b.author_id = u.user_id
                    {whereClause}
                    ORDER BY b.created_at DESC
                    LIMIT @Limit OFFSET @Offset;
                ";

                var rows = (await db.QueryAsync(sql, parameters)).ToList();

                int totalRecords = rows.Count > 0
                    ? Convert.ToInt32(rows.First().total_count)
                    : 0;

                var blogs = rows.Select(x => new
                {
                    id = x.id,
                    title = x.title,
                    shortDescription = x.short_description,
                    imageUrl = x.image_url,
                    slug = x.slug,
                    categoryName = x.category_name,
                    authorName = x.author_name,
                    likesCount = x.likes_count ?? 0,
                    createdAt = x.created_at
                }).ToList();

                return Ok(new
                {
                    success = true,
                    data = blogs,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalRecords,
                        totalPages = (int)Math.Ceiling((double)totalRecords / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // =========================
        // 3. BLOG DETAIL (READ PAGE)
        // =========================


        // =========================
        // 4. LIKE API
        // =========================

        // =========================
        private string GetUserIp()
        {
            return HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                   ?? HttpContext.Connection.RemoteIpAddress?.ToString();
        }



        [HttpGet("{slug}")]
        [HttpGet("slug/{slug}")]

        public async Task<IActionResult> GetBlogBySlug(string slug)
        {
            using var conn = Connection;

            var sql = @"
    SELECT 
        b.post_id AS Id,
        b.title AS Title,
        b.content AS Content,
        b.image_url AS ImageUrl,
        b.slug AS Slug,
        b.created_at AS CreatedAt,
        COALESCE(c.category_name, 'General') AS CategoryName,
        u.first_name || ' ' || u.last_name AS AuthorName,
        (SELECT COUNT(*) 
         FROM mktech_blog_likes l 
         WHERE l.post_id = b.post_id) AS LikesCount,
        b.views_count AS ViewsCount
    FROM mktech_blog_posts b
    LEFT JOIN mktech_blog_categories c 
        ON b.category_id = c.category_id
    LEFT JOIN mktech_user_register u 
        ON b.author_id = u.user_id
    WHERE b.slug = @Slug
    LIMIT 1;
";

            var blog = await conn.QueryFirstOrDefaultAsync<BlogDto>(sql, new { Slug = slug });

            if (blog == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Blog not found"
                });
            }

            // 👉 Increase view count
            var updateSql = @"
    UPDATE mktech_blog_posts
    SET views_count = COALESCE(views_count, 0) + 1
    WHERE slug = @Slug;
";

            await conn.ExecuteAsync(updateSql, new { Slug = slug });

            return Ok(new
            {
                success = true,
                data = blog
            });
        }



        [HttpPost("like/{id}")]
        public async Task<IActionResult> LikeBlog(int id)
        {
            using var conn = Connection;

            var userIp = GetUserIp();

            // check if already liked
            var exists = await conn.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*) 
                FROM mktech_blog_likes
                WHERE post_id = @PostId AND user_ip = @UserIp;
            ", new { PostId = id, UserIp = userIp });

            if (exists > 0)
            {
                // unlike
                await conn.ExecuteAsync(@"
                    DELETE FROM mktech_blog_likes
                    WHERE post_id = @PostId AND user_ip = @UserIp;
                ", new { PostId = id, UserIp = userIp });
            }
            else
            {
                // like
                await conn.ExecuteAsync(@"
                    INSERT INTO mktech_blog_likes (post_id, user_ip)
                    VALUES (@PostId, @UserIp);
                ", new { PostId = id, UserIp = userIp });
            }

            var likes = await conn.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*) 
                FROM mktech_blog_likes
                WHERE post_id = @PostId;
            ", new { PostId = id });

            return Ok(new
            {
                success = true,
                likesCount = likes,
                message = exists > 0 ? "Unliked" : "Liked"
            });
        }



        //    [HttpPost("like/{id}")]
        //    public async Task<IActionResult> LikeBlog(int id)
        //    {
        //        using var conn = Connection;

        //        string userIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        //        // 1. Check already liked
        //        var checkSql = @"
        //    SELECT COUNT(*) 
        //    FROM mktech_blog_likes
        //    WHERE post_id = @PostId AND user_ip = @UserIp;
        //";

        //        var exists = await conn.ExecuteScalarAsync<int>(checkSql, new
        //        {
        //            PostId = id,
        //            UserIp = userIp
        //        });

        //        if (exists > 0)
        //        {
        //            // 👉 UNLIKE (remove like)
        //            var deleteSql = @"
        //        DELETE FROM mktech_blog_likes
        //        WHERE post_id = @PostId AND user_ip = @UserIp;
        //    ";

        //            await conn.ExecuteAsync(deleteSql, new
        //            {
        //                PostId = id,
        //                UserIp = userIp
        //            });
        //        }
        //        else
        //        {
        //            // 👉 LIKE
        //            var insertSql = @"
        //        INSERT INTO mktech_blog_likes (post_id, user_ip)
        //        VALUES (@PostId, @UserIp);
        //    ";

        //            await conn.ExecuteAsync(insertSql, new
        //            {
        //                PostId = id,
        //                UserIp = userIp
        //            });
        //        }

        //        // 2. Get updated count
        //        var countSql = @"
        //    SELECT COUNT(*) 
        //    FROM mktech_blog_likes
        //    WHERE post_id = @PostId;
        //";

        //        var likesCount = await conn.ExecuteScalarAsync<int>(countSql, new { PostId = id });

        //        return Ok(new
        //        {
        //            success = true,
        //            likesCount,
        //            message = exists > 0 ? "Unliked" : "Liked"
        //        });
        //    }
    }
}


