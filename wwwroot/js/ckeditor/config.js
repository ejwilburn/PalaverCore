/**
 * @license Copyright (c) 2003-2017, CKSource - Frederico Knabben. All rights reserved.
 * For licensing, see LICENSE.md or http://ckeditor.com/license
 */

CKEDITOR.editorConfig = function(config) {
    // Define changes to default configuration here.
    // For complete reference see:
    // http://docs.ckeditor.com/#!/api/CKEDITOR.config
    config.toolbar = [
        { name: 'insert', items: ['Link', 'Unlink', 'Image', 'Youtube', 'CodeSnippet'] },
        { name: 'styles', items: ['FontSize', 'TextColor'] },
        { name: 'basicstyles', items: ['Bold', 'Italic', 'Underline', 'Strike', '-', 'RemoveFormat'] },
        { name: 'paragraph', items: ['NumberedList', 'BulletedList', '-', 'Outdent', 'Indent'] },
        { name: 'paste', items: ['PasteText'] },
        { name: 'about', items: ['a11yhelp'] }
    ];
    config.skin = 'moono-dark';
    config.disableNativeSpellChecker = false;
    config.startupFocus = true;
    config.toolbarCanCollapse = true;
    config.extraPlugins = 'codesnippet,image,image2,link,prism,youtube';
    config.removePlugins = 'about';
    config.autoGrow_onStartup = true;
    config.autoGrow_minHeight = 100;
    config.height = 100;
    config.htmlEncodeOutput = false;
    config.youtube_privacy = true;
    config.youtube_autoplay = false;
    config.youtube_controls = true;
    config.youtube_disabled_fields = ['chkAutoplay'];
    config.resize_enabled = false;
    config.enableTabKeyTools = false;
    config.defaultLanguage = 'en';
    config.codeSnippet_theme = 'onedark';
    config.enterMode = CKEDITOR.ENTER_BR;
    config.keystrokes = [
        [CKEDITOR.SHIFT + 13, null],
    ];
    if (typeof BASE_URL !== 'undefined')
        config.uploadUrl = BASE_URL + 'api/FileHandler/AutoUpload';
    else
        config.uploadUrl = '/api/FileHandler/AutoUpload';
    // The default plugins included in the basic setup define some buttons that
    // are not needed in a basic editor. They are removed here.
    config.removeButtons = 'Cut,Copy,Paste,Undo,Redo,Anchor,Subscript,Superscript';
    // Simplify the dialog windows.
    config.removeDialogTabs = 'image:advanced;link:advanced';
};