using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace mkinfotech.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestnewController : ControllerBase
    {
        private readonly IConfiguration _config;

        public TestnewController(IConfiguration config)
        {
            _config = config;
        }

        // GET: api/Testnew/employees
        [HttpGet("employees")]
        public IActionResult GetEmployees()
        {
            var list = new List<object>();

            using (var conn = new NpgsqlConnection(
                _config.GetConnectionString("DefaultConnection")))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand("SELECT id, name FROM emp", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new
                        {
                            id = reader.GetInt32(0),
                            name = reader.GetString(1)
                        });
                    }
                }
            }

            return Ok(list);
        }
    }
}