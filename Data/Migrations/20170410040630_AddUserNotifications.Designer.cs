using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using PalaverCore.Data;

namespace PalaverCore.Data.Migrations
{
    [DbContext(typeof(PalaverDbContext))]
    [Migration("20170410040630_AddUserNotifications")]
    partial class AddUserNotifications
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "1.1.1");

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<string>("ClaimType")
                        .HasColumnName("claimtype");

                    b.Property<string>("ClaimValue")
                        .HasColumnName("claimvalue");

                    b.Property<int>("RoleId")
                        .HasColumnName("roleid");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("roleclaim");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<int>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<string>("ClaimType")
                        .HasColumnName("claimtype");

                    b.Property<string>("ClaimValue")
                        .HasColumnName("claimvalue");

                    b.Property<int>("UserId")
                        .HasColumnName("userid");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("userclaim");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<int>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasColumnName("loginprovider");

                    b.Property<string>("ProviderKey")
                        .HasColumnName("providerkey");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnName("providerdisplayname");

                    b.Property<int>("UserId")
                        .HasColumnName("userid");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("userlogin");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<int>", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnName("userid");

                    b.Property<int>("RoleId")
                        .HasColumnName("roleid");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("userrole");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<int>", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnName("userid");

                    b.Property<string>("LoginProvider")
                        .HasColumnName("loginprovider");

                    b.Property<string>("Name")
                        .HasColumnName("name");

                    b.Property<string>("Value")
                        .HasColumnName("value");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("usertoken");
                });

            modelBuilder.Entity("Palaver.Models.Comment", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<DateTime>("Created")
                        .HasColumnName("created");

                    b.Property<int?>("ParentCommentId")
                        .HasColumnName("parentcommentid");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnName("text");

                    b.Property<int>("ThreadId")
                        .HasColumnName("threadid");

                    b.Property<DateTime>("Updated")
                        .HasColumnName("updated");

                    b.Property<int>("UserId")
                        .HasColumnName("userid");

                    b.HasKey("Id");

                    b.HasIndex("ParentCommentId");

                    b.HasIndex("ThreadId");

                    b.HasIndex("UserId");

                    b.ToTable("comment");
                });

            modelBuilder.Entity("Palaver.Models.FavoriteComment", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnName("userid");

                    b.Property<int>("CommentId")
                        .HasColumnName("commentid");

                    b.HasKey("UserId", "CommentId");

                    b.HasIndex("CommentId");

                    b.ToTable("favoritecomment");
                });

            modelBuilder.Entity("Palaver.Models.FavoriteThread", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnName("userid");

                    b.Property<int>("ThreadId")
                        .HasColumnName("threadid");

                    b.HasKey("UserId", "ThreadId");

                    b.HasIndex("ThreadId");

                    b.ToTable("favoritethread");
                });

            modelBuilder.Entity("Palaver.Models.Role", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnName("concurrencystamp");

                    b.Property<string>("Name")
                        .HasColumnName("name")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedName")
                        .HasColumnName("normalizedname")
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasName("RoleNameIndex");

                    b.ToTable("role");
                });

            modelBuilder.Entity("Palaver.Models.Subscription", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnName("userid");

                    b.Property<int>("ThreadId")
                        .HasColumnName("threadid");

                    b.HasKey("UserId", "ThreadId");

                    b.HasIndex("ThreadId");

                    b.ToTable("subscription");
                });

            modelBuilder.Entity("Palaver.Models.Thread", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<DateTime>("Created")
                        .HasColumnName("created");

                    b.Property<bool>("IsSticky")
                        .HasColumnName("issticky");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnName("title");

                    b.Property<DateTime>("Updated")
                        .HasColumnName("updated");

                    b.Property<int>("UserId")
                        .HasColumnName("userid");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("thread");
                });

            modelBuilder.Entity("Palaver.Models.UnreadComment", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnName("userid");

                    b.Property<int>("CommentId")
                        .HasColumnName("commentid");

                    b.HasKey("UserId", "CommentId");

                    b.HasIndex("CommentId");

                    b.ToTable("unreadcomment");
                });

            modelBuilder.Entity("Palaver.Models.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnName("accessfailedcount");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnName("concurrencystamp");

                    b.Property<DateTime>("Created")
                        .HasColumnName("created");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnName("email")
                        .HasMaxLength(256);

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnName("emailconfirmed");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnName("lockoutenabled");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnName("lockoutend");

                    b.Property<string>("NormalizedEmail")
                        .HasColumnName("normalizedemail")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedUserName")
                        .HasColumnName("normalizedusername")
                        .HasMaxLength(256);

                    b.Property<bool>("NotificationEnabled")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("notificationenabled")
                        .HasDefaultValueSql("true");

                    b.Property<string>("PasswordHash")
                        .HasColumnName("passwordhash");

                    b.Property<string>("PhoneNumber")
                        .HasColumnName("phonenumber");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnName("phonenumberconfirmed");

                    b.Property<string>("SecurityStamp")
                        .HasColumnName("securitystamp");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnName("twofactorenabled");

                    b.Property<string>("UserName")
                        .HasColumnName("username")
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("Email");

                    b.HasIndex("NormalizedEmail")
                        .HasName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasName("UserNameIndex");

                    b.HasIndex("UserName");

                    b.ToTable("user");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>", b =>
                {
                    b.HasOne("Palaver.Models.Role")
                        .WithMany("Claims")
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<int>", b =>
                {
                    b.HasOne("Palaver.Models.User")
                        .WithMany("Claims")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<int>", b =>
                {
                    b.HasOne("Palaver.Models.User")
                        .WithMany("Logins")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<int>", b =>
                {
                    b.HasOne("Palaver.Models.Role")
                        .WithMany("Users")
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Palaver.Models.User")
                        .WithMany("Roles")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Palaver.Models.Comment", b =>
                {
                    b.HasOne("Palaver.Models.Comment", "Parent")
                        .WithMany("Comments")
                        .HasForeignKey("ParentCommentId");

                    b.HasOne("Palaver.Models.Thread", "Thread")
                        .WithMany("Comments")
                        .HasForeignKey("ThreadId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Palaver.Models.User", "User")
                        .WithMany("Comments")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Palaver.Models.FavoriteComment", b =>
                {
                    b.HasOne("Palaver.Models.Comment", "Comment")
                        .WithMany("FavoriteComments")
                        .HasForeignKey("CommentId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Palaver.Models.User", "User")
                        .WithMany("FavoriteComments")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Palaver.Models.FavoriteThread", b =>
                {
                    b.HasOne("Palaver.Models.Thread", "Thread")
                        .WithMany("FavoriteThreads")
                        .HasForeignKey("ThreadId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Palaver.Models.User", "User")
                        .WithMany("FavoriteThreads")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Palaver.Models.Subscription", b =>
                {
                    b.HasOne("Palaver.Models.Thread", "Thread")
                        .WithMany("Subscriptions")
                        .HasForeignKey("ThreadId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Palaver.Models.User", "User")
                        .WithMany("Subscriptions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Palaver.Models.Thread", b =>
                {
                    b.HasOne("Palaver.Models.User", "User")
                        .WithMany("Threads")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Palaver.Models.UnreadComment", b =>
                {
                    b.HasOne("Palaver.Models.Comment", "Comment")
                        .WithMany("UnreadComments")
                        .HasForeignKey("CommentId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Palaver.Models.User", "User")
                        .WithMany("UnreadComments")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
        }
    }
}
