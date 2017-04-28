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

// jshint esversion:6

/*  Functionality just for the Thread.cshtml page */

const PAGE_TITLE = 'Palaver';
const HUB_ACTION_RETRY_DELAY = 5000; // in ms
const INITIAL_SIGNALR_CONNECTION_RETRY_DELAY = 200; // in ms
const NOTIFICATION_SNIPPET_SIZE = 100; // in characters
const NOTIFICATION_DURATION = 5000; // In ms
const IMAGE_LOADING_TRANSITION_DURATION = 300; // in ms

const wowhead_tooltips = { "colorlinks": true, "iconizelinks": true, "renamelinks": true };

class Thread {
    constructor(threadId, commentId, userId) {
        // Thread-centric properties.
        this.threadId = threadId;
        this.commentId = commentId;
        this.totalUnread = 0;
        this.userId = userId;
        this.allowBack = false;

        // Editor related
        this.editor = null;
        this.editorForm = null;
        this.editingParentId = null;
        this.editing = null;
        this.editingCommentId = null;
        this.editingOrigText = null;

        // SignalR related
        this.srConnection = null;
        this.initialSignalrConnectionDone = false;

        // Mobile related
        this.isMobile = Util.isMobileDisplay();
        this.loadToFirstUnread = false;

        $(document).ready(() => this.initPage());
    }

    initPage() {
        $('#reconnectingModal').modal({
            backdrop: 'static',
            keyboard: false,
            show: false
        });

        this.startSignalr();

        window.onpopstate = (event) => {
            if (Util.isNumber(event.state.threadId))
                this.threadId = event.state.threadId;
            else
                return;

            if (Util.isNumber(event.state.commentId))
                this.commentId = event.state.commentId;

            this.loadThread(this.threadId, this.commentId, true);
        };

        $('#threadList').sidebar('setting', 'dimPage', false)
            .sidebar('setting', 'closable', false);

        // Close the sidebar if a thread is selected and this is a mobile
        // device.
        if (this.isMobile && Util.isNumber(this.threadId)) {
            $('#threadList').sidebar('hide');
        }

        $('#newthreadicon').on('click', () => { this.newThread(); });

        // Register the primary key event handler for the page.
        $(document).keydown((e) => { return this.pageKeyDown(e); });

        this.updateTotalUnreadDisplay();

        this.prepImages();
        this.formatDateTimes();

        if (!Util.isNumber(this.threadId))
            return;

        // Select the current thread if one is loaded.
        this.selectThread(this.threadId);
        if (Util.isNumber(this.commentId))
            this.focusCommentId(this.commentId);

        this.fixEditing();

        // Enable infinite scrolling of the thread list.
        let context = $('#threadList');
        let pane = $('#threads');
        $('#threads')
            .visibility({
                once: false,
                initialCheck: true,
                continuous: true,
                checkOnRefresh: true,
                //includeMargin: true,
                context: context,
                // update size when new content loads
                observeChanges: true,
                // load content on bottom edge visible
                // The onUpdate call is a temporary workaround to fix a bug in Semantic-UI 2.2.10 not firing onBottomVisible properly.
                onBottomVisible: () => {
                    //this.loadMoreThreads();
                },
                onUpdate: (e) => {
                    if (pane.height() - context.scrollTop() <= context.height())
                        this.loadMoreThreads();
                }
            });

        this.requestNotificationPermission();
    }

    openEditor(openAt, initialValue) {
        this.cancelReply();
        $(openAt).append(TemplateRenderer.render('editor'));
        this.editorForm = $('#editorForm');
        this.editor = CKEDITOR.replace('editor');
        this.editor.on('key', (e) => { return this.replyKeyPressed(e); });
        if (initialValue)
            this.editor.setData(initialValue);
        return this.editor;
    }

    cancelReply() {
        let wasEditing = this.editing,
            origText = this.editingOrigText;

        this.closeEditor();

        if (wasEditing) {
            wasEditing.html(origText);
        }
    }

    closeEditor() {
        if (this.editor) {
            this.editor.focusManager.blur(true);
            this.editor.destroy();
            this.editor = null;
            this.editing = null;
            this.editingParentId = null;
            this.editingCommentId = null;
            this.editingOrigText = null;
            $(this.editorForm).remove();
            this.editorForm = null;
        }
    }

