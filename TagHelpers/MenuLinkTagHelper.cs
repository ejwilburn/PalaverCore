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

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace PalaverCore.TagHelpers;

/// <summary>
/// <see cref="ITagHelper"/> implementation targeting &lt;menulink&gt; elements that assist with rendering contextually aware menu links.
/// If the current route is matched the given &lt;menulink&gt; will be active. This was added to demonstrate how a TagHelper might be used
/// with Semantic UI to implement a simple menu.
/// </summary>
[HtmlTargetElement("menulink", Attributes = "controller-name, action-name, menu-text")]
public class MenuLinkTagHelper : TagHelper
{
    public string ControllerName { get; set; }
    public string ActionName { get; set; }
    public string MenuText { get; set; }

    [ViewContext]
    public ViewContext ViewContext { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var urlHelper = new UrlHelper(ViewContext);

        string menuUrl = urlHelper.Action(ActionName, ControllerName);

        output.TagName = "a";
        output.Attributes.Add("href", $"{menuUrl}");
        output.Attributes.Add("class", "item blue");
        output.Content.SetContent(MenuText);

        var routeData = ViewContext.RouteData.Values;
        var currentController = routeData["controller"];
        var currentAction = routeData["action"];

        if (String.Equals(ActionName, currentAction as string, StringComparison.OrdinalIgnoreCase)
            && String.Equals(ControllerName, currentController as string, StringComparison.OrdinalIgnoreCase))
        {
            output.Attributes.SetAttribute("class", "active item blue");
        }

    }
}