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
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PalaverCore.Data.Migrations;

public partial class AddTimezoneToDbDateFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<DateTime>(
            name: "created",
            table: "user",
            type: "timestamptz",
            nullable: false,
            oldClrType: typeof(DateTime));

        migrationBuilder.AlterColumn<DateTime>(
            name: "updated",
            table: "thread",
            type: "timestamptz",
            nullable: false,
            oldClrType: typeof(DateTime));

        migrationBuilder.AlterColumn<DateTime>(
            name: "created",
            table: "thread",
            type: "timestamptz",
            nullable: false,
            oldClrType: typeof(DateTime));

        migrationBuilder.AlterColumn<DateTime>(
            name: "updated",
            table: "comment",
            type: "timestamptz",
            nullable: false,
            oldClrType: typeof(DateTime));

        migrationBuilder.AlterColumn<DateTime>(
            name: "created",
            table: "comment",
            type: "timestamptz",
            nullable: false,
            oldClrType: typeof(DateTime));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<DateTime>(
            name: "created",
            table: "user",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamptz");

        migrationBuilder.AlterColumn<DateTime>(
            name: "updated",
            table: "thread",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamptz");

        migrationBuilder.AlterColumn<DateTime>(
            name: "created",
            table: "thread",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamptz");

        migrationBuilder.AlterColumn<DateTime>(
            name: "updated",
            table: "comment",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamptz");

        migrationBuilder.AlterColumn<DateTime>(
            name: "created",
            table: "comment",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamptz");
    }
}