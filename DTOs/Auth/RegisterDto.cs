//public class RegisterDto
//{
//    public string FirstName { get; set; }
//    public string LastName { get; set; }
//    public string Email { get; set; }
//    public string CountryCode { get; set; }
//    public string MobileNumber { get; set; }
//    public string Password { get; set; }
//    public int RoleId { get; set; }
//}

using System.ComponentModel.DataAnnotations;

public class RegisterDto
{
    [Required]
    public string FirstName { get; set; }

    [Required]
    public string LastName { get; set; }
    [Required]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid email format")]

    public string Email { get; set; }

    [Required]
    public string CountryCode { get; set; }

    [Required]
    [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Mobile number must be 10 digits")]
    public string MobileNumber { get; set; }

    [Required]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; }
    //public object RoleId { get; internal set; }

    public int RoleId { get; set; } = 102; // default Client
}