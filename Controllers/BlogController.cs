//using Dapper;
//using Microsoft.AspNetCore.Mvc;
//using Npgsql;
//using System.Data;
//using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

//namespace mkinfotech.Controllers
//{
//    public class BlogController : Controller
//    {

//        private readonly IConfiguration _config;

//        public BlogController(IConfiguration config)
//        {
//            _config = config;
//        }

//        private IDbConnection Connection =>
//            new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
//        public IActionResult Index()
//        {
//            return View();
//        }
//        //🔥 THIS FIXES YOUR 404
//        [HttpGet("{slug}")]
//        public IActionResult Read(string slug)
//        {
//            return View();
//        }

//    }
//}

using Microsoft.AspNetCore.Mvc;
using mkinfotech.DTOs.Blog;
using Npgsql;
using System.Data;
using System.Text.Json;

[Route("blog")]
public class BlogController : Controller
{


 
    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }


    //[HttpGet("{slug}")]
    //public IActionResult Read(string slug)
    //{
    //    return View();
    //}

    [HttpGet("{slug}")]
    public async Task<IActionResult> Read(string slug)
    {
        using var client = new HttpClient();

        var response = await client.GetAsync(
            $"https://localhost:44394/api/BlogPublic/slug/{slug}"
        );

        if (!response.IsSuccessStatusCode)
            return NotFound();

        var json = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<ApiResponseDto<BlogDto>>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        return View(result?.Data);
    }


}