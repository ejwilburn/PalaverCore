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

/*  Functionality just for the Index.cshtml page */
var _editorDefaultHeight = 100;
var _srConnection;
var _allowBack = false;
var _pageTitle = 'Palaver';
var HUB_ACTION_RETRY_DELAY = 5000; // in ms
var NOTIFICATION_SNIPPET_SIZE = 100;
var NOTIFICATION_DURATION = 5000; // In ms
var wowhead_tooltips = { "colorlinks": true, "iconizelinks": true, "renamelinks": true };
window.templates = {};

$(document).ready(function() {
    initPage();
});

function initPage() {
    // Register our primary key event handler for the page.
    $(document).keydown(pageKeyDown);

    $('#reconnectingModal').modal({
        backdrop: 'static',
        keyboard: false,
        show: false
    });

    // Load mustache templates for rendering new content.
    loadTemplates();

    // Setup signalr connection.
    startSignalr();

    // Update the page title.
    updateTitle();

    // Load wowhead script after the page is loaded to stop it from blocking.
    $.getScript('//wow.zamimg.com/widgets/power.js');
}

function showDisconnected() {
    $('#reconnectingModal').modal('show');
}

function hideDisconnected() {
    $('#reconnectingModal').modal('hide');
}

function startSignalr() {
    _srConnection = $.connection.signalrHub;
    _srConnection.client.addThread = addThread;
    _srConnection.client.addOwnThread = addOwnThread;
    _srConnection.client.addComment = addComment;
    _srConnection.client.addOwnComment = addOwnComment;

    $.connection.hub.error(function(error) {
        if (console) {
            if (error)
                console.log(error);
            else
                console.log('Unknown SignalR hub error.');
        }
    });

    $.connection.hub.connectionSlow(function() {
        if (console)
            console.log('SignalR is currently experiencing difficulties with the connection.');
    });

    $.connection.hub.reconnecting(function() {
        showDisconnected();
        if (console)
            console.log('SignalR connection lost, reconnecting.');
    });

    // Try to reconnect every 5 seconds if disconnected.
    $.connection.hub.disconnected(function() {
        showDisconnected();
        if (console)
            console.log('SignlR lost its connection, reconnecting in 5 seconds.');

        setTimeout(function() {
            if (console)
                console.log('SignlR delayed reconnection in progress.');
            startHub();
        }, 5000); // Restart connection after 5 seconds.
    });

    $.connection.hub.reconnected(function() {
        if (console)
            console.log('SignalR reconnected.');

        triedReconnecting = false;
        hideDisconnected();
    });

    startHub();
}

function startHub() {
    $.connection.hub.logging = true;
    $.connection.hub.start().done(function() {
        triedReconnecting = false;
        hideDisconnected();
        if (console)
            console.log("Connected, transport = " + $.connection.hub.transport.name);
    });
}

function loadTemplates() {
    $('script[type=x-mustache-template]').each(function(index) {
        var name = this.attributes.name.value;
        $.get(this.src, function(data) {
            window.templates[name] = data;
        });
    });
}

function addThread(thread) {
    notifyNewThread(thread);
    updateTitle();

    $('#threads').prepend(Mustache.render(window.templates.threadListItem, thread));
}

function addOwnThread(thread) {
    thread.UnreadCount = 0;
    var element = $(Mustache.render(window.templates.threadListItem, thread));
    $('#threads').prepend(element);
    loadOwnThread(thread);
    writeReply();
}

function addComment(comment) {
    notifyNewComment(comment);

    var renderedComment = $(Mustache.render(window.templates.comment, comment));
    if (typeof comment.ParentCommentId === 'number')
        $('#thread .panel[data-id="' + comment.ParentCommentId + '"]')[0].children('.panel').append(renderedComment);
    else
        $(renderedComment).insertAfter('#thread h3');

    popThread(comment.ThreadId);
    updateTitle();
}

function addOwnComment(comment) {
    var renderedComment = Mustache.render(window.templates.comment, comment);
    if (typeof comment.ParentCommentId === 'number')
        $('#thread .panel[data-id="' + comment.ParentCommentId + '"]').children('.panel').append(renderedComment);
    else
        $('#thread').children('.panel').append(renderedComment);
    $('#thread [data-id="' + comment.Id + '"]').focus();

    popThread(comment.ThreadId);
    updateTitle();
}

