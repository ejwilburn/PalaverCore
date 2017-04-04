/**
 * @license Copyright (c) 2003-2017, CKSource - Frederico Knabben. All rights reserved.
 * For licensing, see LICENSE.md or http://ckeditor.com/license
 */

CKEDITOR.editorConfig = function(config) {
    // Define changes to default configuration here.
    // For complete reference see:
    // http://docs.ckeditor.com/#!/api/CKEDITOR.config

    // The toolbar groups arrangement, optimized for a single toolbar row.
    /*
    config.toolbarGroups = [
        { name: 'forms' },
        { name: 'basicstyles', groups: ['basicstyles', 'cleanup'] },
        { name: 'paragraph', groups: ['list', 'indent', 'blocks', 'align'] },
        { name: 'styles' },
        { name: 'insert', groups: ['link', 'image', 'media', 'codesnippet'] },
        { name: 'colors' },
        { name: 'tools' },
        { name: 'others' },
        { name: 'editing', groups: ['paste', 'find', 'selection', 'spellchecker'] }
    ];
	*/

    config.toolbar = [
        { name: 'insert', items: ['Link', 'Unlink', 'Image', 'Embed', 'CodeSnippet'] },
        { name: 'styles', items: ['FontSize', 'TextColor'] },
        { name: 'basicstyles', items: ['Bold', 'Italic', 'Underline', 'Strike', '-', 'RemoveFormat'] },
        { name: 'paragraph', items: ['NumberedList', 'BulletedList', '-', 'Outdent', 'Indent'] },
        { name: 'paste', items: ['Paste', 'PasteText'] },
        { name: 'about', items: ['a11yhelp'] }
    ];
    config.skin = 'moono-dark';
    config.disableNativeSpellChecker = false;
    config.startupFocus = true;
    config.toolbarCanCollapse = true;
    //config.extraPlugins = 'autogrow,find,image,autolink,codesnippet,embedbase,embed';
    //config.removePlugins = 'tab,elementspath,bidi';
    config.autoGrow_onStartup = false;
    config.autoGrow_minHeight = 100;
    config.resize_enabled = false;
    config.enableTabKeyTools = false;
    config.defaultLanguage = 'en';
    config.height = 100;
    config.codeSnippet_theme = 'onedark';
    config.enterMode = CKEDITOR.ENTER_BR;
    config.keystrokes = [
        [CKEDITOR.SHIFT + 13, null],
    ];
    config.uploadUrl = BASE_URL + 'api/FileHandler/AutoUpload';
    // The default plugins included in the basic setup define some buttons that
    // are not needed in a basic editor. They are removed here.
    config.removeButtons = 'Cut,Copy,Paste,Undo,Redo,Anchor,Subscript,Superscript';

    // Dialog windows are also simplified.
    //config.removeDialogTabs = 'link:advanced';
};