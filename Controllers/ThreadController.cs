/*
Copyright 2017, E.J. Wilburn, Marcus McKinnon, Kevin Williams
This program is distributed under the terms of the GNU General Public License.

This file is part of Palaver.

Palaver is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 2 of the License, or
(at your option) any later version.

Palaver is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Palaver.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using PalaverCore.Data;
using PalaverCore.Services;
using PalaverCore.Models;
using PalaverCore.Models.ThreadViewModels;
using Microsoft.Extensions.Logging;

namespace PalaverCore.Controllers
{
    [Authorize]
    public class ThreadController : Controller
    {
        private readonly HttpContext _httpContext;
        private readonly PalaverDbContext _dbContext;
        private readonly StubbleRendererService _stubble;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly int _userId;

        public ThreadController(PalaverDbContext dbContext, UserManager<User> userManager, IMapper mapper,
            IHttpContextAccessor httpContextAccessor, StubbleRendererService stubble, ILoggerFactory loggerFactory)
        {
            this._dbContext = dbContext;
            this._stubble = stubble;
            this._userManager = userManager;
            this._mapper = mapper;
            this._httpContext = httpContextAccessor.HttpContext;
            this._logger = loggerFactory.CreateLogger<ThreadController>();
            this._userId = int.Parse(_userManager.GetUserId(_httpContext.User));
        }

        // Adding this just for the default route until I figure out how to do it right.
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return await Show(null, null);
        }

        [HttpGet("show")]
        [Route("Thread/{threadId?}/{commentId?}")]
        public async Task<IActionResult> Show(int? threadId, int? commentId)
        {
            ViewData["userId"] = _userId;
            List<ListViewModel> threads = _mapper.Map<List<Thread>, List<ListViewModel>>(await _dbContext.GetPagedThreadsListAsync(_userId));
            ViewData["ThreadListViewHtml"] = _stubble.RenderThreadListFromTemplate(threads);

            if (commentId != null )
                ViewData["commentId"] = commentId;

            if (threadId != null)
            {
                SelectedViewModel selectedThread = _mapper.Map<Thread, SelectedViewModel>(await _dbContext.GetThreadAsync((int)threadId, _userId));
                ViewData["threadId"] = selectedThread.Id;
                ViewData["ThreadViewHtml"] = _stubble.RenderThreadFromTemplate(selectedThread);
            }
            else
                ViewData["ThreadViewHtml"] = _stubble.RenderThreadFromTemplate(null);

            return View("~/Views/Thread/Thread.cshtml");
        }

        [HttpGet]
        [Route("/api/PagedThreads/{startIndex?}/{maxResults?}")]
        public async Task<IEnumerable<ListViewModel>> Get(int startIndex = 0, int maxResults = 10)
        {
            List<Thread> threads = await _dbContext.GetPagedThreadsListAsync(_userId, startIndex, maxResults);
            return _mapper.Map<List<Thread>, List<ListViewModel>>(threads);
        }

        [HttpGet]
        [Route("/api/Thread/{threadId}")]
        public async Task<IActionResult> Get(int threadId)
        {
            Thread thread = await _dbContext.GetThreadAsync(threadId, _userId);
            if (thread == null)
            {
                return NotFound();
            }
            return new ObjectResult(_mapper.Map<Thread, SelectedViewModel>(thread));
        }

        [HttpPost]
        [Route("/api/Thread")]
        public async Task<IActionResult> Create([FromBody] string title)
        {
            if (string.IsNullOrEmpty(title))
            {
                return BadRequest();
            }
            Thread newThread = await _dbContext.CreateThreadAsync(title, _userId);
            return CreatedAtRoute(new { id = newThread.Id }, _mapper.Map<Thread, Models.CommentViewModels.DetailViewModel>(newThread));
        }
    }
}