// Move the new comment's thread to the top of the thread list.
function popThread(threadId) {
    var thread = $('#threads [data-id="' + threadId + '"]');
    thread.prependTo('#threads');
    // Update the thread's time.
    var now = new Date();
    var hour = now.getHours(),
        minutes, ampm = 'AM';
    if (hour === 0)
        hour = 12;
    else if (hour === 12)
        ampm = 'PM';
    else if (hour > 12) {
        hour -= 12;
        ampm = 'PM';
    }
    if (now.getMinutes() < 10)
        minutes = '0' + now.getMinutes();
    else
        minutes = now.getMinutes().toString();
    $(thread).children('.threadTime').html('[' + hour + ':' + minutes + ' ' + ampm + ']');
}

function formatDateTime(value) {
    var pattern = /Date\(([^)]+)\)/;
    var results = pattern.exec(value);
    var dt = new Date(parseFloat(results[1]));
    return dt.toLocaleTimeString();
}

// Notify the user of new threads
function notifyNewThread(thread) {
    if (!Notification || Notification.permission !== "granted")
        return;

    var title = 'Palaver thread posted by ' + thread.UserName + '.';
    var filteredThread = {
        Title: $.trim(stripHtml(thread.Title).substring(0, NOTIFICATION_SNIPPET_SIZE))
    };

    var notification = new Notification(title, {
        icon: _baseUrl + 'images/new_message-icon.gif',
        body: Mustache.render(window.templates.threadNotification, filteredThread)
    });
    notification.onclick = function() {
        window.focus();
        window.location.href = _baseUrl + 'Thread/' + thread.Id;
        this.cancel();
    };
    setTimeout(function() { if (notification) notification.close(); }, NOTIFICATION_DURATION);
}

// Notify the user of new comments
function notifyNewComment(comment) {
    if (!Notification || Notification.permission !== "granted")
        return;

    var title = 'Palaver comment posted by ' + comment.UserName + '.';
    var filteredComment = {
        Text: $.trim(stripHtml(comment.Text).substring(0, NOTIFICATION_SNIPPET_SIZE))
    };

    var notification = new Notification(title, {
        icon: _baseUrl + 'images/new_message-icon.gif',
        body: filteredMessage
    });
    notification.onclick = function() {
        window.focus();
        _commentId = commentId;
        loadThread(threadId);
        this.cancel();
    };
    setTimeout(function() { if (notification) notification.close(); }, NOTIFICATION_DURATION);
}

// Strip the message of any HTML using a temporary div.
function stripHtml(text) {
    if (typeof text !== 'string' || text.length === 0)
        return '';

    var tempDiv = document.createElement('DIV');
    tempDiv.innerHTML = text;
    return innerDiv.textContent || innerDiv.innerText;
}

function updateTitle() {
    var threadCounts = $('#threads.unread .badge');
    var totalUnread = 0;

    // Loop through the thread counters and total them.
    if (threadCounts.length > 0) {
        for (var x = 0; x < threadCounts.length; x++) {
            var countString = $(threadCounts[x]).innerText;
            if (typeof countString === 'string' && countString.length > 0) {
                totalUnread += parseInt(countString);
            }
        }
    }

    if (totalUnread > 0)
        document.title = '*' + totalUnread + '* ' + _pageTitle;
    else
        document.title = _pageTitle;
}

function markRead(comment) {
    if ($(comment).hasClass('unread')) {
        $(comment).removeClass('unread');

        var id = $(comment).attr("data-id");
        var data = { 'id': id };

        updateThreadUnread($(comment).attr("data-threadId"));

        jQuery.put(_baseUrl + 'api/Comment/MarkRead/' + id)
            .fail(function(error) {
                if (console) {
                    console.error('Error marking comment ' + id + ' as read.');
                    console.error(error);
                }
            });
    }
    updateTitle();
}

function updateThreadUnread(threadId) {
    // First, get our current unread count.
    var unreadCount = $('#thread .unread').length;

    // If the count is greater than 0, simply update the unread counter next to the thread.
    // Otherwise, clear the unread counter for the thread.
    var thread = $('#threads[data-id="' + threadId + '"]');
    var unreadBadge = thread.children('.badge');
    if (unreadCount > 0) {
        unreadBadge.html(unreadCount.toString());
        thread.addClass('unread');
    } else {
        unreadBadge.html('0');
        thread.removeClass('unread');
    }

    // Update the page title with the unread count.
    updateTitle();
}

