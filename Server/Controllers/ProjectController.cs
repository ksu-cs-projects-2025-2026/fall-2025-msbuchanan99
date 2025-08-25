using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Server.Models;
using System.Text;

namespace Server.Controllers
{
    public class ProjectController : Controller
    {
        #region GET/POST
        // GET: ProjectController
        //Get all existing projects
        public ActionResult Index()
        {
            return View();
        }

        // GET: ProjectController/Details/5
        //Get details for project of given ID
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ProjectController/Create
        //Displays the form for creating a new project
        public ActionResult Create()
        {
            return View();
        }

        // POST: ProjectController/Create
        //Takes in information put into form by user to create a new 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ProjectController/Edit/5
        //Gets the form to edit project of given id
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ProjectController/Edit/5
        //Submits changes made to project
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // POST: ProjectController/Delete/5
        //delete the given project
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        [HttpPost]
        public ActionResult Upload(Byte[]? fileData)
        {
            Project p = new() { FileData = fileData };
            PdfDocument? doc = p.GetFileFromData();
            if (doc == null) return Content("");
            StringBuilder sb = ReadPDF(doc);
            return Content(sb.ToString(), "text/html");
        }

        #endregion

        #region helper

        private StringBuilder ReadPDF (PdfDocument doc)
        {
            StringBuilder sb = new();
            int numPages = doc.GetNumberOfPages();
            for(int i = 1; i < numPages; i++)
            {
                ITextExtractionStrategy strategy = new LocationTextExtractionStrategy();
                string text = PdfTextExtractor.GetTextFromPage(doc.GetPage(i), strategy);
                sb.Append(text);
                sb.Append("/n/n/n/n/n/n/n/n/n");
            }
            return sb;
        }

        #endregion
    }
}
