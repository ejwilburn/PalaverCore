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

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Stubble.Core;
using Stubble.Extensions.Loaders;

namespace Palaver.Services
{
    public class CustomHtmlHelperService
    {
        // Find URLs within text outside of HTML tag properties.
        private static readonly Regex URL_REGEX = new Regex(@"(?<!(?:href=[""']?|src=['""]?|<a[^>]*>)[^.'""]*[\s]*)\b(?:(?:https?|ftps?|file)://|www\.)[-A-Z0-9+&@#/%=~_|$?!:,.]*[A-Z0-9+&@#/%=~_|$]", RegexOptions.IgnoreCase);

        private StubbleRenderer _stubble;
        private String _siteRoot;
        private bool _cacheTemplates;

        public CustomHtmlHelperService(String siteRoot, bool cacheTemplates)
        {
            this._siteRoot = siteRoot;
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
                ResetTemplateCache();
            return _stubble.Render("thread", thread);
        }

        /// <summary>
        /// Render an HTML formatted view of a given comment based on the comment mustache partial template file in wwwroot\templates\partials.
        /// </summary>
        /// <param name="comment"></param>
        /// <returns>HTML formatted view of the comment</returns>
        public string RenderCommentFromTemplate(Palaver.Models.CommentViewModels.DetailViewModel comment)
        {
            if (!_cacheTemplates)
                ResetTemplateCache();
            return _stubble.Render("comment", comment);
        }

        /// <summary>
        /// Render an HTML formatted view of the list of threads based on the threadList mustache template file in wwwroot\templates.
        /// </summary>
        /// <param name="threads"></param>
        /// <returns>HTML formatted view of the list of threads</returns>
        public string RenderThreadListFromTemplate(IEnumerable<Palaver.Models.ThreadViewModels.ListViewModel> threads)
        {
            if (!_cacheTemplates)
                ResetTemplateCache();
            return _stubble.Render("threadList", threads);
        }

        /// <summary>
        /// Render an HTML formatted view of a thread in the thread list based on the threadListItem mustache partial template file
        /// in wwwroot\templates\partials.
        /// </summary>
        /// <param name="thread"></param>
        /// <returns>HTML formatted view of a thread for the thread list.</returns>
        public string RenderThreadListItemFromTemplate(Palaver.Models.ThreadViewModels.ListViewModel thread)
        {
            if (!_cacheTemplates)
                ResetTemplateCache();
            return _stubble.Render("threadListItem", thread);
        }

        /// <summary>
        /// Convert URLs in the text to links if they're not already a link.
        /// </summary>
        /// <param name="input">Text</param>
        /// <returns>The input string with links outside HTML tags formatted as &gt;A&lt; tags.</returns>
        public string Linkify(string input)
        {
            return URL_REGEX.Replace(input, "<a href=\"$1\" target=\"_blank\">$1</a>");
        }

        /// <summary>
        /// Returns a locally formatted date/time string based on the given DateTime show a date if the datetime
        /// is from a previous day and time only if it's from the current day.
        /// </summary>
        /// <param name="time"></param>
        /// <returns>A locally formatted date or time string.</returns>
        public string GetDisplayTime(DateTime time)
		{
			if (DateTime.Today == time.Date)
                return time.ToString("t");
			else
				return time.ToString("d");
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

        /// <summary>
        /// Clear the stubble template cache and reload it.
        /// Used in dev environments to allow for changes without restarting the server.
        /// </summary>
        private void ResetTemplateCache()
        {
            _stubble.ClearCache();
            CacheTemplates();
        }
    }
}
