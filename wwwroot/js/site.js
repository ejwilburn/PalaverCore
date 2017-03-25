$(document).ready(function() {
    $('.ui.search')
        .search({
            apiSettings: {
                url: BASE_URL + 'api/comment/search/{query}'
            },
            fields: {
                description: 'text'
            },
            minCharacters: 3
        });

    //$('#threadList').sidebar('show');
    /*
    $('#threadList').sidebar({
        context: $('#bodyContent')
    });
    */
    //$('#topMenu').sticky();
});