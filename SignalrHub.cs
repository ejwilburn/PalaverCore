using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Hubs;
using Palaver.Data;
using Palaver.Models;

namespace Palaver
{
    public class SignalrHub : IHub, IDisposable
    {
        public IHubCallerConnectionContext<dynamic> Clients { get; set; }
        public HubCallerContext Context { get; set; }
        public IGroupManager Groups { get; set; }

        private readonly PalaverDbContext _dbContext;
        private readonly UserManager<User> _userManager;
        private static Dictionary<string, int> _userIdsByConnection = new Dictionary<string, int>();

        public SignalrHub(PalaverDbContext dbContext, UserManager<User> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
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
            int userId = GetUserId();
            Thread newThread = await _dbContext.CreateThreadAsync(threadTitle, userId);

            Clients.All.addThread(newThread);
        }

        /// <summary>
        /// Create a new reply in an existing thread.
        /// Adding a reply to an existing thread will notify all subscribed and connected clients.
        /// </summary>
        /// <param name="parentId">Id of the parent comment.</param>
        /// <param name="replyText">Text of the reply.</param>
        public async Task NewComment(string commentText, int threadId, int? parentId)
        {
            int userId = GetUserId();
            Comment newComment = await _dbContext.CreateCommentAsync(commentText, threadId, parentId, userId);

            Clients.All.addComment(newComment);
        }

        private int GetUserId()
        {
            return int.Parse(_userManager.GetUserId((ClaimsPrincipal)Context.User));
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