    fixEditing() {
        $(`#thread .comment:not([data-authorid="${this.userId}"])>.content>.actions>.edit`).remove();
    }

    showDisconnected() {
        $('#reconnectingModal').modal('show');
    }

    hideDisconnected() {
        $('#reconnectingModal').modal('hide');
    }

    startSignalr() {
        this.srConnection = $.connection.signalrHub;
        this.srConnection.client.addThread = (thread) => { this.addThread(thread); };
        this.srConnection.client.showThread = (renderedThread) => { this.showThread(renderedThread); };
        this.srConnection.client.addToThreadsList = (threads) => { this.addToThreadsList(threads); };
        this.srConnection.client.addComment = (comment) => { this.addComment(comment); };
        this.srConnection.client.updateComment = (comment) => { this.updateComment(comment); };

        $.connection.hub.error(function(error) {
            if (console) {
                if (error)
                    console.log(error);
                else
                    console.log('Unknown SignalR hub error.');
            }
        });

        $.connection.hub.connectionSlow(() => {
            if (console)
                console.log('SignalR is currently experiencing difficulties with the connection.');
        });

        $.connection.hub.reconnecting(() => {
            this.showDisconnected();
            if (console)
                console.log('SignalR connection lost, reconnecting.');
        });

        // Try to reconnect every 5 seconds if disconnected.
        $.connection.hub.disconnected(() => {
            this.showDisconnected();
            if (console)
                console.log('SignlR lost its connection, reconnecting in 5 seconds.');

            setTimeout(() => {
                if (console)
                    console.log('SignalR delayed reconnection in progress.');
                this.startSignalrHub();
            }, 5000); // Restart connection after 5 seconds.
        });

        $.connection.hub.reconnected(() => {
            if (console)
                console.log('SignalR reconnected.');

            this.hideDisconnected();
        });

        this.startSignalrHub();
    }

    startSignalrHub() {
        $.connection.hub.logging = true;
        $.connection.hub.start().done(() => {
            this.hideDisconnected();
            this.initialSignalrConnectionDone = true;
            if (console)
                console.log("Connected, transport = " + $.connection.hub.transport.name);
            if (Util.isNumber(this.commentId)) {
                this.markReadByCommentId(this.commentId);
                this.commentId = null;
            }
        });
    }

    formatDateTimes() {
        $('time.timeago').timeago();
    }

    prepImages(element) {
        this.enableLazyImageLoading(element);
        Gifffer(); // Add play controls to animated gifs.
    }

    enableLazyImageLoading(element) {
        let enableFor = null;
        if (element)
            enableFor = $(element.find('.text img.lazy'));
        else
            enableFor = $('.comment>.content>.text img.lazy');

        enableFor.visibility({
            context: '#thread',
            type: 'image',
            transition: 'fade in',
            duration: IMAGE_LOADING_TRANSITION_DURATION
        });
    }

    loadMoreThreads() {
        $('#threadsLoader').addClass('active');

        if (!this.initialSignalrConnectionDone) {
            setTimeout(() => {
                this.loadMoreThreads();
            }, INITIAL_SIGNALR_CONNECTION_RETRY_DELAY);
            return;
        }

        let threadsList = $('#threads');
        let threadsCount = threadsList.children('.item').length;
        this.srConnection.server.getPagedThreadsList(threadsCount).fail((error) => {
            this.loadMoreThreadsFailed(error);
        });
    }

    addToThreadsList(threads) {
        $(threads).each((index, element) => {
            if ($(`#threads>.item[data-id="${element.Id}"]`).length === 0)
                $('#threads').append(TemplateRenderer.render('threadListItem', element));
        });
        $('#threadsLoader').removeClass('active');
    }

    loadMoreThreadsFailed(error) {
        if (console) {
            console.error(error);
            console.info(`Retrying in ${HUB_ACTION_RETRY_DELAY / 1000} seconds...`);
        }

        setTimeout(() => {
            this.loadMoreThreads();
        }, HUB_ACTION_RETRY_DELAY);
    }