function incrementThreadUnread(threadId) {
    // Get the thread.
    var thread = $('#threads[data-id="' + threadId + '"]');
    // Get the thread's unread count span.
    var threadCounter = thead.children('.badge');
    // If the counter is empty, set it to (1)
    var threadCounterHtml = threadCounter.html();
    if (threadCounterHtml === null || threadCounterHtml.length === 0 || threadCounterHtml === '0')
        threadCounter.html('1');
    else {
        // Convert the counter to an int and increment it.
        var count = parseInt(threadCounterHtml) + 1;
        // Update the page display.
        threadCounter.html(count.toString());
    }

    // Make sure that thread has the unread class.
    thread.addClass('unread');

    // Update the page title with the unread count.
    updateTitle();
}

function loadOwnThread(thread) {
    _threadId = thread.Id;

    // Change our URL.
    _allowBack = false;
    history.pushState({ threadId: thread.Id }, document.title, _baseUrl + 'Thread/' + thread.Id);
    _allowBack = true;

    var element = $(Mustache.render(window.templates.thread, thread));
    $('#thread').html(element);
    $('#thread h3').focus();
    selectThread(thread.Id);
}

function loadThread(id) {
    _threadId = id;

    // Change our URL.
    _allowBack = false;
    if (typeof _commentId === 'number')
        history.pushState({ threadId: id, commentId: _commentId }, document.title, _baseUrl + 'Thread/' + id + '/' + _commentId);
    else
        history.pushState({ threadId: id }, document.title, _baseUrl + 'Thread/' + id);
    _allowBack = true;

    // Blank out current comments change the class to commentsLoading.
    showBusy();

    $('#thread').html('');
    $.get(
        _baseUrl + 'api/RenderThread/' + _threadId,
        function(data) {
            $('#thread').html(data);
            clearBusy();
            if (typeof _commentId === 'number') {
                focusAndMarkReadCommentId(_commentId);
                _commentId = null;
            }
            selectThread(id);
            updateTitle();
        }
    ).fail(function(error) {
        if (console) {
            console.error('Error loading thread via AJAX.');
            console.error(error);
        }
        clearBusy();
    });
}

function selectThread(threadId) {
    $('#threads .active').removeClass('active');
    $('#threads [data-id="' + threadId + '"]').addClass('active');
}

function writeReply(id) {
    // Close other reply divs.
    try {
        $('#replyDivInner').summernote('destroy');
    } catch (ex) {
        // Do nothing.
    }
    $('#replyDiv').remove();

    var replyingTo;
    if (typeof threadId !== 'number' || threadId === null)
        replyingTo = $('#thread h3');
    else
        replyingTo = $('#thread .panel[data-id="' + id + '"]');

    // Create a new reply div.
    replyingTo.append('<div id="replyDiv" class="editor"><div id="replyDivInner" class="editor-inner"></div>' +
        '<a id="sendReplyLink" tabindex="1" href="javascript:;" onclick="sendReply(' + id + ')" class="submit">submit</a>' +
        '<a tabindex="2" href="javascript:;" onclick="cancelReply()">cancel</a>' +
        '</div>');
    $('#replyDivInner').summernote().on('summernote.keydown', function(we, e) { replyKeyDown(we, e, id); })
        .on('summernote.keyup', function(we, e) { replyKeyUp(we, e, id); })
        .on('summernote.image.upload', function(we, files) {
            imgurUpload(we, files);
        })
        .summernote('focus');
    $('#summernote').summernote({
        callbacks: {
            onImageUpload: function(files) {
                var fileUrl = imgurUpload(files);
            }
        }
    });
}

// Handle shift+enter to save and escape to cancel.
var shiftDown = false;

function replyKeyDown(we, e, commentId) {
    if (e.keyCode == 16) {
        shiftDown = true;
        return true;
    } else if (e.keyCode == 13) {
        SendReply(commentId);
        return false;
    } else if (e.keyCode == 27) {
        CancelReply();
        return false;
    }
    return true;
}

function replyKeyUp(we, e, commentId) {
    if (e.keyCode == 16) {
        shiftDown = false;
    }
    return true;
}

function cancelReply() {
    $('#replyDivInner').summernote('destroy');
    $('#replyDiv').remove();
}

