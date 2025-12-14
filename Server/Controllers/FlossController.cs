using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/floss")]
    public class FlossController : Controller
    {
        private readonly ThreadfolioContext _dbContext;

        public FlossController(ThreadfolioContext context)
        {
            _dbContext = context;
        }

        #region Admin

        /// <summary>
        /// Returns the list of floss
        /// </summary>
        /// <returns>result of the transaction</returns>
        [HttpGet("admin")]
        public async Task<IActionResult> GetAllFlossAsync_Admin()
        {
            try
            {
                List<Floss> floss = await _dbContext.Floss.ToListAsync();
                return Ok(floss);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Update the floss with the given id to match the properties of updateFloss
        /// </summary>
        /// <param name="id">Id of the floss to update</param>
        /// <param name="updateFloss">The changes to be made</param>
        /// <returns>The result of the transaction</returns>
        [HttpPut("admin/{id:int}")]
        public async Task<IActionResult> UpdateFlossDetailsAsync_Admin(int id, Floss updateFloss)
        {
            try
            {
                Floss? oldFloss = await _dbContext.Floss.FirstOrDefaultAsync(f => f.Id == id);
                if (oldFloss is null) return NotFound($"Floss with Id {id} not found.");

                updateFloss.Id = id;
                _dbContext.Entry(updateFloss).State = EntityState.Modified;

                await _dbContext.SaveChangesAsync();
                return Ok();
            }
            catch(Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Creates a new Floss with the given information
        /// </summary>
        /// <param name="newFloss">floss to create</param>
        /// <returns>The result of the transaction</returns>
        [HttpPost("admin")]
        public async Task<IActionResult> CreateFlossAsync_Admin(Floss newFloss)
        {
            try
            {
                await _dbContext.Floss.AddAsync(newFloss);
                await _dbContext.SaveChangesAsync();

                var userIds = await _dbContext.Users.Where(u => u.Role == "User").Select(u => u.Id).ToListAsync();

                var userFlosses = userIds.Select(userId => new UserFloss
                {
                    UserId = userId,
                    FlossId = newFloss.Id,
                    Amount = 0
                }).ToList();
                await _dbContext.UserFloss.AddRangeAsync(userFlosses);

                await _dbContext.SaveChangesAsync();
                return Ok();
            }
            catch(Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        [HttpDelete("admin/{id:int}")]
        public async Task<IActionResult> DeleteFlossAsync_Admin(int id)
        {
            try
            {
                Floss? floss = await _dbContext.Floss.FirstOrDefaultAsync(f => f.Id == id);
                if (floss is null) return NotFound($"Floss with Id {id} not found.");

                _dbContext.Floss.Remove(floss);
                await _dbContext.SaveChangesAsync();

                return Ok();
            }
            catch(Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        #endregion

        #region User

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var floss = await _dbContext.Floss.
                FirstOrDefaultAsync(f => f.Id == id);
            if (floss is not null) return NotFound($"Floss with id {id} not found");

            return Ok(floss);
        }

        [HttpGet("")]
        public async Task<IActionResult> GetAllFloss()
        {
            try
            {
                List<Floss> floss = await _dbContext.Floss.ToListAsync();
                return Ok(floss);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        #endregion

        
        private bool FlossExists(int id)
        {
            return _dbContext.Floss.Any(e => e.Id == id);
        }
    }
}
