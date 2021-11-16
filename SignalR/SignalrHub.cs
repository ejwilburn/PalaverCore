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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PalaverCore.Data;
using PalaverCore.Models;
using PalaverCore.Models.CommentViewModels;
using PalaverCore.Models.ThreadViewModels;
using PalaverCore.Services;

namespace PalaverCore.SignalR
{
    [Authorize]
    public class SignalrHub : Hub
    {
        private static Dictionary<string, string> _threadWatchedByConnection = new Dictionary<string, string>();

        private readonly PalaverDbContext _dbContext;
        private readonly StubbleRendererService _stubble;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;

        public SignalrHub(PalaverDbContext dbContext, StubbleRendererService stubble, UserManager<User> userManager,
            IMapper mapper, ILoggerFactory loggerFactory)
        {
            this._dbContext = dbContext;
            this._stubble = stubble;
            this._userManager = userManager;
            this._mapper = mapper;
            this._logger = loggerFactory.CreateLogger<SignalrHub>();
        }

        public override async Task OnConnectedAsync()
        {
            System.Diagnostics.Debug.WriteLine("Connected ConnectionId: " + Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (exception != null)
                System.Diagnostics.Debug.WriteLine("Disconnected ConnectionId: " + Context.ConnectionId);
            else
                System.Diagnostics.Debug.WriteLine("Lost Connection on ConnectionId: " + Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task WatchThread(int threadId)
        {
            if (_threadWatchedByConnection.ContainsKey(Context.ConnectionId) && _threadWatchedByConnection[Context.ConnectionId] != null)
                await UnsubscribeFromGroup(_threadWatchedByConnection[Context.ConnectionId]);
            string group = threadId.ToString();
            await SubscribeToGroup(group);
        }

        public async Task SubscribeToGroup(string group)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, group);
            lock (_threadWatchedByConnection)
            {
                _threadWatchedByConnection[Context.ConnectionId] = group;
            }
        }

        public async Task UnsubscribeFromGroup(string group)
        {
            bool isSubscribed = false;
            lock(_threadWatchedByConnection)
            {
                if (_threadWatchedByConnection.ContainsKey(Context.ConnectionId))
                {
                    if (_threadWatchedByConnection[Context.ConnectionId] != null)
                        isSubscribed = true;

                    _threadWatchedByConnection.Remove(Context.ConnectionId);
                }
            }
            if (isSubscribed)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        }

        public async Task GetPagedThreadsList(int startIndex)
        {
            List<Thread> threads = await _dbContext.GetPagedThreadsListAsync(GetUserId(), startIndex);
            await Clients.Client(Context.ConnectionId).SendAsync("addToThreadsList", _mapper.Map<List<Thread>, List<ListViewModel>>(threads));
        }

        /// <summary>
        /// Request an HTML rendered thread.  The result is sent back to the client via a remote call.
        /// </summary>
        /// <param name="threadId">Thread Id</param>
        public async Task LoadThread(int threadId)
        {
            Thread selectedThread = await _dbContext.GetThreadAsync(threadId, GetUserId());
            await WatchThread(threadId);
            string output = _stubble.RenderThreadFromTemplate(_mapper.Map<Thread, SelectedViewModel>(selectedThread));
            await Clients.Client(Context.ConnectionId).SendAsync("showThread", output);
        }

        /// <summary>
        /// Create a new thread.
        /// Creating a thread will subscribe all users to it and notify any connected clients.
        /// </summary>
        /// <param name="threadTitle">Text of the thread subject.</param>
        public async Task NewThread(string threadTitle)
        {
            if (String.IsNullOrWhiteSpace(threadTitle))
            {
                throw new Exception("The thread title cannot be empty.");
            }

            Thread newThread = await _dbContext.CreateThreadAsync(threadTitle, GetUserId());
            PalaverCore.Models.ThreadViewModels.CreateResultViewModel resultView = _mapper.Map<Thread, PalaverCore.Models.ThreadViewModels.CreateResultViewModel>(newThread);
            await WatchThread(newThread.Id);
            await Clients.All.SendAsync("addThread", resultView);
        }

        /// <summary>
        /// Create a new reply in an existing thread.
        /// Adding a reply to an existing thread will notify all subscribed and connected clients.
        /// </summary>
        /// <param name="parentId">Id of the parent comment.</param>
        /// <param name="replyText">Text of the reply.</param>
        public async Task NewComment(string commentText, int threadId, int? parentId)
        {
            if (String.IsNullOrWhiteSpace(commentText))
            {
                throw new Exception("The comment text cannot be empty.");
            }

            User curUser = await GetUserAsync();
            Comment newComment = await _dbContext.CreateCommentAsync(commentText, threadId, parentId, curUser);
			Models.CommentViewModels.DetailViewModel resultView = _mapper.Map<Comment, Models.CommentViewModels.DetailViewModel>(newComment);
            await Clients.Client(Context.ConnectionId).SendAsync("addComment", resultView);
            Models.CommentViewModels.DetailViewModel othersView = _mapper.Map<Comment, Models.CommentViewModels.DetailViewModel>(newComment);
            othersView.IsAuthor = false;
            await Clients.AllExcept(new List<String>{ Context.ConnectionId }).SendAsync("addComment", othersView);
        }

        /// <summary>
        /// Edit an existing comment.  The editor must be the creator of the comment or an exception is thrown.
        /// Subscribers will get an updated comment but the unread flag will not be changed.
        /// </summary>
        /// <param name="parentId">Id of the parent comment.</param>
        /// <param name="replyText">Text of the reply.</param>
        public async Task EditComment(int commentId, string commentText)
        {
            if (String.IsNullOrWhiteSpace(commentText))
            {
                throw new Exception("The comment text cannot be empty.");
            }

            Comment existingComment = await _dbContext.Comments.Where(c => c.Id == commentId)
                .Include(c => c.Thread)
                .Include(c => c.User)
                .SingleOrDefaultAsync();
            if (existingComment == null)
            {
                throw new Exception($"Unable to find comment id {commentId}.");
            }

            if (existingComment.UserId != GetUserId())
            {
                throw new Exception("Ony the comment creator may edit a comment.");
            }

            existingComment.Text = commentText;
            await _dbContext.SaveChangesAsync();

            EditResultViewModel resultView = _mapper.Map<Comment, EditResultViewModel>(existingComment);
            await Clients.All.SendAsync("updateComment", resultView);
        }

        public async Task MarkRead(int threadId, int commentId)
        {
            await _dbContext.MarkCommentReadByUser(threadId, commentId, GetUserId());
        }

        private int GetUserId()
        {
            return int.Parse(_userManager.GetUserId((ClaimsPrincipal)Context.User));
        }

        private async Task<User> GetUserAsync()
        {
            return await _userManager.GetUserAsync((ClaimsPrincipal)Context.User);
        }
    }
}
