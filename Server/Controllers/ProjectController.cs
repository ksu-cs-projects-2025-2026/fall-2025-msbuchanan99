using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using System.Text;
using UglyToad.PdfPig;
using FileIO = System.IO.File;
using pdfPage = UglyToad.PdfPig.Content.Page;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectController : Controller
    {
        private readonly ThreadfolioContext _dbContext;
        private readonly string _pdfFolder = Path.Combine(Environment.CurrentDirectory, "Storage", "ProjectPDF");
        public ProjectController(ThreadfolioContext dbContext)
        {
            _dbContext = dbContext;
        }

        //Get the view of the list of projects
        [HttpGet]
        public async Task<IActionResult> Index(int? userId)
        {
            if(userId == null) //used for admin
            {
                var projects = await _dbContext.Projects.ToListAsync();
                if (projects == null || projects.Count == 0)
                {
                    return NotFound("No projects were found");
                }
                return Ok(projects);
            }
            else //used for user getting their project
            {
                var userprojects = await _dbContext.UserProjects.Where(up => up.UserId == userId).ToListAsync();
                if (userprojects == null || userprojects.Count == 0)
                {
                    return NotFound($"No projects for user {userId} found.");
                }

                List<Project> projects = userprojects.Select(up => up.Project).ToList();
                return Ok(projects);
            }
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

        // POST: Projects/5
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
        public async Task<IActionResult> UploadPattern(int? id, int? keyPage, IFormFile file)
        {
            if (id == null) return NotFound("Id cannot be null");
            else if (file == null) return NotFound("File cannot be null");

            var project = _dbContext.Projects.Find(id);
            if(project == null) return NotFound($"Project with Id {id} found");

            try
            {
                project.KeyPage = keyPage;
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
        public async Task<List<ProjectFloss>> ReadPattern(int? id)
        {
            if (id == null) return [];

            Project? project = await _dbContext.Projects.FirstAsync(p => p.Id == id);
            if (project == null || project.KeyPage == null) return [];

            string path = Path.Combine(_pdfFolder, project.FileName! + ".pdf");
            if (!FileIO.Exists(path)) return [];

            //Read the file
            using (PdfDocument pdf = PdfDocument.Open(path))
            {
                List<int> characters = new();
                List<Character> charactersDebug = new();
                int keyPage = (int)project.KeyPage;

                //Read each page to find the character symbols in the pattern
                for (int i = 1; i < keyPage; i++)
                {
                    pdfPage page = pdf.GetPage(i);
                    var pageChars = GetPageChars(page, false, ref charactersDebug);
                    characters.AddRange(pageChars);
                }

                //Read the key page to get key
                Dictionary<int, Floss> flossKey = new();
                List<int> ints = new();
                List<Character> intsDebug = new();
                for (int i = keyPage; i <= pdf.NumberOfPages; i++)
                {
                    pdfPage page = pdf.GetPage(i);
                    var pageChars = GetPageChars(page, true, ref intsDebug);
                    ints.AddRange(pageChars);
                }

                //split ints list into more lists by symbol numbers
                List<List<int>> lines = new List<List<int>>();
                List<int> currentLine = new List<int>();
                foreach (int i in ints)
                {
                    if (i > 255)
                    {
                        if (currentLine.Count > 0)
                        {
                            lines.Add(currentLine);
                            currentLine.Clear();
                        }
                        currentLine.Add(i);
                    }
                    else if (currentLine.Count > 0)
                    {
                        currentLine.Add(i);
                    }
                }

                //Go through each line in lines to create a dictionary of flosses and symbols
                Dictionary<Floss, int> FlossSymbol = new();
                Dictionary<int, int> SymbolAmount = new();
                foreach (List<int> line in lines)
                {
                    int symbol = line[0]; //The symbol is the first int in each row
                    SymbolAmount.Add(symbol, 0);

                    //Go through list of ints to find 'words' (groups of numbers and groups of letters)
                    List<string> words = new();
                    StringBuilder currentWord = new();
                    currentWord.Append(line[1]);
                    for (int i = 2; i < line.Count; i++)
                    {
                        char curLetter = (char)line[i];
                        char lastLetter = (char)line[i - 1];
                        bool curIsDigit = Char.IsDigit(curLetter);
                        bool lastIsDigit = Char.IsDigit(lastLetter);

                        if (lastIsDigit == curIsDigit) currentWord.Append(curLetter);
                        else
                        {
                            words.Add(currentWord.ToString());
                            currentWord.Clear();
                            currentWord.Append(curLetter);
                        }
                    }

                    //One of the words should match a Floss's number, one should match the Floss's name
                    //Or one of the words should be a concatenation of the number and name either way
                    var AllFloss = _dbContext.Floss.AsQueryable();
                    Floss? MatchingFloss = null;
                    foreach (Floss f in AllFloss)
                    {
                        string NameNumber = f.Name + f.Name;
                        string NumberName = f.Number + f.Name;
                        bool matchOne = false;
                        foreach(string w in words)
                        {
                            if(w == NameNumber || w == NumberName)
                            {
                                MatchingFloss = f;
                                break;
                            }
                            else if(w == f.Name || w == f.Number)
                            {
                                if(!matchOne) matchOne = true;
                                else
                                {
                                    MatchingFloss = f;
                                    break;
                                }
                            }
                        }

                        if (MatchingFloss != null)
                        {
                            break;
                        }
                    }

                    if (MatchingFloss == null) throw new Exception($"No match for Symbol {symbol} found.");
                    FlossSymbol.Add(MatchingFloss, symbol);
                }

                //Go through characters list to get number of times each symbol is used
                foreach(int i in characters)
                {
                    if (SymbolAmount.ContainsKey(i))
                    {
                        SymbolAmount[i] = SymbolAmount[i] + 1;
                    }
                }

                //Check Aida number and apply the proper equation
                List<ProjectFloss> projectFloss = new();
                foreach (KeyValuePair<int, int> pair in SymbolAmount)
                {
                    Floss floss = FlossSymbol.Where(fs => fs.Value == pair.Key).FirstOrDefault().Key;
                    ProjectFloss pf = new ProjectFloss(project, floss) { Strands = 2};
                    int skeinsNeeded = GetSkeinAmount(pair.Value, project.Aida, pf.Strands); //2 is hardcoded for default and can be changed
                    pf.Amount = skeinsNeeded;
                    projectFloss.Add(pf);
                }
                
                return projectFloss;
            }
        }

        [HttpGet("download/{filename}")]
        public IActionResult Download(string filename)
        {
            var path = Path.Combine(_pdfFolder, filename);
            if (!FileIO.Exists(path)) return NotFound($"File Not Found");

            var fileBytes = FileIO.ReadAllBytes(path);
            var contentType = "application/octet-stream";

            return File(fileBytes, contentType, filename);
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

        private List<int> GetPageChars(pdfPage page, bool getAll, ref List<Character> charDebug)
        {
            List<int> characters = new();
            string text = page.Text;

            //Read each character and add it to characters list
            foreach (char c in text)
            {
                int cAsInt = (int)c;
                if (getAll || (!getAll && cAsInt > 255))
                {
                    characters.Add(cAsInt);
                    charDebug.Add(new Character { CharVersion = c, IntVersion = cAsInt });
                }
            }
            return characters;
        }

        private int GetSkeinAmount(int numStitches, int Aida, int numStrands)
        {
            double oneSkein = 313.2; //Length of skein with all 6 strands lined up (not separated)
            double skeinLength = oneSkein * (6 / numStrands); //Skein length with num strands end to end (eg if 2 strands used in stitch it would be oneSkein * (6 / 2) bc there are three groups of two strands from one skein)
            double numerator = 2 * (1 + Math.Sqrt(2));
            double inchesPerStitch = numerator / Aida;
            double inchesOfStringNeeded = inchesPerStitch * numStitches;

            int skeinsNeeded = 1;
            while(skeinsNeeded * inchesOfStringNeeded < skeinLength)
            {
                skeinsNeeded++;
            }
            return skeinsNeeded;
        }

        #endregion
    }

    public class Character
    {
        public char CharVersion { get; set; }
        public int IntVersion { get; set; }
    }
}
