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
    public class FlossController : Controller
    {
        private readonly ThreadfolioContext _context;

        public FlossController(ThreadfolioContext context)
        {
            _context = context;
        }

        // GET: Floss
        public async Task<IActionResult> Index()
        {
            return View(await _context.Floss.ToListAsync());
        }

        // GET: Floss/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var floss = await _context.Floss
                .FirstOrDefaultAsync(m => m.Id == id);
            if (floss == null)
            {
                return NotFound();
            }

            return View(floss);
        }

        // GET: Floss/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Floss/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Number,HexColor,CreatedOn,LastModified")] Floss floss)
        {
            if (ModelState.IsValid)
            {
                _context.Add(floss);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(floss);
        }

        // GET: Floss/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var floss = await _context.Floss.FindAsync(id);
            if (floss == null)
            {
                return NotFound();
            }
            return View(floss);
        }

        // POST: Floss/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Number,HexColor,CreatedOn,LastModified")] Floss floss)
        {
            if (id != floss.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(floss);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FlossExists(floss.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(floss);
        }

        // GET: Floss/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var floss = await _context.Floss
                .FirstOrDefaultAsync(m => m.Id == id);
            if (floss == null)
            {
                return NotFound();
            }

            return View(floss);
        }

        // POST: Floss/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var floss = await _context.Floss.FindAsync(id);
            if (floss != null)
            {
                _context.Floss.Remove(floss);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FlossExists(int id)
        {
            return _context.Floss.Any(e => e.Id == id);
        }
    }
}