    addThread(thread) {
        let isAuthor = thread.UserId === this.userId;

        if (isAuthor)
            thread.UnreadCount = 0;

        $('#threads').prepend(TemplateRenderer.render('threadListItem', thread));

        if (isAuthor) {
            this.loadOwnThread(thread);
        } else {
            this.notifyNewThread(thread);
            this.updateTotalUnreadDisplay();
        }
    }

    addComment(comment) {
        let isAuthor = comment.UserId === this.userId;
        let commentList = null,
            renderedComment = null;

        if (isAuthor)
            comment.IsUnread = false;
        else {
            comment.IsUnread = true;
            this.incrementThreadUnread(comment.ThreadId);
        }

        if (comment.ThreadId === this.threadId) {
            renderedComment = $(TemplateRenderer.render('comment', comment));
            if (Util.isNumber(comment.ParentCommentId))
                commentList = $(`#thread .comment[data-id="${comment.ParentCommentId}"]>.comments`);
            else
                commentList = $('#thread>.comments');

            renderedComment.appendTo(commentList);
            commentList.removeClass('hidden');
        }

        this.popThread(comment.ThreadId);

        if (isAuthor) {
            renderedComment.children('.edit').removeClass('hidden');
            this.closeEditor();
            this.focusCommentId(comment.Id);
            this.clearBusy();
        } else {
            this.updateTotalUnreadDisplay();
            this.notifyNewComment(comment);
        }

        if (comment.ThreadId === this.threadId) {
            this.prepImages(renderedComment);
            this.formatDateTimes();
            twttr.widgets.load(renderedComment.get());
        }
    }

    updateComment(comment) {
        let isAuthor = comment.UserId === this.userId;

        if (isAuthor) {
            this.closeEditor();
            this.focusCommentId(comment.Id);
            this.clearBusy();
        }

        let commentBody = $(`#thread .comment[data-id="${comment.Id}"]>.content>.text`);
        commentBody.html(comment.DisplayText);
        this.prepImages();
        this.formatDateTimes();
        twttr.widgets.load(commentBody.get());
    }

    // Move the new comment's thread to the top of the thread list.
    popThread(threadId) {
        let thread = $(`#threads [data-id="${threadId}"]`);
        thread.prependTo('#threads');
        // Update the thread's time.
        var curTime = new Date();
        let time = $(thread).find('time.timeago');
        time.attr('title', null);
        time.attr('datetime', curTime.toISOString());
        time.timeago();
    }

    requestNotificationPermission() {
        if (this.isNotificationAllowed() && Notification.permission === "default") {
            Notification.requestPermission();
        }
    }

    isNotificationAllowed() {
        if (!Notification || Notification.permission === "denied")
            return false;
        return true;
    }

    // Notify the user of new threads
    notifyNewThread(thread) {
        if (!this.isNotificationAllowed())
            return;

        let title = `Palaver thread posted by ${thread.UserName}.`;
        let filteredThread = {
            Title: $.trim(this.stripHtml(thread.Title).substring(0, NOTIFICATION_SNIPPET_SIZE))
        };

        let notification = new Notification(title, {
            icon: BASE_URL + 'images/new_message-icon.gif',
            body: TemplateRenderer.render('threadNotification', filteredThread)
        });
        notification.onclick = (event) => {
            event.preventDefault();
            event.currentTarget.close();
            window.focus();
            this.loadThread(thread.Id);
        };
        setTimeout(function() { if (notification) notification.close(); }, NOTIFICATION_DURATION);
    }

    // Notify the user of new comments
    notifyNewComment(comment) {
        if (!this.isNotificationAllowed())
            return;

        let title = `Palaver comment posted by ${comment.UserName}.`;
        let filteredComment = $.trim(this.stripHtml(comment.Text).substring(0, NOTIFICATION_SNIPPET_SIZE));

        let notification = new Notification(title, {
            icon: BASE_URL + 'images/new_message-icon.gif',
            body: TemplateRenderer.render('commentNotification', filteredComment)
        });
        notification.onclick = (event) => {
            event.preventDefault();
            event.currentTarget.close();
            window.focus();
            this.loadThread(comment.ThreadId, comment.Id);
        };
        setTimeout(function() { if (notification) notification.close(); }, NOTIFICATION_DURATION);
    }

