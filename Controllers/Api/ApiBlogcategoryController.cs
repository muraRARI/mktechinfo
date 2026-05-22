using Dapper;
using Microsoft.AspNetCore.Mvc;
using mkinfotech.DTOs.Blog.Categories;
using Npgsql;
using System.Data;

namespace mkinfotech.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApiBlogcategoryController : ControllerBase
    {
        private readonly IConfiguration _config;

        public ApiBlogcategoryController(IConfiguration config)
        {
            _config = config;
        }

        private IDbConnection Connection =>
            new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));

        // ================= GET ALL =================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            using var connection = Connection;

            // FIX: Wrapped aliases in double quotes "" so PostgreSQL doesn't force them into lowercase!
            var sql = @"SELECT 
                            category_id AS ""CategoryId"",
                            category_name AS ""CategoryName"",
                            slug AS ""Slug"",
                            description AS ""Description"",
                            is_active AS ""IsActive"",
                            created_at AS ""CreatedAt"",
                            updated_at AS ""UpdatedAt""
                        FROM mktech_blog_categories
                        ORDER BY category_id DESC";

            var data = await connection.QueryAsync<BlogCategoryResponseDto>(sql);

            return Ok(data);
        }

        // ================= CREATE =================
        [HttpPost]
        public async Task<IActionResult> Create(BlogCategoryCreateDto dto)
        {
            using var connection = Connection;

            var slug = GenerateSlug(dto.CategoryName);

            var checkSql = "SELECT COUNT(1) FROM mktech_blog_categories WHERE slug = @Slug";
            var exists = await connection.ExecuteScalarAsync<bool>(checkSql, new { Slug = slug });

            if (exists)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Category already exists"
                });
            }

            var insertSql = @"
                INSERT INTO mktech_blog_categories
                (category_name, slug, description, is_active, created_at)
                VALUES
                (@CategoryName, @Slug, @Description, @IsActive, NOW())
                RETURNING category_id;
            ";

            var id = await connection.ExecuteScalarAsync<int>(insertSql, new
            {
                dto.CategoryName,
                Slug = slug,
                dto.Description,
                dto.IsActive
            });

            return Ok(new
            {
                success = true,
                message = "Category created successfully",
                categoryId = id
            });
        }

        private string GenerateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            return text.ToLower()
                .Trim()
                .Replace(" ", "-")
                .Replace("--", "-");
        }
    }

    public class BlogCategoryResponseDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}