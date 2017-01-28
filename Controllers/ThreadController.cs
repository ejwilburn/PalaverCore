using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Palaver.Data;
using Palaver.Models;

namespace Palaver.Controllers
{
    [Authorize]
    [RequireHttps]
//    [Route("[controller]")]
    public class ThreadController : Controller
    {
        private readonly PalaverDbContext _context;
        private readonly UserManager<User> _userManager;

        public ThreadController(PalaverDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("show")]
        [Route("/thread")]
        public async Task<IActionResult> Show()
        {
            List<Thread> threads = await _context.GetThreadsListAsync(GetUserId());
            return View("~/Views/Thread/Thread.cshtml", threads);
        }

        [HttpGet]
        [Route("/thread/{threadId}")]
        public async Task<IActionResult> Show(int threadId)
        {
            List<Thread> threads = await _context.GetThreadsListAsync(GetUserId());
            ViewData["SelectedThread"] = await _context.GetThreadAsync(threadId, GetUserId());
            return View("~/Views/Thread/Thread.cshtml", threads);
        }

        [HttpGet]
        [Route("/thread/{threadId}/{commentId}")]
        public async Task<IActionResult> Show(int threadId, int commentId)
        {
            List<Thread> threads = await _context.GetThreadsListAsync(GetUserId());
            ViewData["SelectedThread"] = await _context.GetThreadAsync(threadId, GetUserId());
            ViewData["SelectedCommentId"] = commentId;
            return View("~/Views/Thread/Thread.cshtml", threads);
        }

        [HttpGet]
        [Route("/api/thread")]
        public async Task<IEnumerable<Thread>> Get()
        {
            List<Thread> threads = await _context.GetThreadsListAsync(GetUserId());
            return threads;
        }

        [HttpGet]
        [Route("/api/thread/{threadId}")]
        public async Task<IActionResult> Get(int threadId)
        {
            Thread thread = await _context.GetThreadAsync(threadId, GetUserId());
            if (thread == null)
            {
                return NotFound();
            }
            return new ObjectResult(thread);
        }

        [HttpPost]
        [Route("/api/thread")]
        public async Task<IActionResult> Create([FromBody] Thread thread)
        {
            if (thread == null)
            {
                return BadRequest();
            }
            thread.User = await _userManager.GetUserAsync(HttpContext.User);
            await _context.Threads.AddAsync(thread);
            await _context.SaveChangesAsync();
            return CreatedAtRoute(new { id = thread.Id }, thread);
        }

        private int GetUserId()
        {
            return int.Parse(_userManager.GetUserId(HttpContext.User));
        }
    }
}
