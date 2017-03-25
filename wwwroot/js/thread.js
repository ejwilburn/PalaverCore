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

// jshint esversion:6

/*  Functionality just for the Thread.cshtml page */

const PAGE_TITLE = 'Palaver';
const HUB_ACTION_RETRY_DELAY = 5000; // in ms
const NOTIFICATION_SNIPPET_SIZE = 100; // in characters
const NOTIFICATION_DURATION = 5000; // In ms
const EDITOR_DEFAULT_HEIGHT = 100; // in pixels
const wowhead_tooltips = { "colorlinks": true, "iconizelinks": true, "renamelinks": true };

class Thread {
    constructor(threadId, commentId, userId) {
        this.threadId = threadId;
        this.commentId = commentId;
        this.userId = userId;
        this.allowBack = false;
        this.editor = null;
        this.editorForm = null;
        this.srConnection = null;
        this.templates = {};
        this.editingParentId = null;
        this.editing = null;
        this.editingCommentId = null;
        this.editingOrigText = null;
        // this.UPLOAD_DIR = `${BASE_URL}uploads/${thread.userId}`;

        $(document).ready(() => this.initPage());
    }

    initPage() {
        window.onpopstate = (event) => {
            if (Object.isNumber(event.state.threadId))
                this.threadId = event.state.threadId;
            else
                return;

            if (Object.isNumber(event.state.commentId))
                this.commentId = event.state.commentId;

            this.loadThread(this.threadId, this.commentId, true);
        };

        $('#newthreadicon').on('click', () => { this.newThread(); });

        // Register the primary key event handler for the page.
        $(document).keydown((e) => { return this.pageKeyDown(e); });

        $('#reconnectingModal').modal({
            backdrop: 'static',
            keyboard: false,
            show: false
        });

        // Load mustache templates for rendering new content.
        this.loadTemplates();

        this.startSignalr();
        this.updateTitle();

        // Load wowhead script after the page is loaded to stop it from blocking.
        $.getScript('//wow.zamimg.com/widgets/power.js');

        if (!Object.isNumber(this.threadId))
            return;

        // Select the current thread if one is loaded.
        this.selectThread(this.threadId);
        if (Object.isNumber(this.commentId))
            this.focusCommentId(this.commentId);

        this.initEditor();
        this.enableEditing();
    }

    initEditor() {
        // Init the Jodit editor.
        this.editorForm = $('#editorForm');
        this.editorHome = $('#editorHome');
        this.editor = new Jodit('#editor', {
            language: 'en',
            minHeight: EDITOR_DEFAULT_HEIGHT,
            enableDragAndDropFileToEditor: true,
            uploader: {
                url: BASE_URL + 'api/FileHandler/AutoUpload',
                format: 'json',
                pathVariableName: 'path',
                filesVariableName: 'files',
                prepareData: function(data) {
                    return data;
                },
                isSuccess: function(resp) {
                    return resp.success;
                },
                getMsg: function(resp) {
                    return resp.message;
                },
                process: function(resp) {
                    return {
                        files: resp[this.options.uploader.filesVariableName] || [],
                        path: resp.path,
                        baseurl: BASE_URL,
                        error: !resp.success,
                        msg: resp.message
                    };
                },
                error: function(e) {
                    this.events.fire('errorPopap', [(e.getMessage !== undefined ? e.getMessage() : `Upload error ${e.status}: ${e.statusText}`), 'error', 4000]);
                },
                defaultHandlerSuccess: function(data, resp) {
                    var i, field = this.options.uploader.filesVariableName;
                    if (data[field] && data[field].length) {
                        for (i = 0; i < data[field].length; i += 1) {
                            this.selection.insertImage(data.baseurl + data[field][i]);
                        }
                    }
                },
                defaultHandlerError: function(resp) {
                    this.events.fire('errorPopap', [this.options.uploader.getMsg(resp)]);
                }
            },
            filebrowser: {
                // global setting for all operations
                ajax: {
                    async: true,
                    cache: true,
                    contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
                    method: 'POST',
                    dataType: 'json',
                    process: function(resp) {
                        return {
                            files: resp.files || [],
                            path: resp.path,
                            baseurl: resp.baseurl,
                            error: !resp.success,
                            msg: resp.message
                        };
                    }
                },
                uploader: {
                    url: BASE_URL + 'api/FileHandler/Upload'
                },
                // folder creation operation
                create: {
                    url: BASE_URL + 'api/FileHandler/CreateDir',
                },
                // operation of moving the folder / file
                move: {
                    url: BASE_URL + 'api/FileHandler/Move',
                },
                // the operation to delete the folder / file
                remove: {
                    url: BASE_URL + 'api/FileHandler/Delete',
                },
                // viewing a folder and return the list of files in it
                items: {
                    url: BASE_URL + 'api/FileHandler/ListFiles',
                },
                // viewing a folder and return a list of sub-folders in it
                folder: {
                    url: BASE_URL + 'api/FileHandler/ListDirs',
                }
            }
        });
        this.editor.$editor.on('keydown', (e) => { return this.replyKeyDown(e); });
        return this.editor;
    }