function sendReply(parentCommentId) {
    var text = $('#replyDivInner').summernote('code');
    $('#replyDivInner').summernote('destroy');
    $('#replyDiv').remove();

    // Make sure all URLs in the reply have a target.  If not, set it to _blank.
    // We're doing this by using a fake DIV with jquery to find the links.
    var tempDiv = document.createElement('DIV');
    tempDiv.innerHTML = (typeof text !== 'string' ? '' : text);
    var links = $(tempDiv).children('a').each(function(index) {
        if (!$(this).attr('target'))
            $(this).attr('target', '_blank');
    });

    newComment({
        "Text": tempDiv.innerHTML,
        "ThreadId": _threadId,
        "ParentCommentId": parentCommentId
    });
}

function newComment(comment) {
    showBusy();
    if (typeof comment.ParentCommentId !== 'number')
        comment.ParentCommentId = null;
    _srConnection.server.newComment(comment.Text, comment.ThreadId, comment.ParentCommentId).done(function() {
        clearBusy();
    }).fail(function(error) {
        newCommentFailed(error, comment);
        clearBusy();
    });
}

function newCommentFailed(error, title) {
    if (console) {
        console.error(error);
        console.info('Retrying in ' + HUB_ACTION_RETRY_DELAY / 1000 + ' seconds...');
    }

    setTimeout(function() {
        newComment(comment);
    }, HUB_ACTION_RETRY_DELAY);
}

function newThread(title) {
    if (typeof title !== 'string') {
        title = $('#newthread').val();
        $('#newthread').val('');
    }

    if (typeof title !== 'string' || title.length === 0) {
        alert('Unable to determine thread title.');
    }

    showBusy();
    _srConnection.server.newThread(title).done(function() {
        clearBusy();
    }).fail(function(error) {
        newThreadFailed(error, title);
        clearBusy();
    });
}

function showBusy() {
    $(document).addClass('busy');
}

function clearBusy() {
    $(document).removeClass('busy');
}

function newThreadFailed(error, title) {
    if (console) {
        console.error(error);
        console.info('Retrying in ' + HUB_ACTION_RETRY_DELAY / 1000 + ' seconds...');
    }

    setTimeout(function() {
        newThread(title);
    }, HUB_ACTION_RETRY_DELAY);
}

function imgurUpload(files) {
    $.ajax({
        url: 'https://api.imgur.com/3/image',
        type: 'POST',
        headers: {
            Authorization: 'Client-ID fb944f4922deb66',
            Accept: 'application/json'
        },
        data: {
            type: 'base64',
            image: files
        },
        success: function(result) {
            var image = $('<img>').attr('src', 'https://imgur.com/gallery/' + result.data.id);
            $('#summernote').summernote("insertNode", image[0]);
        },
        error: function(xhr, ajaxOptions, thrownError) {
            alert('Error uploading image to imgur.  Status: ' + xhr.status + ' Thrown error: ' + thrownError +
                ' Response: ' + xhr.responseText);
        }
    });
}

function pageKeyDown(e) {
    // If we're entering a new comment, skip checking for unread.
    var focusedInput = $('input:focus');
    if (typeof focusedInput !== 'string' || focusedInput.length === 0) {
        if (e.keyCode == 32) { // space
            goToNextUnread();
            return false;
        } else if (e.keyCode == 82) { // 'r'
            // Find the selected comment if there is one.
            // If not, find the first comment.
            var focusedComment = $('.comment:focus');
            var comment;
            if (typeof focusedComment === 'string' && focusedComment.length > 0)
                comment = focusedComment[0];
            else {
                comments = $('.comment');
                if (comments !== null && comments.length > 0)
                    comment = comments[0];
                else
                    return false; // No comments, do nothing.
            }

            // Reply to this comment if shift isn't pressed.
            // Indented reply if shift is pressed.
            if (e.shiftKey === false) {
                var parentId = $(comment).attr("data-parent-id");
                writeReply(parentId);
            } else {
                var id = $(comment).attr("data-id");
                writeReply(id);
            }
            return false;
        } else if (e.keyCode == 39 || e.keyCode == 40 || e.keyCode == 78) { // right, down, 'n'
            focusNext();
            return false;
        } else if (e.keyCode == 37 || e.keyCode == 38 || e.keyCode == 80) { // left, up, 'p'
            focusPrev();
            return false;
        }
    }
    return true;
}

