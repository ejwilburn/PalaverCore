/*
Copyright 2017, Marcus McKinnon, E.J. Wilburn, Kevin Williams
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
using Microsoft.Extensions.Logging;
using AutoMapper;
using Palaver.Data;
using Palaver.Services;
using Palaver.Models;
using Palaver.Models.ThreadViewModels;

namespace Palaver.Controllers
{
    [Authorize]
    public class ThreadController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly PalaverDbContext _dbContext;
        private readonly CustomHtmlHelperService _htmlHelper;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly int _userId;

        public ThreadController(PalaverDbContext context, UserManager<User> userManager, IMapper mapper,
            IHttpContextAccessor httpContextAccessor, ILoggerFactory loggerFactory, CustomHtmlHelperService htmlHelper)
        {
            this._dbContext = context;
            this._htmlHelper = htmlHelper;
            this._userManager = userManager;
            this._mapper = mapper;
            this._httpContextAccessor = httpContextAccessor;
            this._logger = loggerFactory.CreateLogger<ThreadController>();
            this._userId = int.Parse(_userManager.GetUserId(_httpContextAccessor.HttpContext.User));
        }

        // Adding this just for the default route until I figure out how to do it right.
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return await Show();
        }

        [HttpGet("show")]
        [Route("Thread")]
        public async Task<IActionResult> Show()
        {
            List<ListViewModel> threads = _mapper.Map<List<Thread>, List<ListViewModel>>(await _dbContext.GetThreadsListAsync(_userId));
            ViewData["userId"] = _userId;
            ViewData["ThreadListViewHtml"] = _htmlHelper.RenderThreadListFromTemplate(threads);
            return View("~/Views/Thread/Thread.cshtml");
        }

        [HttpGet]
        [Route("Thread/{id}")]
        public async Task<IActionResult> Show(int id)
        {
            List<ListViewModel> threads = _mapper.Map<List<Thread>, List<ListViewModel>>(await _dbContext.GetThreadsListAsync(_userId));
            SelectedViewModel selectedThread = _mapper.Map<Thread, SelectedViewModel>(await _dbContext.GetThreadAsync(id, _userId));
            ViewData["threadId"] = selectedThread.Id;
            ViewData["userId"] = _userId;
            ViewData["ThreadListViewHtml"] = _htmlHelper.RenderThreadListFromTemplate(threads);
            ViewData["ThreadViewHtml"] = _htmlHelper.RenderThreadFromTemplate(selectedThread);
            return View("~/Views/Thread/Thread.cshtml");
        }

        [HttpGet]
        [Route("Thread/{id}/{commentId}")]
        public async Task<IActionResult> Show(int id, int commentId)
        {
            List<ListViewModel> threads = _mapper.Map<List<Thread>, List<ListViewModel>>(await _dbContext.GetThreadsListAsync(_userId));
            SelectedViewModel selectedThread = _mapper.Map<Thread, SelectedViewModel>(await _dbContext.GetThreadAsync(id, _userId));
            ViewData["threadId"] = selectedThread.Id;
            ViewData["commentId"] = commentId;
            ViewData["userId"] = _userId;
            ViewData["ThreadListViewHtml"] = _htmlHelper.RenderThreadListFromTemplate(threads);
            ViewData["ThreadViewHtml"] = _htmlHelper.RenderThreadFromTemplate(selectedThread);
            return View("~/Views/Thread/Thread.cshtml");
        }

        [HttpGet]
        [Route("/api/Thread")]
        public async Task<IEnumerable<ListViewModel>> Get()
        {
            List<Thread> threads = await _dbContext.GetThreadsListAsync(_userId);
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
            return CreatedAtRoute(new { id = newThread.Id }, _mapper.Map<Thread, CreateResultViewModel>(newThread));
        }

        [HttpGet]
        [Route("/api/RenderThread/{id}")]
        public async Task<string> ShowThreadPartial(int id)
        {
            List<Thread> threads = await _dbContext.GetThreadsListAsync(_userId);
            Thread selectedThread = await _dbContext.GetThreadAsync(id, _userId);
            return _htmlHelper.RenderThreadFromTemplate(_mapper.Map<Thread, SelectedViewModel>(selectedThread));
        }
    }
}
