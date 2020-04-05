using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Homepage.DbModels;
using Homepage.Models;
using Homepage.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        [Authorize]
        public async ValueTask<IActionResult> SignInAsync()
        {
            var nameId = User.FindFirst(ClaimTypes.NameIdentifier);
            if (nameId == null) return Problem();

            var dbUser = await dbContext.Users.SingleOrDefaultAsync(u => nameId.Value == u.FacebookNameId);
            if (dbUser == null)
            {
                dbContext.Users.Add(new DbUser() { FacebookNameId = nameId.Value, Name = User.Identity.Name });
                await dbContext.SaveChangesAsync();
            }

            Response.Cookies.Append("loggedIn", "true");
            await Response.WriteAsync("<script>window.opener.updateStatus(); self.opener = self; window.close();</script>");
            return Ok();
        }

        public IActionResult Index()
        {
            List<CommentViewModel> comments = new List<CommentViewModel>();

            var dbComments = dbContext.Comments
                .Join(dbContext.Users, c => c.UserId, u => u.Id, (c, u) => new { c.Id, u.Name, c.DateTime, c.Text })
                .OrderByDescending(c => c.Id)
                .Take(12);

            foreach (var dbComment in dbComments)
            {
                comments.Add(new CommentViewModel(new CommentId(dbComment.Id), dbComment.Name, dbComment.DateTime, dbComment.Text));
            }            
            
            return View(new IndexViewModel(comments));
        }
        
        [HttpPost]
        public async ValueTask<IActionResult> ChangeNameAsync(string name)
        {
            if (!ModelState.IsValid) return Problem();
            if (!User.Identity.IsAuthenticated) return Problem();

            var nameId = User.FindFirst(ClaimTypes.NameIdentifier);
            if (nameId == null) return Problem();

            var dbUser = await dbContext.Users.SingleOrDefaultAsync(u => nameId.Value == u.FacebookNameId);
            if (dbUser == null) return Problem();

            dbUser.Name = name;
            await dbContext.SaveChangesAsync();
            return Ok();
        }
        
        [HttpPost]
        public async ValueTask<IActionResult> PostAsync(string text)
        {
            if (User.Identity.IsAuthenticated && ModelState.IsValid)
            {
                var nameId = User.FindFirst(ClaimTypes.NameIdentifier);
                if (nameId == null) return Problem();

                var dbUser = await dbContext.Users.SingleOrDefaultAsync(u => nameId.Value == u.FacebookNameId);
                if (dbUser == null) return Problem();

                dbContext.Comments.Add(new DbComment() { UserId = dbUser.Id, DateTime = DateTime.Now, Text = text });
                await dbContext.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        [Authorize]
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