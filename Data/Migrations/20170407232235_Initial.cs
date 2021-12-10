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
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace PalaverCore.Data.Migrations;

public partial class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "usertoken",
            columns: table => new
            {
                userid = table.Column<int>(nullable: false),
                loginprovider = table.Column<string>(nullable: false),
                name = table.Column<string>(nullable: false),
                value = table.Column<string>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_usertoken", x => new { x.userid, x.loginprovider, x.name });
            });

        migrationBuilder.CreateTable(
            name: "role",
            columns: table => new
            {
                id = table.Column<int>(nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                concurrencystamp = table.Column<string>(nullable: true),
                name = table.Column<string>(maxLength: 256, nullable: true),
                normalizedname = table.Column<string>(maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_role", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "user",
            columns: table => new
            {
                id = table.Column<int>(nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                accessfailedcount = table.Column<int>(nullable: false),
                concurrencystamp = table.Column<string>(nullable: true),
                created = table.Column<DateTime>(nullable: false),
                email = table.Column<string>(maxLength: 256, nullable: false),
                emailconfirmed = table.Column<bool>(nullable: false),
                lockoutenabled = table.Column<bool>(nullable: false),
                lockoutend = table.Column<DateTimeOffset>(nullable: true),
                normalizedemail = table.Column<string>(maxLength: 256, nullable: true),
                normalizedusername = table.Column<string>(maxLength: 256, nullable: true),
                passwordhash = table.Column<string>(nullable: true),
                phonenumber = table.Column<string>(nullable: true),
                phonenumberconfirmed = table.Column<bool>(nullable: false),
                securitystamp = table.Column<string>(nullable: true),
                twofactorenabled = table.Column<bool>(nullable: false),
                username = table.Column<string>(maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "roleclaim",
            columns: table => new
            {
                id = table.Column<int>(nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                claimtype = table.Column<string>(nullable: true),
                claimvalue = table.Column<string>(nullable: true),
                roleid = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_roleclaim", x => x.id);
                table.ForeignKey(
                    name: "FK_roleclaim_role_roleid",
                    column: x => x.roleid,
                    principalTable: "role",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "userclaim",
            columns: table => new
            {
                id = table.Column<int>(nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                claimtype = table.Column<string>(nullable: true),
                claimvalue = table.Column<string>(nullable: true),
                userid = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_userclaim", x => x.id);
                table.ForeignKey(
                    name: "FK_userclaim_user_userid",
                    column: x => x.userid,
                    principalTable: "user",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "userlogin",
            columns: table => new
            {
                loginprovider = table.Column<string>(nullable: false),
                providerkey = table.Column<string>(nullable: false),
                providerdisplayname = table.Column<string>(nullable: true),
                userid = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_userlogin", x => new { x.loginprovider, x.providerkey });
                table.ForeignKey(
                    name: "FK_userlogin_user_userid",
                    column: x => x.userid,
                    principalTable: "user",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "userrole",
            columns: table => new
            {
                userid = table.Column<int>(nullable: false),
                roleid = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_userrole", x => new { x.userid, x.roleid });
                table.ForeignKey(
                    name: "FK_userrole_role_roleid",
                    column: x => x.roleid,
                    principalTable: "role",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_userrole_user_userid",
                    column: x => x.userid,
                    principalTable: "user",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "thread",
            columns: table => new
            {
                id = table.Column<int>(nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                created = table.Column<DateTime>(nullable: false),
                issticky = table.Column<bool>(nullable: false),
                title = table.Column<string>(nullable: false),
                updated = table.Column<DateTime>(nullable: false),
                userid = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_thread", x => x.id);
                table.ForeignKey(
                    name: "FK_thread_user_userid",
                    column: x => x.userid,
                    principalTable: "user",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "comment",
            columns: table => new
            {
                id = table.Column<int>(nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                created = table.Column<DateTime>(nullable: false),
                parentcommentid = table.Column<int>(nullable: true),
                text = table.Column<string>(nullable: false),
                threadid = table.Column<int>(nullable: false),
                updated = table.Column<DateTime>(nullable: false),
                userid = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_comment", x => x.id);
                table.ForeignKey(
                    name: "FK_comment_comment_parentcommentid",
                    column: x => x.parentcommentid,
                    principalTable: "comment",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_comment_thread_threadid",
                    column: x => x.threadid,
                    principalTable: "thread",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_comment_user_userid",
                    column: x => x.userid,
                    principalTable: "user",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "favoritethread",
            columns: table => new
            {
                userid = table.Column<int>(nullable: false),
                threadid = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_favoritethread", x => new { x.userid, x.threadid });
                table.ForeignKey(
                    name: "FK_favoritethread_thread_threadid",
                    column: x => x.threadid,
                    principalTable: "thread",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_favoritethread_user_userid",
                    column: x => x.userid,
                    principalTable: "user",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "subscription",
            columns: table => new
            {
                userid = table.Column<int>(nullable: false),
                threadid = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_subscription", x => new { x.userid, x.threadid });
                table.ForeignKey(
                    name: "FK_subscription_thread_threadid",
                    column: x => x.threadid,
                    principalTable: "thread",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_subscription_user_userid",
                    column: x => x.userid,
                    principalTable: "user",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "favoritecomment",
            columns: table => new
            {
                userid = table.Column<int>(nullable: false),
                commentid = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_favoritecomment", x => new { x.userid, x.commentid });
                table.ForeignKey(
                    name: "FK_favoritecomment_comment_commentid",
                    column: x => x.commentid,
                    principalTable: "comment",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_favoritecomment_user_userid",
                    column: x => x.userid,
                    principalTable: "user",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "unreadcomment",
            columns: table => new
            {
                userid = table.Column<int>(nullable: false),
                commentid = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_unreadcomment", x => new { x.userid, x.commentid });
                table.ForeignKey(
                    name: "FK_unreadcomment_comment_commentid",
                    column: x => x.commentid,
                    principalTable: "comment",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_unreadcomment_user_userid",
                    column: x => x.userid,
                    principalTable: "user",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_roleclaim_roleid",
            table: "roleclaim",
            column: "roleid");

        migrationBuilder.CreateIndex(
            name: "IX_userclaim_userid",
            table: "userclaim",
            column: "userid");

        migrationBuilder.CreateIndex(
            name: "IX_userlogin_userid",
            table: "userlogin",
            column: "userid");

        migrationBuilder.CreateIndex(
            name: "IX_userrole_roleid",
            table: "userrole",
            column: "roleid");

        migrationBuilder.CreateIndex(
            name: "IX_comment_parentcommentid",
            table: "comment",
            column: "parentcommentid");

        migrationBuilder.CreateIndex(
            name: "IX_comment_threadid",
            table: "comment",
            column: "threadid");

        migrationBuilder.CreateIndex(
            name: "IX_comment_userid",
            table: "comment",
            column: "userid");

        migrationBuilder.CreateIndex(
            name: "IX_favoritecomment_commentid",
            table: "favoritecomment",
            column: "commentid");

        migrationBuilder.CreateIndex(
            name: "IX_favoritethread_threadid",
            table: "favoritethread",
            column: "threadid");

        migrationBuilder.CreateIndex(
            name: "RoleNameIndex",
            table: "role",
            column: "normalizedname",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_subscription_threadid",
            table: "subscription",
            column: "threadid");

        migrationBuilder.CreateIndex(
            name: "IX_thread_userid",
            table: "thread",
            column: "userid");

        migrationBuilder.CreateIndex(
            name: "IX_unreadcomment_commentid",
            table: "unreadcomment",
            column: "commentid");

        migrationBuilder.CreateIndex(
            name: "IX_user_email",
            table: "user",
            column: "email");

        migrationBuilder.CreateIndex(
            name: "EmailIndex",
            table: "user",
            column: "normalizedemail");

        migrationBuilder.CreateIndex(
            name: "UserNameIndex",
            table: "user",
            column: "normalizedusername",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_user_username",
            table: "user",
            column: "username");

        migrationBuilder.Sql("CREATE INDEX \"IX_comment_text_fulltextsearch\" ON comment USING gist(to_tsvector('english', text));");
        migrationBuilder.Sql(@"CREATE FUNCTION search_comments(p_find text)
            RETURNS SETOF comment AS $func$
                SELECT * FROM comment WHERE to_tsvector('english', text) @@ to_tsquery('english', p_find)
                ORDER BY ts_rank_cd(to_tsvector('english', text), to_tsquery('english', p_find)) DESC, created DESC;
            $func$ LANGUAGE sql;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "roleclaim");

        migrationBuilder.DropTable(
            name: "userclaim");

        migrationBuilder.DropTable(
            name: "userlogin");

        migrationBuilder.DropTable(
            name: "userrole");

        migrationBuilder.DropTable(
            name: "usertoken");

        migrationBuilder.DropTable(
            name: "favoritecomment");

        migrationBuilder.DropTable(
            name: "favoritethread");

        migrationBuilder.DropTable(
            name: "subscription");

        migrationBuilder.DropTable(
            name: "unreadcomment");

        migrationBuilder.DropTable(
            name: "role");

        migrationBuilder.DropTable(
            name: "comment");

        migrationBuilder.DropTable(
            name: "thread");

        migrationBuilder.DropTable(
            name: "user");
    }
}