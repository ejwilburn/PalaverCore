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

const COMMENT_FORMAT = 'Markdown';

const tuiEditor = toastui.Editor;
const { codeSyntaxHighlight } = toastui.Editor.plugin;

class Editor {

    editor = null;
    editorForm = null;
    editing = null;
    editingParentId = null;
    editingCommentId = null;
    editingOrigText = null;
    editorLoaded = false;
    thread = null;
    sendingReply = false;

    constructor(thread) {
        this.thread = thread;
    }

    openEditor(openAt, commentId) {
        this.cancelReply();
        $(openAt).append(TemplateRenderer.render('editor'));
        this.editorForm = $('#editorForm');
        // this.editorLoaded = false;
        this.editor = new tuiEditor({
            el: document.querySelector('#editor'),
            height: 'auto',
            initialEditType: 'markdown',
            previewStyle: 'tab',
            usingStatistics: false,
            theme: 'dark',
            hideModeSwitch: true,
            placeholder: 'Enter comment...',
            plugins: [[codeSyntaxHighlight, { highlighter: Prism }]],
            events: {
                load: () => {
                    if (Util.isNumber(commentId)) {
                        this.thread.getCommentForEdit(commentId);
                    }
                },
                focus: (editorMode, event) => {
                    // The load event below fires before the editor's UI is fully displayed, handle the load
                    // event on focus instead.
                    // if (!this.editorLoaded) {
                        document.querySelector('#editorCancel').scrollIntoViewIfNeeded();
                        // this.editorLoaded = true;
                    // }
                    this.editor.off('focus');
                },
                keydown: (editorMode, event) => { return this.replyKeyPressed(editorMode, event); },
            },
            hooks: {
                addImageBlobHook: (blob, callback) => {
                    const uploadUrl = (BASE_URL ?? '/') + 'api/FileHandler/AutoUpload';
                    let formData = new FormData();
                    formData.append('file', blob);
                    $.ajax({
                        method: 'POST',
                        url: uploadUrl,
                        data: formData,
                        processData: false,
                        contentType: false,
                        success: (data) => {
                            callback(data.url);
                            document.querySelector('#editorCancel').scrollIntoViewIfNeeded();
                        }
                    });
                    return false
                },
            },
        });
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
            this.editor.destroy();
        }

        this.editorLoaded = false;
        this.editor = null;
        this.editing = null;
        this.editingParentId = null;
        this.editingCommentId = null;
        this.editingOrigText = null;
        this.sendingReply = false;
        $(this.editorForm)?.remove();
        this.editorForm = null;
    }

    replyTo(parentId) {
        if (!Util.isNumber(parentId)) {
            // They're replying to the thread or a direct reply to a top level comment.
            this.openEditor(this.thread.$thread.children('.comments'));
            return;
        }
        this.editingParentId = parentId;
        this.openEditor(this.thread.$thread.find(`.comment[data-id="${parentId}"]>.comments`));
    }

    setComment(comment) {
        if (comment?.Text) {
            this.editor?.setMarkdown(comment.Text);
        } else {
            console.warn('Comment has no text to pass to the editor.');
        }
    }

    editComment(commentId) {
        this.editingCommentId = commentId;
        this.editing = this.thread.$thread.find(`.comment[data-id="${commentId}"]>.content>.text`);
        this.editingOrigText = this.editing.html();
        this.editing.empty();
        this.openEditor(this.editing, commentId);
    }

    replyKeyPressed(editorMode, event) {
        // Save if shift+enter is pressed.
        if (event.keyCode == 13 && event.shiftKey) {
            event.preventDefault();
            this.sendReply();
            return false;
        } else if (event.keyCode == 27) {
            event.preventDefault();
            this.cancelReply();
            return false;
        }
        return true;
    }

    sendReply() {
        // Don't post if in the middle of posting, prevents double/triple posting by hitting shift+enter quickly and repeatedly
        // Gets cleared every time the editor is closed.
        if (this.sendingReply) {
            return;
        }
        this.sendingReply = true;

        let text = this.editor.getMarkdown();
        if (Util.isNullOrEmpty(text) || text === '<p><br></p>') {
            alert("Replies cannot be empty.");
            return;
        }

        let parentCommentId = this.editingParentId;

        this.thread.showBusy();
        
        // Make sure all URLs in the reply have a target.  If not, set it to _blank.
        // We're doing this by using a fake DIV with jquery to find the links.
        let tempDiv = document.createElement('DIV');
        tempDiv.innerHTML = text;
        let links = $(tempDiv).find('a').each(function (index) {
            if (!$(this).attr('target'))
                $(this).attr('target', '_blank');
        });

        if (!this.editing) {
            this.thread.newComment({
                "Text": tempDiv.innerHTML,
                "Format": COMMENT_FORMAT,
                "ThreadId": this.thread.threadId,
                "ParentCommentId": (!Util.isNumber(parentCommentId) ? null : parentCommentId)
            });
        } else {
            this.thread.saveUpdatedComment({
                "Id": this.editingCommentId,
                "Text": tempDiv.innerHTML,
                "Format": COMMENT_FORMAT
            });
        }
    }

    isEditorInUse() {
        if (this.editor || $('input:focus').length > 0 || $('textarea:focus').length > 0)
            return true;
        return false;
    }

}