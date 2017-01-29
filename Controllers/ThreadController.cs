using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using Palaver.Data;
using Palaver.Models;
using Palaver.Models.ThreadViewModels;

namespace Palaver.Controllers
{
    [Authorize]
    [RequireHttps]
//    [Route("[controller]")]
    public class ThreadController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly PalaverDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private readonly int _userId;

        public ThreadController(PalaverDbContext context, UserManager<User> userManager, IMapper mapper, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _userId = int.Parse(_userManager.GetUserId(_httpContextAccessor.HttpContext.User));
            ViewData["userId"] = _userId;
        }

        [HttpGet("show")]
        [Route("/thread")]
        public async Task<IActionResult> Show()
        {
            List<Thread> threads = await _context.GetThreadsListAsync(_userId);
            return View("~/Views/Thread/Thread.cshtml", threads);
        }

        [HttpGet]
        [Route("/thread/{threadId}")]
        public async Task<IActionResult> Show(int threadId)
        {
            List<Thread> threads = await _context.GetThreadsListAsync(_userId);
            Thread selectedThread = await _context.GetThreadAsync(threadId, _userId);
            ViewData["SelectedThread"] = selectedThread;
            ViewData["threadId"] = selectedThread.Id;
            return View("~/Views/Thread/Thread.cshtml", threads);
        }

        [HttpGet]
        [Route("/thread/{threadId}/{commentId}")]
        public async Task<IActionResult> Show(int threadId, int commentId)
        {
            List<Thread> threads = await _context.GetThreadsListAsync(_userId);
            Thread selectedThread = await _context.GetThreadAsync(threadId, _userId);
            ViewData["SelectedThread"] = selectedThread;
            ViewData["threadId"] = selectedThread.Id;
            ViewData["commentId"] = commentId;
            return View("~/Views/Thread/Thread.cshtml", threads);
        }

        [HttpGet]
        [Route("/api/thread")]
        public async Task<IEnumerable<ListViewModel>> Get()
        {
            List<Thread> threads = await _context.GetThreadsListAsync(_userId);
            return _mapper.Map<List<Thread>, List<ListViewModel>>(threads);
        }

        [HttpGet]
        [Route("/api/thread/{threadId}")]
        public async Task<IActionResult> Get(int threadId)
        {
            Thread thread = await _context.GetThreadAsync(threadId, _userId);
            if (thread == null)
            {
                return NotFound();
            }
            return new ObjectResult(_mapper.Map<Thread, SelectedViewModel>(thread));
        }

        [HttpPost]
        [Route("/api/thread")]
        public async Task<IActionResult> Create([FromBody] string title)
        {
            if (string.IsNullOrEmpty(title))
            {
                return BadRequest();
            }
            Thread newThread = await _context.CreateThreadAsync(title, _userId);
            return CreatedAtRoute(new { id = newThread.Id }, _mapper.Map<Thread, CreateResultViewModel>(newThread));
        }

        private int GetUserId()
        {
            return int.Parse(_userManager.GetUserId(HttpContext.User));
        }
    }
}
