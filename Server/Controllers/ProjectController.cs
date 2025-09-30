using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Spire.Pdf;
using Spire.Pdf.Texts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FileIO = System.IO.File;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectController : Controller
    {
        private readonly ThreadfolioContext _dbContext;
        private readonly string _pdfFolder = Path.Combine(AppContext.BaseDirectory, "Storage", "ProjectPDF");
        public ProjectController(ThreadfolioContext dbContext)
        {
            _dbContext = dbContext;
        }

        //Get the view of the list of projects
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var projects = await _dbContext.Projects.ToListAsync();
            if (projects == null || projects.Count == 0)
            {
                return NotFound("No projects were found");
            }

            return View(projects);
        }

        //Get the view of an individual project
        [HttpGet("{id}/details")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound("Project Id cannot be null");
            }

            var project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (project == null)
            {
                return NotFound($"Project with Id {id} not found");
            }

            return View(project);
        }

        //Get the create view
        [HttpGet("create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,FileName,IsCompleted,CompletionDate,CreatedOn,LastModified")] Project project)
        {
            if (ModelState.IsValid)
            {
                _dbContext.Add(project);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(project);
        }

        //Get the view to edit the given project
        [HttpGet("{id}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id)
        {
            if(id == null)
            {
                return NotFound("Project Id cannot be null");
            }

            var project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (project == null)
            {
                return NotFound($"Project with Id {id} not found");
            }

            return View(project);
        }

        //Post the edit of the project
        [HttpPost("{id}/edit")]
        public async Task<IActionResult> Edit(int id, [Bind("Id, Name, FileName, IsCompleted, CompletionDate")] Project project)
        {
            if (id != project.Id) return NotFound("Id cannot be null");

            if (ModelState.IsValid)
            {
                try
                {
                    _dbContext.Update(project);
                    await _dbContext.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.Id))
                    {
                        return NotFound($"Project with id {id} not found");
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(Index)); //on success redirect to Index
            }
            return View(project); //If model's properties dont follow [] guidelines go back to project view
        }


        // GET: Project/Delete/5
        [HttpGet("{id}/delete")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _dbContext.Projects
                .FirstOrDefaultAsync(p => p.Id == id);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // POST: Floss/Delete/5
        [HttpDelete("{id}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project = await _dbContext.Projects.FindAsync(id);
            if (project != null)
            {
                //Delete connected ProjectFloss
                foreach (var pf in project.ProjectFloss)
                {
                    _dbContext.ProjectFloss.Remove(pf);
                }

                //delete pdf file
                if(!string.IsNullOrWhiteSpace(project.FileName)) DeleteDocument(project.FileName);

                _dbContext.Projects.Remove(project);
            }

            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{id}/Pattern")]
        public async Task<IActionResult> ViewPattern(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _dbContext.Projects
                .FirstOrDefaultAsync(p => p.Id == id);
            if (project == null)
            {
                return NotFound();
            }

            return View("PatternView", project);
        }

        [HttpPost("{id}/Pattern/Upload")]
        public async Task<IActionResult> UploadPattern(int? id, IFormFile file)
        {
            if (id == null) return NotFound("Id cannot be null");
            else if (file == null) return NotFound("File cannot be null");

            var project = _dbContext.Projects.Find(id);
            if(project == null) return NotFound($"Project with Id {id} found");

            try
            {
                project.FileName = await UploadDocument(file);
                _dbContext.Projects.Update(project);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            return RedirectToAction(nameof(ViewPattern));
        }

        [HttpPost("{id}/Pattern/ReadPattern")]
        public async Task<char[]> ReadPattern(int? id, int listPage)
        {
            if (id == null) return [];

            Project? project = await _dbContext.Projects.FirstAsync(p => p.Id == id);
            if (project == null) return [];

            string path = Path.Combine(_pdfFolder, project.FileName!);
            using FileStream fs = new(path, FileMode.Open);
            PdfDocument pdf = new();
            pdf.LoadFromStream(fs);
            char[] chars = new char[2000000];
            int i = 0;
            foreach(PdfPageBase page in pdf.Pages)
            {
                PdfTextExtractor ext = new PdfTextExtractor(page);
                PdfTextExtractOptions extractOptions = new PdfTextExtractOptions() { IsExtractAllText = true };
                string text = ext.ExtractText(extractOptions);
                foreach(char c in text)
                {
                    if(i < 2000000)
                    {
                        chars[i] = c;
                        i++;
                    }
                }
            }
            return chars;
        }

        #region Helper Methods

        private async Task<string> UploadDocument(IFormFile file)
        {
            if(file == null)
            {
                throw new ArgumentNullException("File is null");
            }
            if(file.ContentType != "application/pdf")
            {
                throw new ArgumentException("File is not a pdf");
            }
            
            //Upload new file
            string uniqueFileName = Guid.NewGuid().ToString() + ".pdf";
            string path = Path.Combine(_pdfFolder, uniqueFileName);
            while (FileIO.Exists(path))
            {
                uniqueFileName = Guid.NewGuid().ToString() + ".pdf";
                path = Path.Combine(_pdfFolder, uniqueFileName);
            }
            using (var filestream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(filestream);
            }
            return uniqueFileName;
        }

        private void DeleteDocument(string oldFileName)
        {
            if (string.IsNullOrEmpty(oldFileName)) throw new ArgumentNullException("Old File name is not valid");

            string path = Path.Combine(_pdfFolder, oldFileName);
            if (FileIO.Exists(path)) 
            {
                FileIO.Delete(path);
            }
            else
            {
                throw new ArgumentException("File with that name was not found");
            }
        }

        private bool ProjectExists (int id)
        {
            return _dbContext.Projects.Any(p => p.Id == id);
        }

        #endregion
    }
}
