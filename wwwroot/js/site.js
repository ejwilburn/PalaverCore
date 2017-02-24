var themes = {
    "material": "lib/bootstrap/dist/css/bootstrap.css",
    "bootstrap default": "//netdna.bootstrapcdn.com/bootstrap/3.3.5/css/bootstrap.css",
    "cerulean": "//bootswatch.com/cerulean/bootstrap.css",
    "cosmo": "//bootswatch.com/cosmo/bootstrap.css",
    "cyborg": "//bootswatch.com/cyborg/bootstrap.css",
    "darkly": "//bootswatch.com/darkly/bootstrap.css",
    "flatly": "//bootswatch.com/flatly/bootstrap.css",
    "lumen": "//bootswatch.com/lumen/bootstrap.css",
    "paper": "//bootswatch.com/paper/bootstrap.css",
    "journal": "//bootswatch.com/journal/bootstrap.css",
    "readable": "//bootswatch.com/readable/bootstrap.css",
    "sandstone": "//bootswatch.com/sandstone/bootstrap.css",
    "simplex": "//bootswatch.com/simplex/bootstrap.css",
    "slate": "//bootswatch.com/slate/bootstrap.css",
    "spacelab": "//bootswatch.com/spacelab/bootstrap.css",
    "superhero": "//bootswatch.com/superhero/bootstrap.css",
    "united": "//bootswatch.com/united/bootstrap.css",
    "yeti": "//bootswatch.com/yeti/bootstrap.css",
}

$(document).ready(function() {
    $.material.init();

    $('[data-toggle=offcanvas]').click(function() {
        $('.row-offcanvas').toggleClass('active');
    });

    // Initialize the theme switcher.
    var $themesheet = $('<link href="' + themes['material'] + '" rel="stylesheet" />')
    $themesheet.appendTo('head');
    var options = $.map(themes, function(url, theme) {
        return '<li class="theme-option" data-theme="' + theme + '"><a href="#">' + theme + '</a></li>';
    }).join('');
    $('#theme-selector ul').html(options);
    $('#theme-selector .theme-option').on('click', function() {
        $themesheet.attr('href', themes[$(this).data('theme')]);
    });
});