    // Strip the message of any HTML using a temporary div.
    stripHtml(text) {
        if (!Util.isString(text) || Util.isNullOrEmpty(text))
            return '';

        let tempDiv = document.createElement('DIV');
        tempDiv.innerHTML = text;
        return tempDiv.textContent || tempDiv.innerText;
    }

    countAllUnread() {
        this.totalUnread = 0;
        let threadCounts = $('#threads .unread .unreadcount');

        // Loop through the thread counters and total them.
        if (threadCounts.length > 0) {
            for (let x = 0; x < threadCounts.length; x++) {
                let countString = $(threadCounts[x]).text();
                if (!Util.isNullOrEmpty(countString)) {
                    this.totalUnread += parseInt(countString);
                }
            }
        }
    }

    updateTotalUnreadDisplay() {
        this.countAllUnread();
        if (this.totalUnread > 0)
            document.title = `*${this.totalUnread}* ${PAGE_TITLE}`;
        else
            document.title = PAGE_TITLE;

        if (this.isMobile) {
            let mobileUnread = $('#mobileUnread');
            mobileUnread.text(this.totalUnread + ' Unread');
            if (this.totalUnread > 0) {
                mobileUnread.removeClass('secondary');
                mobileUnread.addClass('primary');
            } else {
                mobileUnread.removeClass('primary');
                mobileUnread.addClass('secondary');
            }
        }
    }

    markReadByCommentId(id) {
        this.markRead($(`#thread .comment[data-id="${id}"]`));
    }

    markRead(comment) {
        if ($(comment).hasClass('unread')) {
            $(comment).removeClass('unread');

            let commentId = $(comment).data('id');
            let threadId = this.threadId;

            this.showBusy();
            this.srConnection.server.markRead(threadId, commentId).done(() => {
                this.updateCurrentThreadUnread(threadId);
                this.clearBusy();
            }).fail((error) => {
                if (console) {
                    console.error(`Error marking comment ${id} as read.`);
                    console.error(error);
                }
                this.clearBusy();
            });
        }
        this.updateTotalUnreadDisplay();
    }

    updateCurrentThreadUnread(threadId) {
        // First, get our current unread count.
        let unreadCount = $('#thread .unread').length;

        // If the count is greater than 0, simply update the unread counter next to the thread.
        // Otherwise, clear the unread counter for the thread.
        let thread = $(`#threads [data-id="${threadId}"]`);
        let unreadCounter = thread.find('.unreadcount');
        if (unreadCount > 0) {
            unreadCounter.html(unreadCount.toString());
            unreadCounter.removeClass('hidden');
            thread.addClass('unread');
        } else {
            unreadCounter.html('0');
            unreadCounter.addClass('hidden');
            thread.removeClass('unread');
        }

        // Update the page title with the unread count.
        this.updateTotalUnreadDisplay();
    }

    incrementThreadUnread(threadId) {
        this.totalUnread++;
        // Get the thread.
        let thread = $(`#threads [data-id="${threadId}"]`);
        // If the counter is empty, set it to (1)
        let unreadCounter = thread.find('.unreadcount');
        let threadCounterHtml = unreadCounter.html();
        if (Util.isNullOrEmpty(threadCounterHtml) || threadCounterHtml === '0') {
            unreadCounter.html('1');
        } else {
            // Convert the counter to an int and increment it.
            let count = parseInt(threadCounterHtml) + 1;
            // Update the page display.
            unreadCounter.html(count.toString());
        }

        // Make sure that thread has the unread class.
        thread.addClass('unread');
        unreadCounter.removeClass('hidden');

        // Update the page title with the unread count.
        this.updateTotalUnreadDisplay();
    }

    loadOwnThread(thread) {
        this.threadId = thread.Id;

        // Change our URL.
        this.allowBack = false;
        history.pushState({ threadId: thread.Id }, document.title, `${BASE_URL}Thread/${thread.Id}`);
        this.allowBack = true;

        this.closeEditor();
        $('#thread').replaceWith(TemplateRenderer.render('thread', thread));
        this.selectThread(thread.Id);
        $('#thread').scrollTop(0);
        this.prepImages();
        this.formatDateTimes();
        this.openEditor('#thread .comments');
    }

