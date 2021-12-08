/*
Copyright 2021, E.J. Wilburn, Marcus McKinnon, Kevin Williams
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

/*  Functionality just for the Thread.cshtml page */

const PAGE_TITLE = 'Palaver';
const HUB_ACTION_RETRY_DELAY = 5000; // in ms
const INITIAL_SIGNALR_CONNECTION_RETRY_DELAY = 200; // in ms
const NOTIFICATION_SNIPPET_SIZE = 100; // in characters
const NOTIFICATION_DURATION = 5000; // In ms
const IMAGE_LOADING_TRANSITION_DURATION = 300; // in ms

const wowhead_tooltips = { "colorlinks": true, "iconizelinks": true, "renamelinks": true };

class Thread {

    $thread = null;
    $threadList = null;
    $threads = null;
    threadId = null;
    commentId = null;
    totalUnread = 0;
    userId = null;
    allowBack = false;
    isMobile = Util.isMobileDisplay();
    loadToFirstUnread = false;

    signalr = {
        logger: null,
        conn: null,
        dcTime: null
    };

    constructor(threadId, commentId, userId) {
        // Thread-centric properties.
        this.threadId = threadId;
        this.commentId = commentId;
        this.userId = userId;

        this.editor = new Editor(this);

        $(document).ready(() => this.initPage());
    }

    async initPage() {
        this.$thread = $('#thread');
        this.$threadList = $('#threadList');
        this.$threads = $('#threads');

        $('#reconnectingModal').modal({
            backdrop: 'static',
            keyboard: false,
            show: false
        });

        window.onpopstate = (event) => {
            if (Util.isNumber(event.state.threadId))
                this.threadId = event.state.threadId;
            else
                return;

            if (Util.isNumber(event.state.commentId))
                this.commentId = event.state.commentId;

            this.loadThread(this.threadId, this.commentId, true);
        };

        this.$threadList.sidebar('setting', 'dimPage', false)
            .sidebar('setting', 'closable', false);

        // Close the sidebar if a thread is selected and this is a mobile
        // device.
        if (this.isMobile && Util.isNumber(this.threadId)) {
            this.$threadList.sidebar('hide');
        }

        $('#newthreadicon').on('click', () => { this.newThread(); });

        // Register the primary key event handler for the page.
        $(document).keydown((e) => { return this.pageKeyDown(e); });

        this.updateTotalUnreadDisplay();

        this.prepImages();
        this.formatDateTimes();
        Prism.highlightAll();

        // Select the current thread if one is loaded.
        if (Util.isNumber(this.threadId)) {
            this.selectThread(this.threadId);
            if (Util.isNumber(this.commentId))
                this.focusCommentId(this.commentId);
        }

        this.requestNotificationPermission();

        await this.startSignalr();

        // Enable infinite scrolling of the thread list.
        this.$threads.visibility({
            once: false,
            initialCheck: true,
            continuous: true,
            checkOnRefresh: true,
            includeMargin: true,
            context: this.$threadList,
            // update size when new content loads
            observeChanges: true,
            // load content on bottom edge visible
            // The onUpdate call is a temporary workaround to fix a bug in Semantic-UI 2.2.10 not firing onBottomVisible properly.
            onBottomVisible: () => {
                //this.loadMoreThreads();
            },
            onUpdate: (e) => {
                if (this.$threads.height() - this.$threadList.scrollTop() <= this.$threadList.height())
                    this.loadMoreThreads();
            }
        });
    }

    enableInfiniteThreadList() {
    }

    showDisconnected() {
        $('#reconnectingModal').modal('show');
    }

    hideDisconnected() {
        $('#reconnectingModal').modal('hide');
    }

