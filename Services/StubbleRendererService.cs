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

using System;
using System.Collections.Generic;
using Stubble.Core;
using Stubble.Extensions.Loaders;

namespace Palaver.Services
{
    public class StubbleRendererService
    {
        private StubbleRenderer _stubble;
        private bool _cacheTemplates;

        public StubbleRendererService(bool cacheTemplates)
        {
            this._cacheTemplates = cacheTemplates;
            LoadTemplates();
        }

        /// <summary>
        /// Render an HTML formatted view of a given thread based on the thread mustache template file in wwwroot\templates.
        /// </summary>
        /// <param name="thread"></param>
        /// <returns>HTML formatted view of the thread</returns>
        public string RenderThreadFromTemplate(Palaver.Models.ThreadViewModels.SelectedViewModel thread)
        {
            if (!_cacheTemplates)
                LoadTemplates();
            return _stubble.Render("thread", thread);
        }

        /// <summary>
        /// Render an HTML formatted view of the list of threads based on the threadList mustache template file in wwwroot\templates.
        /// </summary>
        /// <param name="threads"></param>
        /// <returns>HTML formatted view of the list of threads</returns>
        public string RenderThreadListFromTemplate(IEnumerable<Palaver.Models.ThreadViewModels.ListViewModel> threads)
        {
            if (!_cacheTemplates)
                LoadTemplates();
            return _stubble.Render("threadList", threads);
        }

        /// <summary>
        /// Load mustache templates for rendering content with Stubble, pre-compiling and caching them for speed.
        /// </summary>
        private void LoadTemplates()
        {
            _stubble = new StubbleBuilder()
                .SetPartialTemplateLoader(new FileSystemLoader("./wwwroot/templates/partials/"))
                .SetTemplateLoader(new FileSystemLoader("./wwwroot/templates/"))
                .SetMaxRecursionDepth(5000)
                .Build();
            CacheTemplates();
        }

        /// <summary>
        /// Load mustache templates, compile and cache them for faster rendering.
        /// Partials can't be cached currently.
        /// </summary>
        private void CacheTemplates()
        {
            _stubble.CacheTemplate("thread");
            _stubble.CacheTemplate("threadList");
        }
    }
}
