/*
Copyright 2012, Marcus McKinnon, E.J. Wilburn, Kevin Williams
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
var NOTIFICATION_SNIPPET_SIZE = 100;
var NOTIFICATION_DURATION = 5000; // In ms
var wowhead_tooltips = { "colorlinks": true, "iconizelinks": true, "renamelinks": true };

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
    _srConnection = $.connection.SignalrHub;
    _srConnection.client.addThread = addThread;
    _srConnection.client.addComment = addComment;

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
            console.log('SignalR is currently experiencing difficulties with the connection.')
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

function addThread(thread) {
    var newClass = '';
    var newBadge = '';
    if (_userId != thread.UserId) {
        newClass = ' class="unread"';
        newBadge = '<span class="badge">1</span>';
        notifyNewThread('Palaver thread posted by ' + thread.User.Name + '.', thread.Title, thread.ThreadId);
        updateTitle();
    } else
        window.location.hash = '#' + thread.ThreadId;

    $('#threads').prepend(
        '<li' + newClass + '><a href="#' + thread.Id + '" data-id="' + thread.Id + '"><span class="thread-date">[' +
        formatDateTime(thread.Updated) + ']</span> ' + thread.Title + ' ' + newBadge + '</a></li>'
    );
}

function addComment(comment) {
    var newClass = '';
    var newBadge = '';
    if (_userId != comment.UserId) {
        newClass = ' class="unread"';
        newBadge = '<span class="badge">9999</span>';
        notifyNewComment('Palaver comment posted by ' + comment.User.Name + '.', comment.Text, comment.ThreadId, comment.Id);
        //updateTitle();
    }

    if (_threadId == comment.ThreadId) {
        // TODO: Flesh this out.  Copy fromm a hidden template div.
        var commentHtml = '<div>' + comment.Text + '</div>';
        if (comment.ParentCommentId != null)
            $('#comments[data-id="' + comment.ParentCommentId + ' div.panel-body]').append(commentHtml);
        else
            $('#comments h3').append(commentHtml);
        $('#comments[data-id=' + comment.Id + ']').focus();
    }

    /*
    // get the comment's parent
    var commentItem = $('li[data-id="' + reply.parentId + '"]')[0];
    // Add the comment to the parent.
    $(commentItem).children('ul').append(reply.text);

    var newcomment = $('div[data-id="' + reply.commentId + '"]')[0];
    if (user != reply.userid) {
        $(newcomment).addClass('unread');
        $(newcomment).click(function() {
            markRead(this);
        });
        // Update the new comment count for the thread.
        incrementThreadUnread(reply.threadId);
        notify('Palaver comment posted by ' + reply.authorName + '.', reply.text, reply.threadId, reply.commentId);
    } else {
        $(newcomment).focus();
    }
    // Move the new comment's thread to the top of the thread list.
    var thread = $('li[data-thread-id="' + reply.threadId + '"]');
    $(thread).prependTo('div#threads ul');
    // Update the thread's time.
    var now = new Date();
    var hour = now.getHours(),
        minutes, ampm = 'AM';
    if (hour == 0)
        hour = 12;
    else if (hour == 12)
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
    */
    updateTitle();
}

function formatDateTime(value) {
    var pattern = /Date\(([^)]+)\)/;
    var results = pattern.exec(value);
    var dt = new Date(parseFloat(results[1]));
    return dt.toLocaleTimeString();
}

// Notify the user of new threads
function notifyNewThread(title, threadTitle, threadId) {
    if (!Notification || Notification.permission !== "granted")
        return;

    // Strip the message of any HTML using a temporary div.
    var tempDiv = document.createElement('DIV');
    tempDiv.innerHTML = (threadTitle == null ? '' : '<ul>' + threadTitle + '</ul>');
    var innerDiv = $(tempDiv).find('div')[0];
    $(innerDiv).children('span.user').remove();
    $(innerDiv).children('span.commentTime').remove();
    var filteredMessage = innerDiv.textContent || innerDiv.innerText;
    filteredMessage = $.trim(filteredMessage.substring(0, NOTIFICATION_SNIPPET_SIZE));
    var notification = new Notification(title, {
        icon: _baseUrl + 'images/new_message-icon.gif',
        body: filteredMessage
    });
    notification.onclick = function() {
        window.focus();
        window.location.href = _baseUrl + 'thread/' + threadId;
        this.cancel();
    };
    setTimeout(function() { if (notification) notification.close(); }, NOTIFICATION_DURATION);
}

// Notify the user of new comments
function notify(title, commentText, threadId, commentId) {
    if (!Notification || Notification.permission !== "granted")
        return;

    // Strip the message of any HTML using a temporary div.
    var tempDiv = document.createElement('DIV');
    tempDiv.innerHTML = (commentText == null ? '' : '<ul>' + commentText + '</ul>');
    var innerDiv = $(tempDiv).find('div')[0];
    $(innerDiv).children('span.user').remove();
    $(innerDiv).children('span.commentTime').remove();
    var filteredMessage = innerDiv.textContent || innerDiv.innerText;
    filteredMessage = $.trim(filteredMessage.substring(0, NOTIFICATION_SNIPPET_SIZE));
    var notification = new Notification(title, {
        icon: _baseUrl + 'images/new_message-icon.gif',
        body: filteredMessage
    });
    notification.onclick = function() {
        window.focus();
        _commentId = commentId;
        GetComments(threadId);
        this.cancel();
    };
    setTimeout(function() { if (notification) notification.close(); }, NOTIFICATION_DURATION);
}

