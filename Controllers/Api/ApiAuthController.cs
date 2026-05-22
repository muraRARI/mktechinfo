using BCrypt.Net;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace mkinfotech.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApiAuthController : ControllerBase
    {
        private readonly IConfiguration _config;

        public ApiAuthController(IConfiguration config)
        {
            _config = config;
        }

        private IDbConnection Connection =>
            new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));

        // ================= REGISTER =================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            using var db = Connection;

            var exists = await db.QueryFirstOrDefaultAsync(@"
                SELECT 1 FROM mktech_user_register 
                WHERE email = @Email OR mobile_number = @MobileNumber",
                new { dto.Email, dto.MobileNumber });

            if (exists != null)
                return BadRequest(new { message = "User already exists" });

            await db.ExecuteAsync(@"
                INSERT INTO mktech_user_register
                (first_name, last_name, email, country_code, mobile_number, password_hash, role_id)
                VALUES
                (@FirstName, @LastName, @Email, @CountryCode, @MobileNumber, @PasswordHash, @RoleId)",
                new
                {
                    dto.FirstName,
                    dto.LastName,
                    dto.Email,
                    dto.CountryCode,
                    dto.MobileNumber,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    RoleId = dto.RoleId
                });

            return Ok(new { message = "Registered successfully" });
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                using var db = Connection;

                var user = await db.QueryFirstOrDefaultAsync(@"
            SELECT 
                user_id,
                first_name,
                email,
                password_hash,
                role_id
            FROM mktech_user_register
            WHERE email = @Email",
                    new { dto.Email });

                if (user == null)
                    return Unauthorized(new { message = "Invalid credentials" });

                // SAFE NULL CHECK
                string passwordHash = user.password_hash?.ToString();

                if (string.IsNullOrEmpty(passwordHash))
                    return Unauthorized(new { message = "Invalid credentials" });

                bool valid = BCrypt.Net.BCrypt.Verify(dto.Password, passwordHash);

                if (!valid)
                    return Unauthorized(new { message = "Invalid credentials" });

                var token = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();

                await db.ExecuteAsync(@"
            INSERT INTO user_refresh_tokens (user_id, refresh_token, expires_at)
            VALUES (@UserId, @Token, @Expiry)",
                    new
                    {
                        UserId = user.user_id,
                        Token = refreshToken,
                        Expiry = DateTime.UtcNow.AddDays(7)
                    });

                // ✅ COOKIE FIX FOR PRODUCTION + CROSS DOMAIN
                Response.Cookies.Append("AccessToken", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddMinutes(30),
                    Path = "/"
                });

                Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddDays(7),
                    Path = "/"
                });


                return Ok(new
                {
                    message = "Login successful",
                    user = new
                    {
                        id = user.user_id,
                        name = user.first_name,
                        email = user.email,
                        role = user.role_id
                    },
                    token = token,
                    refreshToken = refreshToken
                });
            }
            catch (Exception ex)
            {
                // IMPORTANT: this will show real error in logs
                return StatusCode(500, new
                {
                    message = "Server error during login",
                    error = ex.Message
                });
            }
        }
        // ================= JWT =================
        private string GenerateJwtToken(dynamic user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.user_id.ToString()),
                new Claim(ClaimTypes.Email, user.email.ToString()),
                new Claim(ClaimTypes.Name, user.first_name.ToString()),
                new Claim(ClaimTypes.Role,
                    user.role_id == 100 ? "SuperAdmin" :
                    user.role_id == 101 ? "Admin" :
                    "Client")
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        [Authorize]
        [HttpGet("redirect")]
        public IActionResult RedirectAfterLogin()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var route = role switch
            {

                "SuperAdmin" => "/Superadmin/Dashboard/",

                "Admin" => "/Admin/Dashboard/",



                _ => "/Client/Dashboard"
            };

            return Ok(new { url = route });
        }


        private string GenerateRefreshToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }
    }
}