    async startSignalr() {
        if (console)
            console.info('Connecting to SignalR hub...');

        let sr = this.signalr;
        sr.conn = new signalR.HubConnectionBuilder()
            .withUrl(BASE_URL + "threads")
            .configureLogging(signalR.LogLevel.Information)
            .build();

        sr.conn.onClosed = (e) => {
            if (e) {
                this.showDisconnected();
                if (console)
                    console.error('Connection closed with error: ' + e);
                setTimeout(() => { this.startSignalr(); }, 5000); // Restart connection after 5 seconds.
            } else {
                if (console)
                    console.info('Disconnected');
            }
        };

        sr.conn.on('addThread', (thread) => { this.addThread(thread); });
        sr.conn.on('showThread', (renderedThread) => { this.showThread(renderedThread); });
        sr.conn.on('addToThreadsList', (threads) => { this.addToThreadsList(threads); });
        sr.conn.on('addComment', (comment) => { this.addComment(comment); });
        sr.conn.on('updateComment', (comment) => { this.updateComment(comment); });
        sr.conn.on('setEditorComment', (comment) => { this.setEditorComment(comment); });

        try
        {
            await sr.conn.start();
            if (console)
                console.info('SignalR connected.');
            this.hideDisconnected();
            if (Util.isNumber(this.commentId)) {
                this.markReadByCommentId(this.commentId);
                this.commentId = null;
            }
        } catch (ex) {
            if (console)
                console.error(ex);
            setTimeout(() => { this.startSignalr(); }, 5000); // Restart connection after 5 seconds.
        }
    }

    formatDateTimes(element) {
        let targets = null;
        if (element)
            targets = $(element).find('time.timeago-new');
        else
            targets = $('time.timeago-new');

        targets.timeago().removeClass('timeago-new');
    }

    prepImages() {
        if (!this.blazy)
            this.initBlazy();
        else
            this.blazy.revalidate();
        Gifffer(); // Add play controls to animated gifs.
    }

    initBlazy() {
        if (this.blazy)
            this.blazy.destroy();
        this.blazy = new Blazy({
            container: '#thread',
            offset: 500,
            success: function(element) {
                setTimeout(function() {
                    $(element.parentNode).removeClass('loading');
                }, 200);
            }
        });
    }

    async loadMoreThreads() {
        $('#threadsLoader').addClass('active');

        let threadsCount = this.$threads.children('.item').length;

        try {
            await this.signalr.conn.invoke('getPagedThreadsList', threadsCount);
        } catch (ex) {
            this.loadMoreThreadsFailed(error);
        }
    }

    addToThreadsList(threads) {
        $(threads).each((index, element) => {
            if (this.$threads.children(`.item[data-id="${element.Id}"]`).length === 0)
                this.$threads.append(TemplateRenderer.render('threadListItem', element));
        });
        this.formatDateTimes(this.$threads);
        $('#threadsLoader').removeClass('active');
    }

    loadMoreThreadsFailed(error) {
        if (console) {
            console.error(error);
            console.warn(`Retrying in ${HUB_ACTION_RETRY_DELAY / 1000} seconds...`);
        }

        setTimeout(() => {
            this.loadMoreThreads();
        }, HUB_ACTION_RETRY_DELAY);
    }

    addThread(thread) {
        let isAuthor = thread.UserId === this.userId;

        if (isAuthor)
            thread.UnreadCount = 0;

        this.$threads.prepend(TemplateRenderer.render('threadListItem', thread));

        if (isAuthor) {
            this.loadOwnThread(thread);
        } else {
            this.notifyNewThread(thread);
            this.updateTotalUnreadDisplay();
        }
    }

    addComment(comment) {
        let commentList = null,
            renderedComment = null;

        if (comment.IsAuthor)
            comment.IsUnread = false;
        else {
            comment.IsUnread = true;
            this.incrementThreadUnread(comment.ThreadId);
        }

        if (comment.ThreadId === this.threadId) {
            renderedComment = $(TemplateRenderer.render('comment', comment));
            if (Util.isNumber(comment.ParentCommentId))
                commentList = this.$thread.find(`.comment[data-id="${comment.ParentCommentId}"]>.comments`);
            else
                commentList = this.$thread.children('.comments');

            renderedComment.appendTo(commentList);
            commentList.removeClass('hidden');
        }

        this.popThread(comment.ThreadId);

        if (comment.IsAuthor) {
            this.editor.closeEditor();
            this.focusCommentId(comment.Id);
            this.clearBusy();
        } else {
            this.updateTotalUnreadDisplay();
            this.notifyNewComment(comment);
        }

        if (comment.ThreadId === this.threadId) {
            this.prepImages();
            this.formatDateTimes(renderedComment);
            Prism.highlightAll();
            twttr.widgets.load(renderedComment.get());
        }
    }