    cancelReply() {
        let wasEditing = null,
            origText = null;
        if (this.editing) {
            wasEditing = this.editing;
            origText = this.editingOrigText;
        }

        this.resetEditor();

        if (wasEditing) {
            wasEditing.html(origText);
        }
    }

    resetEditor() {
        if (this.editor) {
            this.editor.val('');
            this.editing = null;
            this.editingParentId = null;
            this.editingCommentId = null;
            this.editingOrigText = null;
            $(this.editor.$element).blur();
            this.editorHome.append(this.editorForm);
        } else {
            this.initEditor();
        }
    }

    enableEditing() {
        $(`#thread .comment[data-authorid="${this.userId}"] .edit`).removeClass('hidden');
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
                    console.log('SignlR delayed reconnection in progress.');
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
            if (console)
                console.log("Connected, transport = " + $.connection.hub.transport.name);
        });
    }

    loadTemplates() {
        $('script[type=x-mustache-template]').each((index, item) => {
            let name = item.attributes.name.value;
            $.get(item.src, (data) => { this.templates[name] = data; });
        });
    }

    addThread(thread) {
        let isAuthor = thread.UserId === this.userId;

        if (isAuthor)
            thread.UnreadCount = 0;

        $('#threads').prepend(Mustache.render(this.templates.threadListItem, thread));

        if (isAuthor) {
            this.loadOwnThread(thread);
        } else {
            this.notifyNewThread(thread);
            this.updateTitle();
        }
    }

    addComment(comment) {
        let isAuthor = comment.UserId === this.userId;
        let commentList = null;

        if (isAuthor)
            comment.IsUnread = false;

        let renderedComment = $(Mustache.render(this.templates.comment, comment));
        if (Object.isNumber(comment.ParentCommentId))
            commentList = $(`#thread .comment[data-id="${comment.ParentCommentId}"]>.comments`);
        else
            commentList = $('#thread>.comments');

        renderedComment.appendTo(commentList);
        commentList.removeClass('hidden');

        this.popThread(comment.ThreadId);

        if (isAuthor) {
            renderedComment.children('.edit').removeClass('hidden');
            this.resetEditor();
            this.focusComment(renderedComment);
            this.clearBusy();
        } else {
            this.updateTitle();
            this.notifyNewComment(comment);
        }
    }

    updateComment(comment) {
        let isAuthor = comment.UserId === this.userId;

        if (isAuthor) {
            this.resetEditor();
            this.focusCommentId(comment.Id);
            this.clearBusy();
        }

        $(`#thread .comment[data-id="${comment.Id}"] .text`).html(comment.Text);
    }

    // Move the new comment's thread to the top of the thread list.
    popThread(threadId) {
        let thread = $(`#threads [data-id="${threadId}"]`);
        thread.prependTo('#threads');
        // Update the thread's time.
        $(thread).children('.time').html(this.getCurrentTimeString());
    }

    getCurrentTimeString() {
        return (new Date()).toLocaleTimeString().replace(/:\d\d /, ' ');
    }

    // Notify the user of new threads
    notifyNewThread(thread) {
        if (!Notification || Notification.permission !== "granted")
            return;

        let title = `Palaver thread posted by ${thread.UserName}.`;
        let filteredThread = {
            Title: $.trim(stripHtml(thread.Title).substring(0, NOTIFICATION_SNIPPET_SIZE))
        };

        let notification = new Notification(title, {
            icon: BASE_URL + 'images/new_message-icon.gif',
            body: Mustache.render(this.templates.threadNotification, filteredThread)
        });
        notification.onclick = function() {
            window.focus();
            window.location.href = `${BASE_URL}Thread/${thread.Id}`;
            this.cancel();
        };
        setTimeout(function() { if (notification) notification.close(); }, NOTIFICATION_DURATION);
    }

    // Notify the user of new comments
    notifyNewComment(comment) {
        if (!Notification || Notification.permission !== "granted")
            return;

        let title = `Palaver comment posted by ${comment.UserName}.`;
        let filteredComment = {
            Text: $.trim(stripHtml(comment.Text).substring(0, NOTIFICATION_SNIPPET_SIZE))
        };

        let notification = new Notification(title, {
            icon: BASE_URL + 'images/new_message-icon.gif',
            body: filteredMessage
        });
        notification.onclick = () => {
            window.focus();
            this.loadThread(this.threadId, this.commentId);
            this.cancel();
        };
        setTimeout(function() { if (notification) notification.close(); }, NOTIFICATION_DURATION);
    }

    // Strip the message of any HTML using a temporary div.
    stripHtml(text) {
        if (!Object.isString(text) || text.isBlank())
            return '';

        let tempDiv = document.createElement('DIV');
        tempDiv.innerHTML = text;
        return innerDiv.textContent || innerDiv.innerText;
    }

    updateTitle() {
        let threadCounts = $('#threads .unread .unreadcount');
        let totalUnread = 0;

        // Loop through the thread counters and total them.
        if (threadCounts.length > 0) {
            for (let x = 0; x < threadCounts.length; x++) {
                let countString = $(threadCounts[x]).text();
                if (Object.isString(countString) && !countString.isBlank()) {
                    totalUnread += parseInt(countString);
                }
            }
        }

        if (totalUnread > 0)
            document.title = `*${totalUnread}* ${PAGE_TITLE}`;
        else
            document.title = PAGE_TITLE;
    }

    markRead(comment) {
        if ($(comment).hasClass('unread')) {
            $(comment).removeClass('unread');

            let id = $(comment).data('id');

            this.showBusy();
            this.srConnection.server.markRead(id).done(() => {
                this.updateThreadUnread(this.threadId);
                this.clearBusy();
            }).fail((error) => {
                if (console) {
                    console.error(`Error marking comment ${id} as read.`);
                    console.error(error);
                }
                this.clearBusy();
            });
        }
        this.updateTitle();
    }

    updateThreadUnread(threadId) {
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
        this.updateTitle();
    }

    incrementThreadUnread(threadId) {
        // Get the thread.
        let thread = $(`#threads [data-id="${threadId}"]`);
        // If the counter is empty, set it to (1)
        let unreadCounter = thread.find('.unreadcount');
        let threadCounterHtml = unreadLabel.html();
        if (threadCounterHtml === null || threadCounterHtml.length === 0 || threadCounterHtml === '0') {
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
        this.updateTitle();
    }

    loadOwnThread(thread) {
        this.threadId = thread.Id;

        // Change our URL.
        this.allowBack = false;
        history.pushState({ threadId: thread.Id }, document.title, `${BASE_URL}Thread/${thread.Id}`);
        this.allowBack = true;

        if (this.editor) {
            this.editor.destroy();
        }
        $('#thread').replaceWith(Mustache.render(this.templates.thread, thread));
        this.initEditor();
        this.selectThread(thread.Id);
        $('body').scrollTop(0);
        this.editor.$editor.focus();
    }

    loadThread(threadId, commentId = null, isBack = false) {
        this.threadId = threadId;
        this.commentId = commentId;
        let haveCommentId = Object.isNumber(commentId);

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

        $('#thread').html('');
        $.get(
            `${BASE_URL}api/RenderThread/${threadId}`,
            (data) => {
                if (this.editor) {
                    this.editor.destroy();
                }
                $('#thread').replaceWith(data);
                if (haveCommentId) {
                    this.focusAndMarkReadCommentId(commentId);
                    this.commentId = null;
                }
                this.selectThread(threadId);
                this.updateTitle();
                this.initEditor();
                this.enableEditing();
                $('body').scrollTop(0);
                this.clearBusy();
            }
        ).fail((error) => {
            this.clearBusy();
            if (console) {
                console.error('Error loading thread via AJAX.');
                console.error(error);
            }
        });
    }

    selectThread(threadId) {
        $('#threads .active').removeClass('active');
        $(`#threads [data-id="${threadId}"]`).addClass('active');
    }

    replyTo(parentId) {
        if (!Object.isNumber(parentId)) {
            // They're replying to the thread or a direct reply to a top level comment.
            this.focusComment(this.editorForm);
            this.editor.$editor.focus();
            return;
        }
        let replyingTo = $(`.comment[data-id="${parentId}"]`);

        // Move the editor to the comment being replied to.
        this.editor.val('');
        this.editingParentId = parentId;
        replyingTo.children('.comments').append(this.editorForm);
        this.focusComment(this.editorForm);
        this.editor.$editor.focus();
    }

    editComment(id) {
        // Move the editor to the comment being edited.
        this.editing = $(`.comment[data-id="${id}"] .text`);
        this.editingCommentId = id;
        this.editingOrigText = this.editing.html();
        this.editor.val(this.editingOrigText);
        this.editing.html('');
        this.editing.append(this.editorForm);
        this.focusCommentId(id);
        this.editor.$editor.focus();
    }

    replyKeyDown(e) {
        if (e.shiftKey && e.keyCode == 13) {
            this.sendReply();
            return false;
        } else if (e.keyCode == 27) {
            this.resetEditor();
            return false;
        }
        return true;
    }

    sendReply() {
        let text = this.editor.val();
        if (text.isBlank()) {
            alert("Replies cannot be empty.");
            return;
        }

        let parentCommentId = this.editingParentId;

        this.showBusy();

        // Make sure all URLs in the reply have a target.  If not, set it to _blank.
        // We're doing this by using a fake DIV with jquery to find the links.
        let tempDiv = document.createElement('DIV');
        tempDiv.innerHTML = text;
        let links = $(tempDiv).children('a').each(function(index) {
            if (!$(this).attr('target'))
                $(this).attr('target', '_blank');
        });

        if (!this.editing) {
            this.newComment({
                "Text": tempDiv.innerHTML,
                "ThreadId": this.threadId,
                "ParentCommentId": (!Object.isNumber(parentCommentId) ? null : parentCommentId)
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
        if (!Object.isNumber(comment.ParentCommentId))
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

        if (!Object.isString(title) || title.isBlank()) {
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
                    if (replyToId.length === 0)
                        replyToId = this.threadId;
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
        // Exit if there are no unread items.
        if (unreadItems === null || unreadItems.length === 0)
            return;
        // If there is only one, focus on that one and mark it read.
        if (unreadItems.length == 1) {
            this.focusAndMarkRead($(unreadItems[0]));
            return;
        }
        // Get the currently selected item.
        let focusedComments = $('#thread .comment:focus');
        let focusedId;
        // If we don't have a focused comment, focus and mark the first unread comment as read.
        if (focusedComments === null || focusedComments.length === 0) {
            this.focusAndMarkRead($(unreadItems[0]));
            return;
        } else
            focusedId = $(focusedComments[0]).data('id');

        // Find the next unread item.
        let nextUnread = this.findNextUnreadComment(focusedId);
        // If there isn't a next one, just focus & mark the first.
        if (nextUnread === null)
            this.focusAndMarkRead($(unreadItems[0]));
        else
            this.focusAndMarkRead(nextUnread);
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
        let body = $('body');
        body.scrollTop(body.scrollTop() + ($(comment).position().top - body.offset().top));
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
        if (nextComment !== null)
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