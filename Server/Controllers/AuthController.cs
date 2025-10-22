using Microsoft.AspNetCore.Mvc;
using Server.Data;
using Server.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly ThreadfolioContext _dbContext;
        public AuthController(ThreadfolioContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginInfo body)
        {
            User? user = _dbContext.Users
                .FirstOrDefault(u => u.Username == body.Username && u.Password == body.Password);
            if (user == null) return Unauthorized("Invalid Credentials");

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, ((UserType)user.Role).ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            return Ok();
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok();
        }

        [HttpGet("me")]
        public IActionResult Me()
        {
            if (!(User?.Identity?.IsAuthenticated ?? false)) return Unauthorized();
            return Ok(new
            {
                Id = User.FindFirstValue(ClaimTypes.NameIdentifier),
                Username = User.Identity!.Name,
                Role = User.FindFirstValue(ClaimTypes.Role)
            });
        }
    }

    public class LoginInfo
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
