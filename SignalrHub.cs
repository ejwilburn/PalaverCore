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

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Hubs;
using AutoMapper;
using Palaver.Data;
using Palaver.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Palaver
{
    public class SignalrHub : IHub, IDisposable
    {
        public IHubCallerConnectionContext<dynamic> Clients { get; set; }
        public HubCallerContext Context { get; set; }
        public IGroupManager Groups { get; set; }

        private readonly PalaverDbContext _dbContext;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private static Dictionary<string, int> _userIdsByConnection = new Dictionary<string, int>();

        public SignalrHub(PalaverDbContext dbContext, UserManager<User> userManager, IMapper mapper)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _mapper = mapper;
        }

        public Task OnConnected()
        {
            _userIdsByConnection.Add(Context.ConnectionId, GetUserId());
            System.Diagnostics.Debug.WriteLine("Connected ConnectionId: " + Context.ConnectionId);
            return Clients.Client(Context.ConnectionId).SetConnectionId(Context.ConnectionId);
        }

        public Task OnDisconnected(bool stopCalled)
        {
            _userIdsByConnection.Remove(Context.ConnectionId);
            if (stopCalled)
                System.Diagnostics.Debug.WriteLine("Disconnected ConnectionId: " + Context.ConnectionId);
            else
                System.Diagnostics.Debug.WriteLine("Lost Connection on ConnectionId: " + Context.ConnectionId);
            return Clients.Client(Context.ConnectionId).Remove();
        }

        public Task OnReconnected()
        {
            _userIdsByConnection.Add(Context.ConnectionId, GetUserId());
            System.Diagnostics.Debug.WriteLine("Reconnected ConnectionId: " + Context.ConnectionId);
            return Clients.Client(Context.ConnectionId).SetConnectionId(Context.ConnectionId);
        }

        public Task Subscribe(int threadId)
        {
            return Groups.Add(Context.ConnectionId, threadId.ToString());
        }

        public Task Unsubscribe(int threadId)
        {
            return Groups.Remove(Context.ConnectionId, threadId.ToString());
        }

        /// <summary>
        /// Create a new thread.
        /// Creating a thread will subscribe all users to it and notify any connected clients.
        /// </summary>
        /// <param name="threadTitle">Text of the thread subject.</param>
        public async Task NewThread(string threadTitle)
        {
            Thread newThread = await _dbContext.CreateThreadAsync(threadTitle, GetUserId());
            Palaver.Models.ThreadViewModels.CreateResultViewModel resultView = _mapper.Map<Thread, Palaver.Models.ThreadViewModels.CreateResultViewModel>(newThread);
            Clients.Caller.addOwnThread(resultView);
            Clients.Others.addThread(resultView);
        }

        /// <summary>
        /// Create a new reply in an existing thread.
        /// Adding a reply to an existing thread will notify all subscribed and connected clients.
        /// </summary>
        /// <param name="parentId">Id of the parent comment.</param>
        /// <param name="replyText">Text of the reply.</param>
        public async Task NewComment(string commentText, int threadId, int? parentId)
        {
            User curUser = await GetUserAsync();
            Comment newComment = await _dbContext.CreateCommentAsync(commentText, threadId, parentId, curUser);
            Palaver.Models.CommentViewModels.CreateResultViewModel resultView = _mapper.Map<Comment, Palaver.Models.CommentViewModels.CreateResultViewModel>(newComment);
            Clients.Caller.addOwnComment(resultView);
            Clients.Others.addComment(resultView);
        }

        public async Task MarkRead(int id)
        {
            _dbContext.Remove(new UnreadComment { UserId = GetUserId(), CommentId = id });
            try 
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // This means the unreadcomment is already deleted, ignore it.
            }
        }

        private int GetUserId()
        {
            return int.Parse(_userManager.GetUserId((ClaimsPrincipal)Context.User));
        }

        private async Task<User> GetUserAsync()
        {
            return await _userManager.GetUserAsync((ClaimsPrincipal)Context.User);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
