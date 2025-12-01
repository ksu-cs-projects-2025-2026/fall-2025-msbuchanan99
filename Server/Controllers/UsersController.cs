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

        [HttpGet("admin")]
        public async Task<IActionResult> GetAllUsersAsync_Admin()
        {
            try
            {
                List<User> users = await _context.Users.ToListAsync();
                return Ok(users);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("admin")]
        public async Task<IActionResult> CreateUserAsync_Admin(User newUser)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newUser.Username)) return BadRequest("Username cannot be empty");
                if (string.IsNullOrWhiteSpace(newUser.Role)) return BadRequest("Role cannot be empty");

                var hasUsername = await _context.Users.AnyAsync(u => u.Username == newUser.Username);
                if (hasUsername) return BadRequest("Username already exists.");

                var hasher = new PasswordHasher<User>();
                newUser.HashPassword = hasher.HashPassword(newUser, "P455W0RD");

                await _context.Users.AddAsync(newUser);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPut("admin/{id:int}")]
        public async Task<IActionResult> UpdateUserAsync_Admin(int id, User updateUser)
        {
            try
            {
                User? current = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
                if (current is null) return NotFound($"User with Id {id} not found.");

                if (string.IsNullOrWhiteSpace(updateUser.Username)) return BadRequest("Username cannot be empty");
                if (string.IsNullOrWhiteSpace(updateUser.Role)) return BadRequest("Role cannot be empty");
                if (!(updateUser.Role == "Admin" || updateUser.Role == "User")) return BadRequest("Incorrect Role Information.");
                if (updateUser.LastModified <= updateUser.CreatedOn) return BadRequest("LastModified cannot be before creationDate");

                updateUser.Id = id;
                _context.Entry(updateUser).State = EntityState.Modified;

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete("admin/{id:int}")]
        public async Task<IActionResult> DeleteUserAsync_Admin(int id)
        {
            try
            {
                User? user = await _context.Users.FirstOrDefaultAsync(f => f.Id == id);
                if (user is null) return NotFound($"User with Id {id} not found.");

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("admin/{id:int}/floss")]
        public async Task<IActionResult> GetUserFlossAsync_Admin(int id)
        {
            try
            {
                User? user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
                if (user is null) return NotFound($"User with Id {id} not found.");

                List<FlossDTO> flosses = new();
                foreach (UserFloss uf in _context.UserFloss.Where(uf => uf.UserId == id))
                {
                    Floss f = _context.Floss.First(f => f.Id == uf.FlossId);
                    flosses.Add(new(
                        uf.FlossId, 
                        f.Name,
                        f.Number,
                        f.HexColor,
                        uf.Amount
                     ));
                }

                return Ok(flosses);
            }
            catch(Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPut("admin/{userId:int}/floss/{flossId:int}")]
        public async Task<IActionResult> UpdateUserFlossAsync_Admin(int userId, int flossId, int amount)
        {
            try
            {
                UserFloss? uf = await _context.UserFloss.FirstOrDefaultAsync(
                    uf => uf.UserId == userId && uf.FlossId == flossId);
                if (uf is null) return NotFound("UserFloss not found.");

                if (uf.Amount == amount) return Ok();
                if (uf.Amount < 0) return BadRequest("Amount cannot be less than 0.");

                uf.Amount = amount;
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch(Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("admin/{id:int}/projects")]
        public async Task<IActionResult> GetProjectsAdmin(int id)
        {
            if (UserExists(id, out User? user))
            {
                var projects = await _context.Projects.Where(p => p.UserId == id)
                    .Select(p => new ProjectAdminDTO(p.Id, p.UserId, p.Name, p.IsCompleted, p.CompletionDate, p.KeyPage, p.Aida, p.CreatedOn, p.LastModified))
                    .ToListAsync();
                return Ok(projects);
            }
            else
            {
                return NotFound("User not found");
            }
        }


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
            return BadRequest(ModelState);
        }

        // PUT: api/users/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Edit(int id, Dictionary<string, string?> user)
        {
            User? currentUser;
            if (!UserExists(id, out currentUser))
                return NotFound();

            if (ModelState.IsValid)
            {
                //Check if username needs to be changed
                if (user["Username"] != currentUser!.Username) currentUser.Username = user["Username"];

                //Check if password needs to be changed
                if (user["Password"] is not null)
                {
                    //Check if passwords match
                    var hasher = new PasswordHasher<User>();
                    currentUser.HashPassword = hasher.HashPassword(currentUser, user["Password"]!);
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
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("{id:int}/projects")]
        public async Task<IActionResult> GetProjects(int id)
        {
            if(UserExists(id, out User? user))
            {
                var projects = await _context.Projects.Where(p => p.UserId == id)
                    .Select(p => new ProjectDTO( p.Id, p.UserId, p.Name, p.IsCompleted, p.CompletionDate, p.KeyPage, p.Aida))
                    .ToListAsync();
                return Ok(projects);
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
                List<FlossDTO> flosses = new();
                foreach ((Floss floss, int amount) in user!.Floss)
                {
                    flosses.Add(new(floss.Id, floss.Name, floss.Number, floss.HexColor, amount));
                }

                return Ok(flosses);
            }
            else
            {
                return NotFound("User not found");
            }
        }

        [HttpPut("{userId:int}/floss/{flossId:int}")]
        public async Task<IActionResult> UpdateFloss(int userId, int flossId, int amount)
        {
            if (!_context.Users.Any(u => u.Id == userId)) return NotFound($"User with Id {userId} not found.");
            if (!_context.Floss.Any(f => f.Id == flossId)) return NotFound($"Floss with Id {flossId} not found.");

            var floss = await _context.UserFloss.FirstOrDefaultAsync(uf => uf.FlossId == flossId && uf.UserId == userId);
            if (floss is null) return NotFound($"User Floss not found.");

            floss.Amount = amount;

            await _context.SaveChangesAsync();
            return Ok();
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
        public record ProjectDTO(int Id, int UserId, string? Name, bool IsCompleted, DateTime? CompletionDate, int? KeyPage, int? Aida);
        public record ProjectAdminDTO(int Id, int UserId, string? Name, bool IsCompleted, DateTime? CompletionDate, int? KeyPage, int? Aida, DateTime? CreatedOn, DateTime? LastModified);
        public record FlossDTO(int Id, string? Name, string? Number, string? HexColor, int Amount);
    }
}
