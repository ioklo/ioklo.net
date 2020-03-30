using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Homepage.DbModels;
using Homepage.Models;
using Homepage.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Homepage.Controllers
{
    public class MainController : Controller
    {
        private readonly ILogger<MainController> logger;
        private MainDbContext dbContext;

        public MainController(ILogger<MainController> logger, MainDbContext dbContext)
        {
            this.logger = logger;
            this.dbContext = dbContext;

            dbContext.Database.EnsureCreated();
        }

        public IActionResult Index()
        {
            List<CommentViewModel> comments = new List<CommentViewModel>();

            foreach (var dbComment in dbContext.Comments.OrderByDescending(c => c.Id).Take(12))
            {
                comments.Add(new CommentViewModel(new CommentId(dbComment.Id), "IOKLO", dbComment.DateTime, dbComment.Text));
            }
            
            // comments.Add(new CommentViewModel(new CommentId(2), "IOKLO", new DateTime(2019, 10, 17, 20, 18, 0), "ㅋㅋㅋㅋ"));
            // comments.Add(new CommentViewModel(new CommentId(3), "IOKLO", new DateTime(2018, 10, 17, 20, 18, 0), "응답하라?"));

            return View(new IndexViewModel(comments));
        }

        [HttpPost]
        public async ValueTask<IActionResult> PostAsync(PostArgument arg)
        {
            if (ModelState.IsValid)
            {
                // TODO: <script>, 태그 조심
                dbContext.Comments.Add(new DbComment() { UserId = 0, DateTime = DateTime.Now, Text = arg.Text });
                await dbContext.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        public ValueTask<IActionResult> PrivacyAsync()
        {
            return new ValueTask<IActionResult>(View());
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel(Activity.Current?.Id ?? HttpContext.TraceIdentifier));
        }
    }
}