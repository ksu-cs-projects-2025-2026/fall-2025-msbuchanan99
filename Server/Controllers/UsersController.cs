using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly ThreadfolioContext _context;

        public UsersController(ThreadfolioContext context)
        {
            _context = context;
        }

        #region Admin Routes

        // GET: Users
        //public async Task<IActionResult> Index()
        //{
        //    return View(await _context.Users.ToListAsync());
        //}

        #endregion

        #region User Routes

        // POST: api/users/create
        //HashPassword that is brought in isn't actually Hashed
        [HttpPost("create")]
        public async Task<IActionResult> Create([Bind("Username,HashPassword")] User user)
        {
            if (ModelState.IsValid)
            {
                //Check for duplicate usernames
                if (await _context.Users.AnyAsync(u => u.Username.ToLower() == user.Username.ToLower()))
                    return BadRequest("That Username already exists.");

                //Hash the password
                var hasher = new PasswordHasher<User>();
                user.HashPassword = hasher.HashPassword(user, user.HashPassword);

                //Set Settings
                user.Role = "User";
                user.CreatedOn = DateTime.UtcNow;
                user.LastModified = DateTime.UtcNow;

                //Save new user
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return Ok();
            }
            return BadRequest("Model State Invalid.");
        }

        // PUT: api/users/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Edit(int id, Dictionary<string, string> user)
        {
            User? currentUser;
            if (!UserExists(id, out currentUser))
                return NotFound();

            if (ModelState.IsValid)
            {
                //Check if username needs to be changed
                if (user.ContainsKey("Username"))
                {
                    //Check if username matches
                    if (user["Username"] != currentUser!.Username) currentUser.Username = user["Username"];
                }

                //Check if password needs to be changed
                if(user.ContainsKey("Password"))
                {
                    //Check if passwords match
                    var hasher = new PasswordHasher<User>();
                    var verifyResult = hasher.VerifyHashedPassword(currentUser, currentUser.HashPassword, user["Password"]);
                    if (verifyResult == PasswordVerificationResult.Failed)
                    {
                        currentUser.HashPassword = hasher.HashPassword(currentUser, user["Password"]);
                    }
                }

                currentUser!.LastModified = DateTime.UtcNow;

                //Save Changes
                await _context.SaveChangesAsync();

                //refresh login cookies
                await ReissueCookieForSelfEdit(currentUser);

                //return
                return Ok(new UserDTO(currentUser.Id, currentUser.Username, currentUser.Role.ToString()));
            }
            return BadRequest("Model State not Valid");
        }

        // DELETE: api/users/
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            User? user;
            if (UserExists(id, out user))
            {
                _context.Users.Remove(user);

                var userFlosses = await _context.UserFloss.Where(uf => uf.UserId == id).ToListAsync();
                if (userFlosses != null && userFlosses!.Count != 0)
                {
                    _context.UserFloss.RemoveRange(userFlosses);
                }

                //var projects = await _context.Projects.Where(p => p.UserId == id).ToListAsync();
                //if (projects != null && projects!.Count != 0)
                //{
                //    _context.UserProjects.RemoveRange(projects);
                //}
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("{id:int}/projects")]
        public async Task<IActionResult> GetProjects(int id)
        {
            User? user;
            if(UserExists(id, out user))
            {
                return Ok(user!.Projects);
            }
            else
            {
                return NotFound("User not found");
            }
        }

        [HttpGet("{id:int}/floss")]
        public async Task<IActionResult> GetFloss(int id)
        {
            User? user;
            if (UserExists(id, out user))
            {
                return Ok(user!.Floss);
            }
            else
            {
                return NotFound("User not found");
            }
        }

        #endregion

        private bool UserExists(int id, out User? user)
        {
            user = _context.Users.Find(id);
            return user is not null;
        }

        private async Task ReissueCookieForSelfEdit(User user)
        {
            var editedSelf = User.FindFirst(ClaimTypes.NameIdentifier)?.Value == user.Id.ToString();
            if (editedSelf)
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new(ClaimTypes.Name, user.Username),
                    new(ClaimTypes.Role, user.Role)
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity));
            }
        }

        public record UserDTO(int Id, string Username, string Role);
    }
}