    updateComment(comment) {
        let isAuthor = comment.UserId === this.userId;

        if (isAuthor) {
            this.editor.closeEditor();
            this.focusCommentId(comment.Id);
            this.clearBusy();
        }

        let commentElement = this.$thread.find(`.comment[data-id="${comment.Id}"]`);
        let commentBody = commentElement.find('.content>.text');
        commentBody.html(comment.DisplayText);
        this.prepImages();
        Prism.highlightAll();
        twttr.widgets.load(commentBody.get());
    }

    setEditorComment(comment) {
        this.clearBusy();
        this.editor.setComment(comment);
    }

    // Move the new comment's thread to the top of the thread list.
    popThread(threadId) {
        let thread = this.$threads.find(`[data-id="${threadId}"]`);
        thread.prependTo(this.$threads);
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
        let threadCounts = this.$threads.find('.unread .unreadcount');

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
        this.markRead(this.$thread.find(`.comment[data-id="${id}"]`));
    }

    markRead(comment) {
        if ($(comment).hasClass('unread')) {
            $(comment).removeClass('unread');

            let commentId = $(comment).data('id');
            let threadId = this.threadId;

            this.showBusy();
            this.signalr.conn.invoke('markRead', threadId, commentId).then(() => {
                this.updateCurrentThreadUnread(threadId);
                this.clearBusy();
            }).catch((error) => {
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
        let unreadCount = this.$thread.find('.unread').length;

        // If the count is greater than 0, simply update the unread counter next to the thread.
        // Otherwise, clear the unread counter for the thread.
        let thread = this.$threads.find(`[data-id="${threadId}"]`);
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
        let thread = this.$threads.find(`[data-id="${threadId}"]`);
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

        this.editor.closeEditor();
        this.$thread.replaceWith(TemplateRenderer.render('thread', thread));
        this.$thread = $('#thread');
        this.selectThread(thread.Id);
        this.$thread.scrollTop(0);
        this.formatDateTimes(this.$thread);
        this.editor.openEditor(this.$thread.children('.comments'));
    }

    loadThread(threadId, commentId = null, isBack = false) {
        this.threadId = threadId;
        this.commentId = commentId;
        let haveCommentId = Util.isNumber(commentId);

        // Blank out current comments change the class to commentsLoading.
        $('#threadLoader').addClass('active');

        // Change our URL.
        if (!isBack) {
            this.allowBack = false;
            if (haveCommentId)
                history.pushState({ threadId: threadId, commentId: commentId }, document.title, `${BASE_URL}Thread/${threadId}/${commentId}`);
            else
                history.pushState({ threadId: threadId }, document.title, `${BASE_URL}Thread/${threadId}`);
            this.allowBack = true;
        }

        this.signalr.conn.invoke('loadThread', threadId).catch((error, threadId, commentId) => {
            this.loadThreadFailed(error, threadId, commentId);
            this.clearBusy();
        });
    }

    showThread(renderedThread) {
        let haveCommentId = Util.isNumber(this.commentId);
        this.$threads.visibility('disable callbacks');
        let newContent = $(renderedThread);

        this.editor.closeEditor();
        this.$thread.replaceWith(newContent);
        this.$thread = $('#thread');
        if (haveCommentId) {
            this.focusAndMarkReadCommentId(this.commentId);
            this.commentId = null;
        }
        this.selectThread(this.threadId);
        this.updateTotalUnreadDisplay();
        if (!haveCommentId)
            this.$thread.scrollTop(0);

        $('#threadLoader').removeClass('active');

        if (this.loadToFirstUnread) {
            this.loadToFirstUnread = false;
            this.goToNextUnread();
        }
        setTimeout(() => { this.updateThreadDisplayAsync(); }, 100);
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

    updateThreadDisplayAsync() {
        this.initBlazy();
        this.prepImages();
        this.formatDateTimes(this.$thread);
        Prism.highlightAll();
        setTimeout(() => { twttr.widgets.load(this.$thread); }, 100);
        this.$threads.visibility('enable callbacks');
    }

    selectThread(threadId) {
        this.$threads.find('.active').removeClass('active');
        this.$threads.find(`[data-id="${threadId}"]`).addClass('active');
        if (this.isMobile)
            this.$threadList.sidebar('hide');
    }

    getCommentForEdit(commentId) {
        this.showBusy();
        this.signalr.conn.invoke('getCommentForEdit', commentId).catch((error) => {
            this.getCommentForEditFailed(error, commentId);
            this.clearBusy();
        });
    }

    getCommentForEditFailed(error, commentId) {
        if (console) {
            console.error(error);
            console.info(`Retrying in ${HUB_ACTION_RETRY_DELAY / 1000} seconds...`);
        }

        setTimeout(() => {
            this.getCommentForEdit(commentId);
        }, HUB_ACTION_RETRY_DELAY);
    }

    newComment(comment) {
        this.showBusy();
        if (!Util.isNumber(comment.ParentCommentId))
            comment.ParentCommentId = null;
        this.signalr.conn.invoke('newComment', comment.Text, comment.Format, comment.ThreadId, comment.ParentCommentId).catch((error) => {
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
        this.signalr.conn.invoke('editComment', comment.Id, comment.Text, comment.Format).catch((error) => {
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
        this.signalr.conn.invoke('newThread', title).then(() => {
            $('#newthread').val('');
            this.clearBusy();
        }).catch((error) => {
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

    pageKeyDown(e) {
        // Only handle these key events if not in an editor or the new thread box.
        if (!this.editor.isEditorInUse()) {
            if (e.keyCode == 32) { // space
                this.goToNextUnread();
                return false;
            } else if (e.keyCode == 82) { // 'r'
                // Find the selected comment if there is one.
                // If not, find the first comment.
                let focusedComment = this.$thread.find('.comment:focus');
                let comment;
                if (focusedComment.length > 0)
                    comment = $(focusedComment[0]);
                else {
                    comment = this.$thread;
                }

                // Reply to this comment if shift isn't pressed.
                // Indented reply if shift is pressed.
                let replyToId = null;
                if (e.shiftKey === false) {
                    replyToId = comment.data('parentid');
                } else {
                    replyToId = comment.data('id');
                }

                this.editor.replyTo(replyToId);
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
        let unreadItems = this.$thread.find('.unread');
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
        let focusedComments = this.$thread.find('.comment:focus');
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
        let unreadThreads = this.$threads.children('.unread');

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
        this.focusComment(this.$thread.find(`.comment[data-id="${commentId}"]`));
    }

    focusAndMarkReadCommentId(commentId) {
        this.focusAndMarkRead(this.$thread.find(`.comment[data-id="${commentId}"]`));
    }

    focusComment(comment) {
        this.$thread.scrollTop(this.$thread.scrollTop() + ($(comment).position().top - this.$thread.offset().top));
        $(comment).focus();
    }

    focusNext() {
        // Find the first focused comment.
        let focusedComments = this.$thread.find('.comment:focus');
        // If we don't have a focused comment, find the first one and focus that then exit.
        if (focusedComments.length === 0) {
            this.focusComment(this.$thread.find('.comment').first());
            return;
        }

        // Find the next comment after this one and focus it.
        let nextComment = this.findNextComment($(focusedComments[0]).data('id'));
        if (!Util.isNull(nextComment))
            this.focusComment(nextComment);
    }

    findNextComment(commentId) {
        // Find all comments, exit if there are none.
        let comments = this.$thread.find('.comment');
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
        let comments = this.$thread.find('.comment');
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
        let comments = this.$thread.find('.comment');
        if (comments.length === 0)
            return;

        // If we only have one comment, focus on that one.
        if (comments.length == 1) {
            this.focusComment($(comments)[0]);
            return;
        }

        // Find the first focused comment.
        let focusedComments = this.$thread.find('.comment:focus');
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