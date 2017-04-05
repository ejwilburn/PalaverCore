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

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Palaver.Data;
using Palaver.Models;
using Palaver.Models.CommentViewModels;
using System.Collections.Generic;

namespace Palaver.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class CommentController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly PalaverDbContext _dbContext;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly int _userId;

        public CommentController(PalaverDbContext dbContext, UserManager<User> userManager, IMapper mapper,
            IHttpContextAccessor httpContextAccessor, ILoggerFactory loggerFactory)
        {
            this._dbContext = dbContext;
            this._userManager = userManager;
            this._mapper = mapper;
            this._httpContextAccessor = httpContextAccessor;
            this._logger = loggerFactory.CreateLogger<CommentController>();
            this._userId = int.Parse(_userManager.GetUserId(_httpContextAccessor.HttpContext.User));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            Comment comment = await _dbContext.GetCommentAsync(id, _userId);
            if (comment == null)
            {
                return NotFound();
            }
            return new ObjectResult(_mapper.Map<Comment, DetailViewModel>(comment));
        }

        [HttpGet("Search/{searchText}")]
        public async Task<SearchResultsViewModel> Search(string searchText)
        {
            SearchResultsViewModel results = new SearchResultsViewModel();
            List<Comment> comments = await _dbContext.Search(searchText);
            if (comments != null && comments.Count > 0)
            {
                results.results = _mapper.Map<List<Comment>, List<SearchResultViewModel>>(comments);
                results.success = true;
            }
            else
            {
                results.success = false;
            }
            return results;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateViewModel comment)
        {
            if (string.IsNullOrEmpty(comment.Text))
            {
                return BadRequest();
            }
            Comment newComment = await _dbContext.CreateCommentAsync(comment.Text, comment.ThreadId, comment.ParentCommentId, await GetUserAsync());
            return CreatedAtRoute(new { id = newComment.Id }, _mapper.Map<Comment, CreateResultViewModel>(newComment));
        }

        [HttpPut("{id}")]
        [Route("MarkRead/{id}")]
        public async Task<IActionResult> MarkRead(int id)
        {
            await _dbContext.MarkCommentReadByUser(id, _userId);
            return new NoContentResult();
        }

        private async Task<User> GetUserAsync()
        {
            return await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
        }
    }
}