function goToNextUnread() {
    var unreadItems = $('#thread .unread');
    // Exit if there are no unread items.
    if (unreadItems === null || unreadItems.length === 0)
        return;
    // If there is only one, focus on that one and mark it read.
    if (unreadItems.length == 1) {
        focusAndMarkRead($(unreadItems[0]));
        return;
    }
    // Get the currently selected item.
    var focusedComments = $('#thread .panel:focus');
    var focusedId;
    // If we don't have a focused comment, focus and mark the first unread comment as read.
    if (focusedComments === null || focusedComments.length === 0) {
        focusAndMarkRead($(unreadItems[0]));
        return;
    } else
        focusedId = $(focusedComments[0]).attr('data-id');

    // Find the next unread item.
    var nextUnread = findNextUnreadComment(focusedId);
    // If there isn't a next one, just focus & mark the first.
    if (nextUnread === null)
        focusAndMarkRead($(unreadItems[0]));
    else
        focusAndMarkRead(nextUnread);
}

function focusAndMarkRead(unreadComment) {
    focusComment(unreadComment);
    markRead(unreadComment);
}

function focusCommentId(commentId) {
    focusComment($('#thread div[data-id="' + commentId + '"]'));
}

function focusAndMarkReadCommentId(commentId) {
    var comment = $('#thread div[data-id="' + commentId + '"]');
    focusComment(comment);
    markRead(comment);
}

function focusComment(comment) {
    $(comment).scrollTo();
    // $('#thread').scrollTop($('#thread').scrollTop() + ($(comment).position().top - $('#thread').offset().top));
    // $(comment).focus();
}

function focusNext() {
    // Find all comments, exit if there are none.
    var comments = $('#thread .panel');
    if (comments === null || comments.length === 0)
        return;

    // If we only have one comment, focus on that one.
    if (comments.length == 1) {
        $(comments[0]).focus();
        return;
    }

    // Find the first focused comment.
    var focusedComments = $('#thread .panel:focus');
    var focusedId;
    // If we don't have a focused comment, find the first one and focus that then exit.
    if (focusedComments === null || focusedComments.length === 0) {
        $(comments[0]).focus();
        return;
    } else
        focusedId = $(focusedComments[0]).attr('data-id');

    // Find the next comment after this one and focus it.
    var nextComment = findNextComment(focusedId);
    if (nextComment !== null)
        nextComment.focus();
}

function findNextComment(commentId) {
    // Find all comments, exit if there are none.
    var comments = $('#thread .panel');
    if (comments === null || comments.length === 0)
        return;

    // Loop through our comment divs until we get to the currentId, then return the one after that.
    // If we don't find the Id or there isn't another comment after this one, return null.
    for (var x = 0; x < comments.length; x++) {
        if ($(comments[x]).attr('data-id') == commentId) {
            if (x + 1 < comments.length)
                return $(comments[x + 1]);

            // We found the id but there are no comments after, return null.
            return null;
        }
    }

    // No comment matched the pass Id, return null.
    return null;
}

function findNextUnreadComment(commentId) {
    // Find all comments, exit if there are none.
    var comments = $('#thread .panel');
    if (comments === null || comments.length === 0)
        return;

    // Loop through our comment divs until we get to the currentId, then return the one after that.
    // If we don't find the Id or there isn't another comment after this one, return null.
    for (var x = 0; x < comments.length; x++) {
        if ($(comments[x]).attr('data-id') == commentId) {
            // We found our selected comment, loop through the rest
            // of the comments until we find one that's unread.
            // If none are, return null.
            for (var y = x + 1; y < comments.length; y++) {
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

function focusPrev() {
    // Find all comments, exit if there are none.
    var comments = $('#thread .panel');
    if (comments === null || comments.length === 0)
        return;

    // If we only have one comment, focus on that one.
    if (comments.length == 1) {
        $(comments[0]).focus();
        return;
    }

    // Find the first focused comment.
    var focusedComments = $('#thread .panel:focus');
    var focusedId;
    // If we don't have a focused comment, find the last one and focus that then exit.
    if (focusedComments === null || focusedComments.length === 0) {
        comments.last().focus();
        return;
    } else
        focusedId = $(focusedComments[0]).attr('data-id');

    // Loop through our comment divs until we get to our currently selected one, then focus the one before that, if there is one.
    for (var x = comments.length - 1; x > -1; x--) {
        if ($(comments[x]).attr('data-id') == focusedId) {
            if (x - 1 > -1)
                $(comments[x - 1]).focus();

            return;
        }
    }
}