    loadThread(threadId, commentId = null, isBack = false) {
        this.threadId = threadId;
        this.commentId = commentId;
        let haveCommentId = Util.isNumber(commentId);

        // Change our URL.
        if (!isBack) {
            this.allowBack = false;
            if (haveCommentId)
                history.pushState({ threadId: threadId, commentId: commentId }, document.title, `${BASE_URL}Thread/${threadId}/${commentId}`);
            else
                history.pushState({ threadId: threadId }, document.title, `${BASE_URL}Thread/${threadId}`);
            this.allowBack = true;
        }

        // Blank out current comments change the class to commentsLoading.
        this.showBusy();

        this.srConnection.server.loadThread(threadId).fail((error, threadId, commentId) => {
            this.loadThreadFailed(error, threadId, commentId);
            this.clearBusy();
        });
    }

    showThread(renderedThread) {
        let haveCommentId = Util.isNumber(this.commentId);

        this.closeEditor();
        $('#thread').replaceWith(renderedThread);
        if (haveCommentId) {
            this.focusAndMarkReadCommentId(this.commentId);
            this.commentId = null;
        }
        this.selectThread(this.threadId);
        this.updateTotalUnreadDisplay();
        this.fixEditing();
        this.prepImages();
        this.formatDateTimes();
        twttr.widgets.load();
        if (!haveCommentId)
            $('#thread').scrollTop(0);

        this.clearBusy();

        if (this.loadToFirstUnread) {
            this.loadToFirstUnread = false;
            this.goToNextUnread();
        }
    }

    loadThreadFailed(error, threadId, commentId) {
        if (console) {
            console.error(error);
            console.info(`Retrying in ${HUB_ACTION_RETRY_DELAY / 1000} seconds...`);
        }

        setTimeout(() => {
            this.loadThread(threadId, commentId);
        }, HUB_ACTION_RETRY_DELAY);
    }

    selectThread(threadId) {
        $('#threads .active').removeClass('active');
        $(`#threads [data-id="${threadId}"]`).addClass('active');
        if (this.isMobile)
            $('#threadList').sidebar('hide');
    }

    replyTo(parentId) {
        if (!Util.isNumber(parentId)) {
            // They're replying to the thread or a direct reply to a top level comment.
            this.openEditor($('#thread>.comments'));
            return;
        }
        this.editingParentId = parentId;
        this.openEditor($(`.comment[data-id="${parentId}"]>.comments`));
    }

    editComment(id) {
        this.editingCommentId = id;
        this.editing = $(`.comment[data-id="${id}"]>.content>.text`);
        this.editingOrigText = this.editing.html();
        this.editing.empty();
        this.openEditor(this.editing, this.editingOrigText);
    }

    replyKeyPressed(e) {
        // Save if shift+enter is pressed.
        if (e.data.keyCode == 2228237) {
            this.sendReply();
            return false;
        } else if (e.data.keyCode == 27) {
            this.cancelReply();
            return false;
        }
        return true;
    }

    sendReply() {
        let text = this.editor.getData();
        if (Util.isNullOrEmpty(text)) {
            alert("Replies cannot be empty.");
            return;
        }

        let parentCommentId = this.editingParentId;

        this.showBusy();

        // Make sure all URLs in the reply have a target.  If not, set it to _blank.
        // We're doing this by using a fake DIV with jquery to find the links.
        let tempDiv = document.createElement('DIV');
        tempDiv.innerHTML = text;
        let links = $(tempDiv).find('a').each(function(index) {
            if (!$(this).attr('target'))
                $(this).attr('target', '_blank');
        });

        if (!this.editing) {
            this.newComment({
                "Text": tempDiv.innerHTML,
                "ThreadId": this.threadId,
                "ParentCommentId": (!Util.isNumber(parentCommentId) ? null : parentCommentId)
            });
        } else {
            this.saveUpdatedComment({
                "Id": this.editingCommentId,
                "Text": tempDiv.innerHTML
            });
        }
    }

    newComment(comment) {
        this.showBusy();
        if (!Util.isNumber(comment.ParentCommentId))
            comment.ParentCommentId = null;
        this.srConnection.server.newComment(comment.Text, comment.ThreadId, comment.ParentCommentId).fail((error) => {
            this.newCommentFailed(error, comment);
            this.clearBusy();
        });
    }

