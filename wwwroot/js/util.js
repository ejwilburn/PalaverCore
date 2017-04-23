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

const MAX_MOBILE_WIDTH = 767; // in pixels

class Util {
    static isMobileDisplay() {
        return window.innerWidth <= MAX_MOBILE_WIDTH;
    }

    // For all scripts in asyncScripts load them via AJAX then execute their callbacks.
    // This is done for faster page loading for non-critical scripts.
    static loadScriptsAsync(asyncScripts) {
        for (var script of asyncScripts) {
            this.loadScriptAsync(script.url, script.callback);
        }
    }

    static loadScriptAsync(url, callback) {
        if (callback)
            $.getScript(url, (data, textStatus, jqxhr) => { return callback(data, textStatus, jqxhr); });
        else
            $.getScript(url);
    }

    static isNumber(val) {
        return typeof val === 'number';
    }

    static isString(val) {
        return typeof val === 'string';
    }

    static isNull(val) {
        return typeof val === 'undefined' || val === null;
    }

    static isNullOrEmpty(val) {
        if (Util.isNull(val) || (Util.isString(val) && val.trim().length === 0) || (Array.isArray(val) && val.length === 0))
            return true;

        return false;
    }

    static loadTemplates(saveTo) {
        if (Util.isNull(saveTo.templates))
            saveTo.templates = {};
        $('script[type=x-mustache-template]').each((index, item) => {
            let name = item.attributes.name.value;
            $.get(item.src, (data) => { saveTo.templates[name] = data; });
        });
    }
}