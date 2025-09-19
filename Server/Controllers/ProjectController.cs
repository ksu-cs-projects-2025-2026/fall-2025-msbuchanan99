using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using System.Text;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectController : ControllerBase
    {
        private readonly ThreadfolioContext _dbContext;
        public ProjectController(ThreadfolioContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet(Name = "GetProjects")]
        public IEnumerable<Project> Index()
        {
            return _dbContext.Projects.ToList();
        }

    }
}
