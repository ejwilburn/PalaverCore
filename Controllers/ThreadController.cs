using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Palaver.Data;
using Palaver.Models;

namespace Palaver.Controllers
{
    [RequireHttps]
    public class ThreadController : Controller
    {
        private readonly PalaverDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly int _userId;

        public ThreadController(PalaverDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
            _userId = int.Parse(_userManager.GetUserId(HttpContext.User));
        }

        public async Task<IActionResult> Thread()
        {
            return View(await _context.GetThreadsListAsync(_userId));
        }

        public async Task<IActionResult> Thread(int threadId)
        {
            List<Thread> threads = await _context.GetThreadsListAsync(_userId);
            ViewData["SelectedThread"] = await _context.GetThreadAsync(threadId, _userId);
            return View("~/Views/Thread/Thread.cshtml", threads);
        }

        public async Task<IActionResult> Thread(int threadId, int commentId)
        {
            List<Thread> threads = await _context.GetThreadsListAsync(_userId);
            ViewData["SelectedThread"] = await _context.GetThreadAsync(threadId, _userId);
            ViewData["SelectedCommentId"] = commentId;
            return View("~/Views/Thread/Thread.cshtml", threads);
        }
    }
}
