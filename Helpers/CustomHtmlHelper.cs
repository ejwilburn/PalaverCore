/*
Copyright 2012, Marcus McKinnon, E.J. Wilburn, Kevin Williams
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
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Generic;
using Palaver.Models;
using Microsoft.Extensions.Configuration;

namespace Palaver.Helpers
{
    public class CustomHtmlHelper
    {
        // Find URLs within text outside of HTML tag properties.
        // Previous version: @"(?<!(?:href=[""']?|src=['""]?|<a[^>]*>)[^.'""]*[\s]*)\b(?:(?:https?|ftps?|ftpes|file)://)[-A-Z0-9+&;@#/%=~_|$?!:,.]*[A-Z0-9+&;@#/%=~_|$]"
        private static Regex urlRegex1 = new Regex(@"(?<!(?:href=[""']?|src=['""]?|<a[^>]*>)[^.'""]*[\s]*)\b(?:(?:https?|ftps?|ftpes|file)://)[^\s'""]*[^\s'""!?.,]", RegexOptions.IgnoreCase);
        // Find links without the protocol specified.
        // Previous version: @"(?<!(?:http://|ftp://|href=[""']?|src=[""']?|<a[^>]*>)[^.'""]*[\s]*)\b(?:www\.|ftp\.)[-A-Z0-9+&;@#/%=~_|$?!:,.]*[A-Z0-9+&;@#/%=~_|$]"
        private static Regex urlRegex2 = new Regex(@"(?<!(?:http://|ftp://|href=[""']?|src=[""']?|<a[^>]*>)[^.'""]*[\s]*)\b(?:www\.|ftp\.)[^\s'""]*[^\s'""!?.,]", RegexOptions.IgnoreCase);

        public static string Linkify(string input)
        {
            String output = input;
                //"<a target='_blank' href='$1'>$1</a>");
            int beginningSize = output.Length;
            
            // Convert URLs in the text to links if they're not already a link.
            // First linkify URLs with the protocol already there.
            MatchCollection matches = urlRegex1.Matches(output);
            foreach (Match match in matches)
            {
                int sizeDif = output.Length - beginningSize;
                String cleanedMatchValue = WebUtility.HtmlDecode(match.Groups[0].Value).Trim();
                output = output.Substring(0, match.Groups[0].Index + sizeDif) +
                    "<a href=\"" + cleanedMatchValue + "\" target=\"_blank\">" + cleanedMatchValue + "</a>" +
                    output.Substring(match.Groups[0].Index + match.Groups[0].Length + sizeDif);
            }

            // Second, linkify URLs without the protocol specified, assume http.
            beginningSize = output.Length;
            matches = urlRegex2.Matches(output);
            foreach (Match match in matches)
            {
                int sizeDif = output.Length - beginningSize;
                String cleanedMatchValue = WebUtility.HtmlDecode(match.Groups[0].Value).Trim();
                output = output.Substring(0, match.Groups[0].Index + sizeDif) +
                    "<a href=\"http://" + cleanedMatchValue + "\" target=\"_blank\">" + cleanedMatchValue + "</a>" +
                    output.Substring(match.Groups[0].Index + match.Groups[0].Length + sizeDif);
            }
            return output;
        }

        public static string GetDisplayTime(DateTime time)
		{
			if (DateTime.Today == time.Date)
                return time.ToString("t");
			else
				return time.ToString("d");
		}
    }
}