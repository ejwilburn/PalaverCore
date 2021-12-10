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

// Mustache rendering templates.
var templates = {};

class TemplateRenderer {
    static staticConstructor() {
        TemplateRenderer.loadTemplates();
    }

    static render(templateName, data = null) {
        return Mustache.render(templates[templateName], data);
    }

    static loadTemplates() {
        $('script[data-istemplate="true"]').each((index, item) => {
            let name = item.attributes.name.value;
            $.get(item.src, (data) => { templates[name] = data; });
        });
    }
}

TemplateRenderer.staticConstructor();