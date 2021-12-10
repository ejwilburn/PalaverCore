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

namespace PalaverCore.Services;

public class SmtpOptions
{
    public static readonly string CONFIG_SECTION_NAME = "Smtp";

    public string Server { get; set; } = "localhost";
    public int Port { get; set; } = 25;
    public bool RequireTls { get; set; } = false;
    public string Username { get; set; } = null;
    public string Password { get; set; } = null;
    public string FromName { get; set; } = "Palaver";
    public string FromAddress { get; set; } = "noreply@noreply.com";

    public SmtpOptions()
    {
    }
}
