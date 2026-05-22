using Microsoft.AspNetCore.Mvc;
using Npgsql;

[ApiController]
[Route("api/[controller]")]
public class TestEmpController : ControllerBase
{
    private readonly IConfiguration _config;

    public TestEmpController(IConfiguration config)
    {
        _config = config;
    }

    // GET ALL
    [HttpGet]
    public IActionResult GetAll()
    {
        List<TestEmp> list = new List<TestEmp>();

        using (var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection")))
        {
            conn.Open();

            string query = "SELECT * FROM testemp";

            using (var cmd = new NpgsqlCommand(query, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    list.Add(new TestEmp
                    {
                        EmpId = Convert.ToInt32(reader["empid"]),
                        EmpName = reader["empname"].ToString(),
                        Email = reader["email"].ToString(),
                        MobileNo = reader["mobileno"].ToString(),
                        Salary = Convert.ToDecimal(reader["salary"]),
                        Department = reader["department"].ToString()
                    });
                }
            }
        }

        return Ok(list);
    }

    // GET BY ID
    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        TestEmp emp = null;

        using (var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection")))
        {
            conn.Open();

            string query = "SELECT * FROM testemp WHERE empid = @id";

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        emp = new TestEmp
                        {
                            EmpId = Convert.ToInt32(reader["empid"]),
                            EmpName = reader["empname"].ToString(),
                            Email = reader["email"].ToString(),
                            MobileNo = reader["mobileno"].ToString(),
                            Salary = Convert.ToDecimal(reader["salary"]),
                            Department = reader["department"].ToString()
                        };
                    }
                }
            }
        }

        if (emp == null) return NotFound();

        return Ok(emp);
    }

    // INSERT
    [HttpPost]
    public IActionResult Create(TestEmp emp)
    {
        using (var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection")))
        {
            conn.Open();

            string query = @"
                INSERT INTO testemp (empname, email, mobileno, salary, department)
                VALUES (@name, @email, @mobile, @salary, @dept)";

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@name", emp.EmpName);
                cmd.Parameters.AddWithValue("@email", emp.Email);
                cmd.Parameters.AddWithValue("@mobile", emp.MobileNo);
                cmd.Parameters.AddWithValue("@salary", emp.Salary);
                cmd.Parameters.AddWithValue("@dept", emp.Department);

                cmd.ExecuteNonQuery();
            }
        }

        return Ok("Inserted successfully");
    }

    // DELETE
    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        using (var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection")))
        {
            conn.Open();

            string query = "DELETE FROM testemp WHERE empid = @id";

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        return Ok("Deleted successfully");
    }
}