    newCommentFailed(error, comment) {
        if (console) {
            console.error(error);
            console.info(`Retrying in ${HUB_ACTION_RETRY_DELAY / 1000} seconds...`);
        }

        setTimeout(() => {
            this.newComment(comment);
        }, HUB_ACTION_RETRY_DELAY);
    }

    saveUpdatedComment(comment) {
        this.showBusy();
        this.srConnection.server.editComment(comment.Id, comment.Text).fail((error) => {
            this.saveUpdatedCommentError(error, comment);
            this.clearBusy();
        });
    }

    saveUpdatedCommentError(error, comment) {
        if (console) {
            console.error(error);
            console.info(`Retrying in ${HUB_ACTION_RETRY_DELAY / 1000} seconds...`);
        }

        setTimeout(() => {
            this.saveUpdatedComment(comment);
        }, HUB_ACTION_RETRY_DELAY);
    }

    newThread() {
        let title = $('#newthread').val();

        if (!Util.isString(title) || Util.isNullOrEmpty(title)) {
            alert('Unable to determine thread title.');
        }

        this.showBusy();
        this.srConnection.server.newThread(title).done(() => {
            $('#newthread').val('');
            this.clearBusy();
        }).fail((error) => {
            this.newThreadFailed(error, title);
            this.clearBusy();
        });
    }

    showBusy() {
        $(document.body).css({ 'cursor': 'wait' });
    }

    clearBusy() {
        $(document.body).css({ 'cursor': 'default' });
    }

    newThreadFailed(error, title) {
        if (console) {
            console.error(error);
            console.info(`Retrying in ${HUB_ACTION_RETRY_DELAY / 1000} seconds...`);
        }

        setTimeout(() => {
            this.newThread(title);
        }, HUB_ACTION_RETRY_DELAY);
    }

    isEditorInUse() {
        if ($('input:focus').length > 0 || $('.jodit_editor:focus').length > 0 || $('textarea:focus').length > 0)
            return true;
        return false;
    }

    pageKeyDown(e) {
        // Only handle these key events if not in an editor or the new thread box.
        if (!this.isEditorInUse()) {
            if (e.keyCode == 32) { // space
                this.goToNextUnread();
                return false;
            } else if (e.keyCode == 82) { // 'r'
                // Find the selected comment if there is one.
                // If not, find the first comment.
                let focusedComment = $('#thread .comment:focus');
                let comment;
                if (focusedComment.length > 0)
                    comment = $(focusedComment[0]);
                else {
                    comment = $('#thread');
                }

                // Reply to this comment if shift isn't pressed.
                // Indented reply if shift is pressed.
                let replyToId = null;
                if (e.shiftKey === false) {
                    replyToId = comment.data('parentid');
                } else {
                    replyToId = comment.data('id');
                }

                this.replyTo(replyToId);
                return false;
            } else if (e.keyCode == 39 || e.keyCode == 40 || e.keyCode == 78) { // right, down, 'n'
                this.focusNext();
                return false;
            } else if (e.keyCode == 37 || e.keyCode == 38 || e.keyCode == 80) { // left, up, 'p'
                this.focusPrev();
                return false;
            }
        } else if ($('#newthread:focus').length > 0) {
            // If enter is pressed in the new thread box, add a thread.
            // If escape is pressed, clear the input and deselect it.
            if (e.keyCode == 13) {
                this.newThread();
                return false;
            } else if (e.keyCode == 27) {
                $('#newthread').val('').blur();
                return false;
            }
        }
        return true;
    }

    goToNextUnread() {
        let unreadItems = $('#thread .unread');
        // Exit if there are no unread items, except on mobile.
        // On mobile find the next thread with unread and load it.
        if (!unreadItems || unreadItems.length === 0) {
            if (this.isMobile)
                this.loadNextUnreadThread();
            return;
        }

        // If there is only one, focus on that one and mark it read.
        if (unreadItems.length == 1) {
            this.focusAndMarkRead($(unreadItems[0]));
            return;
        }
        // Get the currently selected item.
        let focusedComments = $('#thread .comment:focus');
        let focusedId;
        // If we don't have a focused comment, focus and mark the first unread comment as read.
        if (!focusedComments || focusedComments.length === 0) {
            this.focusAndMarkRead($(unreadItems[0]));
            return;
        } else
            focusedId = $(focusedComments[0]).data('id');

        // Find the next unread item.
        let nextUnread = this.findNextUnreadComment(focusedId);
        // If there isn't a next one, just focus & mark the first.
        if (Util.isNull(nextUnread))
            this.focusAndMarkRead($(unreadItems[0]));
        else
            this.focusAndMarkRead(nextUnread);
    }