function updateTitle() {
    var threadCounts = $('.unread');
    var totalUnread = 0;

    // Loop through the thread counters and total them.
    if (threadCounts.length > 0) {
        for (var x = 0; x < threadCounts.length; x++) {
            var countString = $(threadCounts[x]).html();
            if (countString != null && countString != '') {
                totalUnread += parseInt(countString.replace('(', '').replace(')', ''));
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

        jQuery.get(_baseUrl + 'api/markread', data);
    }
    updateTitle();
}

function updateThreadUnread(threadId) {
    // First, get our current unread count.
    var unreadCount = $('.unread').length;

    // If the count is greater than 0, simply update the unread counter next to the thread.
    // Otherwise, clear the unread counter for the thread.
    if (unreadCount > 0)
        $('#unreadCount' + threadId).html('(' + unreadCount + ')');
    else {
        $('#unreadCount' + threadId).html('');
        $('li[data-thread-id="' + threadId + '"]').removeClass('unread');
    }

    // Update the page title with the unread count.
    updateTitle();
}

function incrementThreadUnread(threadId) {
    // First, get the thread's unread count span.
    var threadCounter = $('#unreadCount' + threadId);
    // If the counter is empty, set it to (1)
    if (threadCounter.html() == null || threadCounter.html() == '')
        threadCounter.html('(1)');
    else {
        // Convert the counter to an int and increment it.
        var count = parseInt(threadCounter.html().replace('(', '').replace(')', '')) + 1;
        // Update the page display.
        threadCounter.html('(' + count + ')');
    }

    // Make sure that thread has the unread class.
    $('li[data-threadId="' + threadId + '"]').addClass('unread');

    // Update the page title with the unread count.
    updateTitle();
}

function GetComments(id) {
    GetComments(id, false);
}

function GetComments(id, isBack) {

    var thread = {
        'id': id
    };

    _threadId = id;

    // Change our URL.
    if (!isBack) {
        _allowBack = false;
        if (_commentId != null)
            history.pushState({ threadId: id, commentId: _commentId }, document.title, _baseUrl + 'thread/' + id + '/' + _commentId);
        else
            history.pushState({ threadId: id }, document.title, _baseUrl + 'thread/' + id);
        _allowBack = true;
    }
    // Blank out current comments change the class to commentsLoading.
    $(document).addClass('busy');

    $('#comments').html('');
    $.get(
        _baseUrl + 'threadPartial/' + _threadId; thread,
        function(data) {
            $('#comments').html(data);
            $('.unread').click(function() {
                markRead(this);
            });
            $(document).removeClass('busy');
            if (_commentId != null) {
                focusAndMarkReadCommentId(_commentId);
                _commentId = null;
            }
            selectThread(id);
            updateTitle();
        }
    );
}

function selectThread(threadId) {
    $('#threads .active').removeClass('active');
    $('#threads li[data-id="' + threadId + '"]').addClass('active');
}

function WriteReply(id) {
    // Close other reply divs.
    try {
        $('#replyBox').summernote('destroy');
    } catch (ex) {; // Do nothing.
    }
    $('#replyDiv').remove();

    // Create a new reply div.
    $('div.comment[data-id="' + id + '"]').append('<div id="replyDiv" style="margin-left: 20px;"><textarea id="replyBox" style="height: 15px; width: 100%; margin:0;" /><a id="sendReplyLink" tabindex="1" href="javascript:;" onclick="SendReply(' + id + ')" class="submit">submit</a><a tabindex="2" href="javascript:;" onclick="CancelReply()">cancel</a></div>');
    var replyBox = $('#replyBox');
    replyBox.summernote().on('summernote.keydown', function(we, e) { replyKeyDown(we, e, id); })
        .on('summernote.keyup', function(we, e) { replyKeyUp(we, e, id); })
        .on('summernote.image.upload', function(we, files) {
            imgurUpload(we, files);
        })
        .summernote('focus');
    $('#summernote').summernote({
        callbacks: {
            onImageUpload: function(files) {
                var fileUrl = imgurUpload(files)

            }
        }
    });
    /*
    var currScrollTop = $('#commentsArea').scrollTop();
    var scrollNeeded = currScrollTop + $('#sendReplyLink').position().top + $('#sendReplyLink').height() - ($('#commentsArea').offset().top + $('#commentsArea').height());
    if ( scrollNeeded > currScrollTop )
        $('#commentsArea').scrollTop(scrollNeeded);
        */
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

function SendReply(commentId) {
    var text = $('#replyBox').summernote('code');
    $('#replyBox').summernote('destroy');
    $('#replyDiv').remove();

    // Make sure all URLs in the reply have a target.  If not, set it to _blank.
    // We're doing this by using a fake DIV with jquery to find the links.
    var tempDiv = document.createElement('DIV');
    tempDiv.innerHTML = (text == null ? '' : text);
    var links = $(tempDiv).children('a');
    for (var x = 0; x < links.length; x++)
        if (!$(links[x]).attr('target'))
            $(links[x]).attr('target', '_blank');
    text = tempDiv.innerHTML;

    if (!_isSafariMobile)
        _srConnection.server.newReply(commentId, text);
    else {
        // Send the message in 50ms so Safari mobile won't get stuck in loading mode.
        window.setTimeout(function() { _srConnection.server.newReply(commentId, text); }, IPAD_SIGNALR_DELAY);
    }
}

function CancelReply() {
    $('#replyBox').summernote('destroy');
    $('#replyDiv').remove();
}

function AddComment() {
    var text = $('#newcomment').val();

    $('#newcomment').val('')

    if (!_isSafariMobile)
        _srConnection.server.newThread(text);
    else {
        // Send the message in 50ms so Safari mobile won't get stuck in loading mode.
        window.setTimeout(function() { _srConnection.server.newThread(text); }, IPAD_SIGNALR_DELAY);
    }
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
    if (focusedInput == null || focusedInput.length == 0) {
        if (e.keyCode == 32) { // space
            goToNextUnread();
            return false;
        } else if (e.keyCode == 82) { // 'r'
            // Find the selected comment if there is one.
            // If not, find the first comment.
            var focusedComment = $('.comment:focus');
            var comment;
            if (focusedComment != null && focusedComment.length > 0)
                comment = focusedComment[0];
            else {
                comments = $('.comment');
                if (comments != null && comments.length > 0)
                    comment = comments[0];
                else
                    return false; // No comments, do nothing.
            }

            // Reply to this comment if shift isn't pressed.
            // Indented reply if shift is pressed.
            if (e.shiftKey == false) {
                var parentId = $(comment).attr("data-parent-id");
                WriteReply(parentId);
            } else {
                var id = $(comment).attr("data-id");
                WriteReply(id);
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
    var unreadItems = $('#comments .unread');
    // Exit if there are no unread items.
    if (unreadItems == null || unreadItems.length == 0)
        return;
    // If there is only one, focus on that one and mark it read.
    if (unreadItems.length == 1) {
        focusAndMarkRead($(unreadItems[0]));
        return;
    }
    // Get the currently selected item.
    var focusedComments = $('#comments .panel:focus');
    var focusedId;
    // If we don't have a focused comment, focus and mark the first unread comment as read.
    if (focusedComments == null || focusedComments.length == 0) {
        focusAndMarkRead($(unreadItems[0]));
        return;
    } else
        focusedId = $(focusedComments[0]).attr('data-id');

    // Find the next unread item.
    var nextUnread = findNextUnreadComment(focusedId);
    // If there isn't a next one, just focus & mark the first.
    if (nextUnread == null)
        focusAndMarkRead($(unreadItems[0]));
    else
        focusAndMarkRead(nextUnread);
}

function focusAndMarkRead(unreadComment) {
    focusComment(unreadComment);
    markRead(unreadComment);
}

function focusCommentId(commentId) {
    focusComment($('#comments div[data-id="' + commentId + '"]'));
}

function focusAndMarkReadCommentId(commentId) {
    var comment = $('#comments div[data-id="' + commentId + '"]');
    focusComment(comment);
    markRead(comment);
}

function focusComment(comment) {
    $('#comments').scrollTop($('#comments').scrollTop() + ($(comment).position().top - $('#comments').offset().top));
    $(comment).focus();
}

function focusNext() {
    // Find all comments, exit if there are none.
    var comments = $('#comments .panel');
    if (comments == null || comments.length == 0)
        return;

    // If we only have one comment, focus on that one.
    if (comments.length == 1) {
        $(comments[0]).focus();
        return;
    }

    // Find the first focused comment.
    var focusedComments = $('#comments .panel:focus');
    var focusedId;
    // If we don't have a focused comment, find the first one and focus that then exit.
    if (focusedComments == null || focusedComments.length == 0) {
        $(comments[0]).focus();
        return;
    } else
        focusedId = $(focusedComments[0]).attr('data-id');

    // Find the next comment after this one and focus it.
    var nextComment = findNextComment(focusedId);
    if (nextComment != null)
        nextComment.focus();
}

function findNextComment(commentId) {
    // Find all comments, exit if there are none.
    var comments = $('#comments .panel');
    if (comments == null || comments.length == 0)
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
    var comments = $('#comments .panel');
    if (comments == null || comments.length == 0)
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
    var comments = $('#comments .panel');
    if (comments == null || comments.length == 0)
        return;

    // If we only have one comment, focus on that one.
    if (comments.length == 1) {
        $(comments[0]).focus();
        return;
    }

    // Find the first focused comment.
    var focusedComments = $('#comments .panel:focus');
    var focusedId;
    // If we don't have a focused comment, find the last one and focus that then exit.
    if (focusedComments == null || focusedComments.length == 0) {
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