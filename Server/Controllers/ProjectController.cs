using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using System.Text;
using UglyToad.PdfPig;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using FileIO = System.IO.File;
using pdfPage = UglyToad.PdfPig.Content.Page;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/projects")]
    public class ProjectController : Controller
    {
        private readonly ThreadfolioContext _dbContext;
        private readonly string _pdfFolder = Path.Combine(Environment.CurrentDirectory, "Storage", "ProjectPDF");
        public ProjectController(ThreadfolioContext dbContext)
        {
            _dbContext = dbContext;
        }

        #region admin routes

        [HttpGet("admin")]
        public async Task<IActionResult> GetAllProjectsAsync_Admin()
        {
            try
            {
                List<Project> projects = await _dbContext.Projects.ToListAsync();
                return Ok(projects);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("admin/{id:int}")]
        public async Task<IActionResult> GetProjectsForUserAsync_Admin(int id)
        {
            try
            {
                List<Project> projects = await _dbContext.Projects.Where(p => p.UserId == id).ToListAsync();
                return Ok(projects);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("admin")]
        public async Task<IActionResult> CreateProjectAsync_Admin(Project newProject)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                await _dbContext.Projects.AddAsync(newProject);
                await _dbContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPut("admin/{id:int}")]
        public async Task<IActionResult> UpdateProjectAsync_Admin(int id, Project updateProject)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                Project? current = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id);
                if (current is null) return NotFound($"Project with Id {id} not found");

                current.Name = updateProject.Name;
                current.Aida = updateProject.Aida;
                current.KeyPage = updateProject.KeyPage;
                current.IsCompleted = updateProject.IsCompleted;
                current.CompletionDate = updateProject.CompletionDate;
                current.CreatedOn = updateProject.CreatedOn;
                current.LastModified = updateProject.LastModified;

                await _dbContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete("admin/{id:int}")]
        public async Task<IActionResult> DeleteProjectAsync_Admin(int id)
        {
            try
            {
                Project? project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id);
                if (project is null) return NotFound($"Project with Id {id} not found");

                _dbContext.Projects.Remove(project);
                await _dbContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        //Project Floss Routes
        [HttpGet("admin/{id:int}/floss")]
        public async Task<IActionResult> GetProjectFlossAsync_Admin(int id)
        {
            try
            {
                Project? project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id);
                if (project is null) return NotFound($"Project with Id {id} not found.");

                List<ProjectFlossDTO> flosses = new();
                foreach (ProjectFloss pf in _dbContext.ProjectFloss.Where(pf => pf.ProjectId == id))
                {
                    Floss f = _dbContext.Floss.First(f => f.Id == pf.FlossId);
                    flosses.Add(new
                        (pf.FlossId, f.Name, f.Number, f.HexColor, pf.Amount, pf.Strands)
                    );
                }

                return Ok(flosses);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("admin/{projectId:int}/floss")]
        public async Task<IActionResult> CreateProjectFlossAsync_Admin(int projectId, ProjectFloss newPF)
        {
            try
            {
                bool projectExists = await _dbContext.Projects.AnyAsync(p => p.Id == projectId);
                if (projectExists) return BadRequest($"Project with Id {projectId} does not exist.");

                bool PFexists = await _dbContext.ProjectFloss.AnyAsync(
                    pf => pf.ProjectId == projectId && pf.FlossId == newPF.FlossId);
                if (PFexists) return BadRequest("This floss already exists in the project");

                await _dbContext.ProjectFloss.AddAsync(newPF);
                await _dbContext.SaveChangesAsync();
                return Ok();
            }
            catch(Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPut("admin/{projectId:int}/floss/{flossId:int}")]
        public async Task<IActionResult> UpdateProjectFlossAsync_Admin(int projectId, int flossId, ProjectFloss update)
        {
            try
            {
                bool projectExists = await _dbContext.Projects.AnyAsync(p => p.Id == projectId);
                if (projectExists) return BadRequest($"Project with Id {projectId} does not exist.");

                ProjectFloss? projectFloss = await _dbContext.ProjectFloss.FirstOrDefaultAsync(
                    pf => pf.ProjectId == projectId && pf.FlossId == flossId);
                if (projectFloss is null) return NotFound("UserFloss not found.");

                if (projectFloss.Amount == update.Amount && projectFloss.Strands == update.Strands) return Ok();
                if (update.Amount <= 0) return BadRequest("Amount cannot be less than or equal to 0.");
                if (update.Strands <= 0) return BadRequest("Strands cannot be less than or equal to 0.");

                projectFloss.Amount = update.Amount;
                projectFloss.Strands = update.Strands;
                await _dbContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete("admin/{projectId:int}/floss/{flossId:int}")]
        public async Task<IActionResult> DeleteProjectFlossAsync_Admin(int projectId, int flossId)
        {
            try
            {
                bool projectExists = await _dbContext.Projects.AnyAsync(p => p.Id == projectId);
                if (projectExists) return BadRequest($"Project with Id {projectId} does not exist.");

                ProjectFloss? projectFloss = await _dbContext.ProjectFloss.FirstOrDefaultAsync(
                    pf => pf.ProjectId == projectId && pf.FlossId == flossId);
                if (projectFloss is null) return NotFound("UserFloss not found.");

                _dbContext.Remove(projectFloss);
                await _dbContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        #endregion

        #region user routes

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

            return Ok(new ProjectDTO(project.Id, project.UserId, project.Name, project.IsCompleted,
                project.CompletionDate, project.KeyPage, project.Aida));
        }

        [HttpPost]
        public async Task<IActionResult> CreateProjectAsync(Project newProject)
        {
            if(!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                await _dbContext.Projects.AddAsync(newProject);
                await _dbContext.SaveChangesAsync();
                return Ok();
            }
            catch(Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPut("{projectId:int}")]
        public async Task<IActionResult> UpdateProjectAsync(int projectId, Project updateProject)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                Project? project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
                if (project is null) return NotFound($"Project with id {projectId} not found");

                project.Name = updateProject.Name;
                project.Aida = updateProject.Aida;
                project.KeyPage = updateProject.KeyPage;
                project.IsCompleted = updateProject.IsCompleted;
                if (project.IsCompleted) project.CompletionDate = DateTime.Now;
                project.LastModified = DateTime.Now;

                await _dbContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete("{projectId:int}")]
        public async Task<IActionResult> DeleteProject(int projectId)
        {
            try
            {
                Project? project = _dbContext.Projects.FirstOrDefault(p => p.Id == projectId);
                if (project is null) return NotFound($"Project with id {projectId} not found");

                _dbContext.Projects.Remove(project);
                await _dbContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("{projectId:int}/floss")]
        public async Task<IActionResult> GetFloss(int? projectId)
        {
            if (projectId == null) return NotFound("ProjectId cannot be null");

            var projectExists = _dbContext.Projects.Any(p => p.Id == projectId);
            if (!projectExists) return NotFound($"Project with Id {projectId} not found");

            IEnumerable<ProjectFloss> projectFloss = _dbContext.ProjectFloss.Where(pf => pf.ProjectId == projectId);
            List<FlossDTO> Flosses = new();
            foreach (var pf in projectFloss)
            {
                var floss = await _dbContext.Floss.FirstOrDefaultAsync(f => f.Id == pf.FlossId);
                var flossDTO = new FlossDTO(floss.Id, floss.Name, floss.Number, floss.HexColor, pf.Amount, pf.Strands, (int)projectId);
                Flosses.Add(flossDTO);
            }

            return Ok(Flosses);
        }

        [HttpPost("{projectId:int}/floss")]
        public async Task<IActionResult> AddFloss(int projectId, int flossId, int amount, int strands)
        {
            try
            {
                Project? project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
                if(project is null) return NotFound($"Project with Id {projectId} not found");

                ProjectFloss? projectFloss = await _dbContext.ProjectFloss.
                    FirstOrDefaultAsync(pf => pf.ProjectId == projectId && pf.FlossId == flossId);
                if (projectFloss is not null) return BadRequest($"This project already has the floss added to it already.");

                projectFloss = new()
                {
                    ProjectId = projectId,
                    FlossId = flossId,
                    Amount = amount,
                    Strands = strands
                };

                await _dbContext.ProjectFloss.AddAsync(projectFloss);
                await _dbContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPut("{projectId:int}/floss/{flossId:int}")]
        public async Task<IActionResult> UpdateFloss(int projectId, int flossId, int amount, int strands)
        {
            try
            {
                Project? project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
                if (project is null) return NotFound($"Project with Id {projectId} not found");

                ProjectFloss? projectFloss = await _dbContext.ProjectFloss.
                    FirstOrDefaultAsync(pf => pf.ProjectId == projectId && pf.FlossId == flossId);
                if (projectFloss is null) return NotFound($"This project doesn't have this floss in it.");

                projectFloss.Amount = amount;
                projectFloss.Strands = strands;

                _dbContext.Update(projectFloss);
                await _dbContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete("{projectId}/floss/{flossId:int}")]
        public async Task<IActionResult> DeleteFloss(int projectId, int flossId)
        {
            try
            {
                Project? project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
                if (project is null) return NotFound($"Project with Id {projectId} not found");

                ProjectFloss? projectFloss = await _dbContext.ProjectFloss.
                    FirstOrDefaultAsync(pf => pf.ProjectId == projectId && pf.FlossId == flossId);
                if (projectFloss is null) return NotFound($"This project doesn't have this floss in it.");

                _dbContext.Remove(projectFloss);
                await _dbContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("{id}/pattern")]
        public async Task<IActionResult> ViewPattern(int? id)
        {
            if (id == null) return NotFound("Id is null");

            var project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (project == null) return NotFound($"Project with id {id} not found.");

            if (project.FileName is null) return NotFound("Project does not have a file.");

            var path = Path.Combine(_pdfFolder, project.FileName + ".pdf");
            if (!FileIO.Exists(path)) return NotFound("File not found in server storage.");

            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var result = File(stream, "application/pdf");

            return result;
        }

        [HttpPost("{id}/pattern/upload")]
        public async Task<IActionResult> UploadPattern(int? id, int? keyPage, IFormFile file)
        {
            if (id == null) return NotFound("Id cannot be null");
            else if (file == null) return NotFound("File cannot be null");

            var project = _dbContext.Projects.Find(id);
            if (project == null) return NotFound($"Project with Id {id} found");

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

        [HttpGet("{id:int}/download")]
        public IActionResult DownloadPattern (int id)
        {
            var project = _dbContext.Projects.FirstOrDefault(p => p.Id == id);
            if (project is null) return NotFound($"Project with id {id} not found.");

            var filename = project.FileName;
            var path = Path.Combine(_pdfFolder, filename);
            if (!FileIO.Exists(path)) return NotFound($"File Not Found");

            var fileBytes = FileIO.ReadAllBytes(path);
            var contentType = "application/pdf";

            return File(fileBytes, contentType, project.Name.Replace(" ", string.Empty));
        }

        [HttpGet("{projectId:int}/pattern/read-key/full-auto")]
        public async Task<IActionResult> ReadPatternKey(int? projectId)
        {
            if (projectId == null) return BadRequest("Id cannot be null");

            Project? project = await _dbContext.Projects.FirstAsync(p => p.Id == projectId);
            if (project == null || project.KeyPage == null) return NotFound($"Project with id {projectId} not found");

            string path = Path.Combine(_pdfFolder, project.FileName! + ".pdf");
            if (!FileIO.Exists(path)) return NotFound("File not found");

            List<List<int>> KeyPageLines = ReadKeyPage(path, (int)project.KeyPage);

            Dictionary<int, SymbolData> SymbolDictionary = new();
            List<Floss> AllFloss = _dbContext.Floss.ToList();
            foreach (List<int> Line in KeyPageLines)
            {
                int symbol = Line[0];
                SymbolData data = new();

                List<string> WordsInLine = GetLineWords(Line);

                Floss? MatchingFloss = MatchWordsToFloss(WordsInLine, AllFloss);
                data.Floss = MatchingFloss;

                SymbolDictionary.Add(symbol, data);
            }

            return Ok(SymbolDictionary);
        }

        [HttpPost("{projectId:int}/pattern/read-key/manual")]
        public async Task<IActionResult> ReadPatternKey_GivenListOfFloss(int projectId, List<int> FlossIds)
        {
            if (FlossIds.Count < 1) return BadRequest("List of flosses must have at least one entry");

            Project? project = await _dbContext.Projects.FirstAsync(p => p.Id == projectId);
            if (project == null || project.KeyPage == null) return NotFound($"Project with id {projectId} not found");

            string path = Path.Combine(_pdfFolder, project.FileName! + ".pdf");
            if (!FileIO.Exists(path)) return NotFound("File not found");

            List<List<int>> KeyPagesLines = ReadKeyPage(path, (int)project.KeyPage);

            Dictionary<int, SymbolData> SymbolDictionary = new();
            for(int i = 0; i < KeyPagesLines.Count; i++)
            {
                int symbol = KeyPagesLines[i][0];
                Floss floss = _dbContext.Floss.First(f => f.Id == FlossIds[i]);

                SymbolDictionary.Add(symbol, new(floss));
            }

            return Ok(SymbolDictionary);
        }

        [HttpPost("{projectId:int}/pattern/read-pattern")]
        public async Task<IActionResult> ReadPattern(int projectId, Dictionary<int, SymbolData> symbolDictionary)
        {
            if (symbolDictionary.Count < 1) return BadRequest("Invalid symbol entries");

            Project? project = await _dbContext.Projects.FirstAsync(p => p.Id == projectId);
            if (project == null || project.KeyPage == null) return NotFound($"Project with id {projectId} not found");

            string path = Path.Combine(_pdfFolder, project.FileName! + ".pdf");
            if (!FileIO.Exists(path)) return NotFound("File not found");

            List<int> characters = new();
            using(PdfDocument pdf = PdfDocument.Open(path))
            {
                int keyPage = (int)project.KeyPage;
                //Read each page to find the character symbols in the pattern
                for (int i = 1; i < keyPage; i++)
                {
                    pdfPage page = pdf.GetPage(i);
                    foreach(char c in page.Text)
                    {
                        int cAsInt = c;
                        if(cAsInt > 255)
                        {
                            if(symbolDictionary.TryGetValue(cAsInt, out var symbolData))
                            {
                                symbolData.Count++;
                            }
                        }
                    }
                }
            }

            List<ProjectFlossDTO> returnable = new();
            foreach (var kvp in symbolDictionary)
            {
                SymbolData data = kvp.Value;
                int symbol = kvp.Key;

                if (data.Count <= 0) continue;
                if (data.Floss is null) continue;

                Floss floss = data.Floss;

                ProjectFlossDTO dto = new(floss.Id, floss.Name, floss.Number, floss.HexColor, data.Count, 2);
                returnable.Add(dto);
            }

            return Ok(returnable);
        }

        #endregion

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

        private List<List<int>> ReadKeyPage(string path, int keyPage)
        {
            Dictionary<int, Floss> flossKey = new();
            List<int> ints = new();
            List<Character> intsDebug = new();

            using (PdfDocument pdf = PdfDocument.Open(path))
            {
                //Read each key page to get key
                for (int i = keyPage; i <= pdf.NumberOfPages; i++)
                {
                    pdfPage page = pdf.GetPage(i);
                    var pageChars = GetPageChars(page, true, ref intsDebug);
                    ints.AddRange(pageChars);
                }
            }

            List<List<int>> lines = new List<List<int>>();
            List<int> currentLine = new List<int>();
            foreach (int i in ints)
            {
                char c = (char)i;
                if (i > 255)
                {
                    if (currentLine.Count > 0)
                    {
                        lines.Add(currentLine);
                        currentLine = new();

                    }
                    currentLine.Add(i);
                }
                else if (currentLine.Count > 0)
                {
                    currentLine.Add(i);
                }
            }
            lines.Add(currentLine);

            return lines;
        }

        private List<string> GetLineWords(List<int> line)
        {
            //Go through list of ints to find 'words' (groups of numbers and groups of letters)
            List<string> words = new();
            StringBuilder currentWord = new();
            currentWord.Append((char)line[1]);
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
                    currentWord = new();
                    currentWord.Append(curLetter);
                }
            }
            words.Add(currentWord.ToString());

            return words;
        }

        private Floss? MatchWordsToFloss(List<string> Words, List<Floss> AllFloss)
        {
            Floss? MatchingFloss = null;
            Floss? MatchOneFloss = null;
            foreach (Floss f in AllFloss)
            {
                string NameNumber = f.Name + f.Number;
                string NumberName = f.Number + f.Name;
                bool matchOne = false;
                foreach (string w in Words)
                {
                    if (w == NameNumber || w == NumberName || w.Contains(NumberName) || w.Contains(NameNumber))
                    {
                        MatchingFloss = f;
                        break;
                    }
                    else if (w == f.Name || w == f.Number)
                    {
                        if (!matchOne)
                        {
                            matchOne = true;
                            MatchOneFloss = f;
                        }
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

            return MatchingFloss ?? MatchOneFloss;
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
    public class SymbolData
    {
        public Floss? Floss { get; set; }
        public int Count { get; set; }
        public SymbolData()
        {
            Count = 0;
            Floss = null;
        }
        public SymbolData(Floss floss)
        {
            Count = 0;
            Floss = floss;
        }
    }
    public record FlossDTO(int Id, string? Name, string? Number, string? HexColor, 
        /*char Symbol,*/ int? Amount, int? Strands, int ProjectId);
    public record ProjectWithFlossDTO(int Id, int UserId, string? Name, bool IsCompleted, 
        DateTime? CompletionDate, int? KeyPage, int? Aida, IEnumerable<FlossDTO>? Floss);
    public record ProjectDTO(int Id, int UserId, string? Name, bool IsCompleted, DateTime? CompletionDate, int? KeyPage, int? Aida);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="Name"></param>
    /// <param name="Number"></param>
    /// <param name="HexColor"></param>
    /// <param name="Amount">Can refer to number of skeins needed or number of stitches</param>
    /// <param name="Strands"></param>
    public record ProjectFlossDTO(int Id, string? Name, string? Number, string? HexColor, int Amount, int Strands);
}
