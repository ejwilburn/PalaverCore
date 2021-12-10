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

using Markdig;
using Markdig.Prism;
using System;
using System.Text.RegularExpressions;
using static PalaverCore.Models.Comment;

namespace PalaverCore.Services;

public class CommentRenderService
{
    private MarkdownPipeline _pipeline;

    public CommentRenderService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UsePrism()
            .UseSoftlineBreakAsHardlineBreak()
            .Build();
    }

    /// <summary>
    /// Apply temporary filtering for comment text display, such as modifying images so they're lazy loaded.
    /// </summary>
    /// <param name="commentText"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    public string ToHtml(string commentText, TextFormat format)
    {
        if (String.IsNullOrWhiteSpace(commentText))
            return commentText;

        string output = commentText;
        switch (format)
        {
            case TextFormat.Markdown:
                output = Markdown.ToHtml(output, _pipeline);
                break;
        }

        output = Linkify(output);
        output = EnableGifPlayOnHover(output);
        output = EnableLazyLoadingImages(output);
        output = EnableTwitterEmbedding(output);
        return output;
    }

    // Regexes for modifying images to lazy load.
    private static readonly Regex GIF_IMAGE_REGEX = new Regex(@"(<img [^>]*?)(?:\s+src=)([""'][^""'>]+[""'](?<=\.gif[""']))([^>]*?>)", RegexOptions.IgnoreCase);
    private static readonly string GIF_IMAGE_REPLACE = "$1 data-gifffer=$2 class=\"animated\"$3";

    /// <summary>
    /// Modifies any img tag with a gif src to add an animated class.
    /// </summary>
    /// <param name="commentText"></param>
    /// <returns></returns>
    private string EnableGifPlayOnHover(string commentText)
    {
        return GIF_IMAGE_REGEX.Replace(commentText, GIF_IMAGE_REPLACE);
    }

    // Regexes for modifying images to lazy load.
    private static readonly Regex IMAGE_TAG_REGEX = new Regex(@"(<img [^>]*?)\s+(src=[""'][^""'>]+[""'](?<!\.gif['""]))([^>]*?>)", RegexOptions.IgnoreCase);
    private static readonly string IMAGE_TAG_LAZY_LOAD_REPLACE = "$1 data-$2 class=\"b-lazy loading\"$3";

    private string EnableLazyLoadingImages(string commentText)
    {
        return IMAGE_TAG_REGEX.Replace(commentText, IMAGE_TAG_LAZY_LOAD_REPLACE);
    }

    // Regexes for modifying images to lazy load.
    // private static readonly Regex TWITTER_URL_REGEX = new Regex(@"(<a [^>]*?href=[""'](?:https?://|//)(?:www\.)?twitter\.com[^""'>]+[""'][^>]*?>)", RegexOptions.IgnoreCase);
    // private static readonly string TWITTER_URL_REPLACEMENT = "<blockquote class=\"twitter-tweet\">$1</blockquote>";
    // private static readonly Regex TWITTER_URL_REGEX = new Regex(@"<a [^>]*?href=[""'](?:https?://|//)(?:www\.)?twitter\.com/[^""'>/]+/status/(\d+)[^""'>]*[""'][^>]*?>.*</a>", RegexOptions.IgnoreCase);
    private static readonly Regex TWITTER_URL_REGEX = new Regex(@"(<a [^>]*?href=[""'](?:https?://|//)(?:www\.)?twitter\.com[^""'>]+[""'][^>]*?>.*?</a>)", RegexOptions.IgnoreCase);
    private static readonly string TWITTER_URL_REPLACEMENT = "<blockquote class=\"twitter-tweet\" data-theme=\"dark\">$1</blockquote>";

    private string EnableTwitterEmbedding(string commentText)
    {
        return TWITTER_URL_REGEX.Replace(commentText, TWITTER_URL_REPLACEMENT);
    }

    // Find URLs within text outside of HTML tag properties.
    private static readonly Regex URL_REGEX_WITH_PROTOCOL = new Regex(@"(?<!(?:href=[""']?|src=['""]?|<a[^>]*>)[^.'""]*[\s]*)" +
        @"\b((?:https?://)(?:&amp;|[-A-Z0-9+&@#/%=~_|$?!:,.()])*[A-Z0-9+&@#/%=~_|$()])", RegexOptions.IgnoreCase);
    private static readonly Regex URL_REGEX_WITHOUT_PROTOCOL = new Regex(@"(?<!(?:href=[""']?|src=['""]?|<a[^>]*>)[^.'""]*[\s]*)" +
        @"\b((?:www\.)(?:&amp;|[-A-Z0-9+&@#/%=~_|$?!:,.()])*[A-Z0-9+&@#/%=~_|$()])", RegexOptions.IgnoreCase);
    private static readonly string URL_REPLACE_BASIC = "<a href=\"$1\" class=\"autolinked\" target=\"_blank\">$1</a>";
    private static readonly string URL_REPLACE_ADD_PROTOCOL = "<a href=\"http://$1\" class=\"autolinked\" target=\"_blank\">$1</a>";
    private static readonly Regex URL_ESCAPED_AMPERSAND = new Regex(@"(?<=href=""https?://[^/]+[^""]?)&amp;(?="" class=""autolinked"")", RegexOptions.IgnoreCase);
    private static readonly Regex TRAILING_WHITESPACE = new Regex(@"(?:&nbsp;|[ \t])+?(?=$|<br|</?p>|</?div>)", RegexOptions.IgnoreCase);

    /// <summary>
    /// Convert URLs in the text to links if they're not already a link.
    /// </summary>
    /// <param name="input">Text</param>
    /// <returns>The input string with links outside HTML tags formatted as &gt;A&lt; tags.</returns>
    private String Linkify(string input)
    {
        String output = TRAILING_WHITESPACE.Replace(input, "");
        output = URL_REGEX_WITH_PROTOCOL.Replace(output, URL_REPLACE_BASIC);
        output = URL_REGEX_WITHOUT_PROTOCOL.Replace(output, URL_REPLACE_ADD_PROTOCOL);
        output = URL_ESCAPED_AMPERSAND.Replace(output, "&");
        return output;
    }

}