    loadNextUnreadThread() {
        let unreadThreads = $('#threads>.unread');

        // Exit if no more threads with unread.
        if (!unreadThreads || unreadThreads.length === 0)
            return;

        this.loadToFirstUnread = true;
        this.loadThread(unreadThreads.data('id'), null, false);
    }

    focusAndMarkRead(unreadComment) {
        this.focusComment(unreadComment);
        this.markRead(unreadComment);
    }

    focusCommentId(commentId) {
        this.focusComment($(`#thread .comment[data-id="${commentId}"]`));
    }

    focusAndMarkReadCommentId(commentId) {
        this.focusAndMarkRead($(`#thread .comment[data-id="${commentId}"]`));
    }

    focusComment(comment) {
        let element = $('#thread');
        element.scrollTop(element.scrollTop() + ($(comment).position().top - element.offset().top));
        $(comment).focus();
    }

    focusNext() {
        // Find the first focused comment.
        let focusedComments = $('#thread .comment:focus');
        // If we don't have a focused comment, find the first one and focus that then exit.
        if (focusedComments.length === 0) {
            this.focusComment($('#thread .comment').first());
            return;
        }

        // Find the next comment after this one and focus it.
        let nextComment = this.findNextComment($(focusedComments[0]).data('id'));
        if (!Util.isNull(nextComment))
            this.focusComment(nextComment);
    }

    findNextComment(commentId) {
        // Find all comments, exit if there are none.
        let comments = $('#thread .comment');
        if (comments.length === 0)
            return null;

        // Loop through our comment divs until we get to the currentId, then return the one after that.
        // If we don't find the Id or there isn't another comment after this one, return null.
        for (let x = 0; x < comments.length; x++) {
            if ($(comments[x]).data('id') == commentId) {
                if (x + 1 < comments.length)
                    return $(comments[x + 1]);

                // We found the id but there are no comments after, return null.
                return null;
            }
        }

        // No comment matched the pass Id, return null.
        return null;
    }

    findNextUnreadComment(commentId) {
        // Find all comments, exit if there are none.
        let comments = $('#thread .comment');
        if (comments.length === 0)
            return null;

        // Loop through our comment divs until we get to the currentId, then return the one after that.
        // If we don't find the Id or there isn't another comment after this one, return null.
        for (let x = 0; x < comments.length; x++) {
            if ($(comments[x]).data('id') == commentId) {
                // We found our selected comment, loop through the rest
                // of the comments until we find one that's unread.
                // If none are, return null.
                for (let y = x + 1; y < comments.length; y++) {
                    if ($(comments[y]).hasClass('unread'))
                        return $(comments[y]);
                }

                // We found the id but there are no unread comments after, return null.
                return null;
            }
        }

        // No comment matched the pass Id, return null.
        return null;
    }

    focusPrev() {
        // Find all comments, exit if there are none.
        let comments = $('#thread .comment');
        if (comments.length === 0)
            return;

        // If we only have one comment, focus on that one.
        if (comments.length == 1) {
            this.focusComment($(comments)[0]);
            return;
        }

        // Find the first focused comment.
        let focusedComments = $('#thread .comment:focus');
        // If we don't have a focused comment, find the last one and focus that then exit.
        if (focusedComments.length === 0) {
            this.focusComment(comments.last());
            return;
        }

        // Loop through our comment divs until we get to our currently selected one, then focus the one before that, if there is one.
        let focusedId = $(focusedComments[0]).data('id');
        for (let x = comments.length - 1; x > -1; x--) {
            if ($(comments[x]).data('id') == focusedId) {
                if (x - 1 > -1)
                    this.focusComment($(comments[x - 1]));

                return;
            }
        }
    }
}