﻿// <auto-generated />
using System;
using FalconBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace FalconBackend.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20250321213121_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("Analytics", b =>
                {
                    b.Property<string>("AppUserEmail")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<float>("AvgEmailsPerDay")
                        .HasColumnType("real");

                    b.Property<float>("AvgEmailsPerWeek")
                        .HasColumnType("real");

                    b.Property<float>("AvgTimeSpentDaily")
                        .HasColumnType("real");

                    b.Property<float>("AvgTimeSpentWeekly")
                        .HasColumnType("real");

                    b.Property<int>("CurrentStreak")
                        .HasColumnType("int");

                    b.Property<int>("EmailsReceivedWeekly")
                        .HasColumnType("int");

                    b.Property<int>("EmailsSentWeekly")
                        .HasColumnType("int");

                    b.Property<bool>("IsActiveToday")
                        .HasColumnType("bit");

                    b.Property<DateTime>("LastResetDate")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("datetime2");

                    b.Property<int>("LongestStreak")
                        .HasColumnType("int");

                    b.Property<int>("ReadEmailsWeekly")
                        .HasColumnType("int");

                    b.Property<int>("SpamEmailsWeekly")
                        .HasColumnType("int");

                    b.Property<float>("TimeSpentThisWeek")
                        .HasColumnType("real");

                    b.Property<float>("TimeSpentToday")
                        .HasColumnType("real");

                    b.Property<int>("TotalDaysTracked")
                        .HasColumnType("int");

                    b.Property<float>("TotalTimeSpent")
                        .HasColumnType("real");

                    b.HasKey("AppUserEmail");

                    b.ToTable("Analytics");
                });

            modelBuilder.Entity("FalconBackend.Models.AppUser", b =>
                {
                    b.Property<string>("Email")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("FullName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("HashedPassword")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("LastLogin")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("Email");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("Username")
                        .IsUnique();

                    b.ToTable("AppUsers");
                });

            modelBuilder.Entity("FalconBackend.Models.Attachments", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("FilePath")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<float>("FileSize")
                        .HasColumnType("real");

                    b.Property<string>("FileType")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<int>("MailId")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.HasKey("Id");

                    b.HasIndex("MailId");

                    b.ToTable("Attachments");
                });

            modelBuilder.Entity("FalconBackend.Models.Contact", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("AppUserEmail")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("EmailAddress")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<bool>("IsFavorite")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("LastContactDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.HasKey("Id");

                    b.HasIndex("AppUserEmail");

                    b.HasIndex("EmailAddress");

                    b.ToTable("Contacts");
                });

            modelBuilder.Entity("FalconBackend.Models.FavoriteTag", b =>
                {
                    b.Property<string>("AppUserEmail")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<int>("TagId")
                        .HasColumnType("int");

                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.HasKey("AppUserEmail", "TagId");

                    b.HasIndex("TagId", "AppUserEmail")
                        .IsUnique();

                    b.ToTable("FavoriteTags");
                });

            modelBuilder.Entity("FalconBackend.Models.Mail", b =>
                {
                    b.Property<int>("MailId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("MailId"), 1L, 1);

                    b.Property<string>("Body")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsFavorite")
                        .HasColumnType("bit");

                    b.Property<string>("MailAccountId")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Subject")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("MailId");

                    b.HasIndex("MailAccountId");

                    b.ToTable("Mails");

                    b.HasDiscriminator<string>("Discriminator").HasValue("Mail");
                });

            modelBuilder.Entity("FalconBackend.Models.MailAccount", b =>
                {
                    b.Property<string>("MailAccountId")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("AppUserEmail")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("EmailAddress")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<bool>("IsDefault")
                        .HasColumnType("bit");

                    b.Property<DateTime>("LastMailSync")
                        .HasColumnType("datetime2");

                    b.Property<int>("Provider")
                        .HasColumnType("int");

                    b.Property<string>("Token")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.HasKey("MailAccountId");

                    b.HasIndex("AppUserEmail");

                    b.HasIndex("EmailAddress")
                        .IsUnique();

                    b.ToTable("MailAccounts");
                });

            modelBuilder.Entity("FalconBackend.Models.MailTag", b =>
                {
                    b.Property<int>("MailReceivedId")
                        .HasColumnType("int");

                    b.Property<int>("TagId")
                        .HasColumnType("int");

                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.HasKey("MailReceivedId", "TagId");

                    b.HasIndex("TagId");

                    b.ToTable("MailTags");
                });

            modelBuilder.Entity("FalconBackend.Models.Recipient", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<int>("MailId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Email");

                    b.HasIndex("MailId");

                    b.ToTable("Recipient");
                });

            modelBuilder.Entity("FalconBackend.Models.Reply", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<int>("RepliedToMailId")
                        .HasColumnType("int");

                    b.Property<int>("ReplyChainPosition")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("RepliedToMailId");

                    b.ToTable("Replies");
                });

            modelBuilder.Entity("Tag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("TagName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("TagType")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Tags");

                    b.HasDiscriminator<string>("TagType").HasValue("SystemTag");
                });

            modelBuilder.Entity("FalconBackend.Models.Draft", b =>
                {
                    b.HasBaseType("FalconBackend.Models.Mail");

                    b.Property<bool>("IsSent")
                        .HasColumnType("bit");

                    b.Property<DateTime>("TimeCreated")
                        .HasColumnType("datetime2");

                    b.HasDiscriminator().HasValue("Draft");
                });

            modelBuilder.Entity("FalconBackend.Models.MailReceived", b =>
                {
                    b.HasBaseType("FalconBackend.Models.Mail");

                    b.Property<bool>("IsRead")
                        .HasColumnType("bit");

                    b.Property<string>("Sender")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<DateTime>("TimeReceived")
                        .HasColumnType("datetime2");

                    b.HasDiscriminator().HasValue("MailReceived");
                });

            modelBuilder.Entity("FalconBackend.Models.MailSent", b =>
                {
                    b.HasBaseType("FalconBackend.Models.Mail");

                    b.Property<DateTime>("TimeSent")
                        .HasColumnType("datetime2");

                    b.HasDiscriminator().HasValue("MailSent");
                });

            modelBuilder.Entity("FalconBackend.Models.UserCreatedTag", b =>
                {
                    b.HasBaseType("Tag");

                    b.Property<string>("CreatedByUserEmail")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.HasIndex("CreatedByUserEmail");

                    b.HasDiscriminator().HasValue("UserTag");
                });

            modelBuilder.Entity("Analytics", b =>
                {
                    b.HasOne("FalconBackend.Models.AppUser", "AppUser")
                        .WithOne("Analytics")
                        .HasForeignKey("Analytics", "AppUserEmail")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AppUser");
                });

            modelBuilder.Entity("FalconBackend.Models.Attachments", b =>
                {
                    b.HasOne("FalconBackend.Models.Mail", "Mail")
                        .WithMany("Attachments")
                        .HasForeignKey("MailId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Mail");
                });

            modelBuilder.Entity("FalconBackend.Models.Contact", b =>
                {
                    b.HasOne("FalconBackend.Models.AppUser", "AppUser")
                        .WithMany("Contacts")
                        .HasForeignKey("AppUserEmail")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AppUser");
                });

            modelBuilder.Entity("FalconBackend.Models.FavoriteTag", b =>
                {
                    b.HasOne("FalconBackend.Models.AppUser", "AppUser")
                        .WithMany("FavoriteTags")
                        .HasForeignKey("AppUserEmail")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.HasOne("Tag", "Tag")
                        .WithMany("FavoriteTags")
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("AppUser");

                    b.Navigation("Tag");
                });

            modelBuilder.Entity("FalconBackend.Models.Mail", b =>
                {
                    b.HasOne("FalconBackend.Models.MailAccount", "MailAccount")
                        .WithMany("Mails")
                        .HasForeignKey("MailAccountId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("MailAccount");
                });

            modelBuilder.Entity("FalconBackend.Models.MailAccount", b =>
                {
                    b.HasOne("FalconBackend.Models.AppUser", "AppUser")
                        .WithMany("MailAccounts")
                        .HasForeignKey("AppUserEmail")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AppUser");
                });

            modelBuilder.Entity("FalconBackend.Models.MailTag", b =>
                {
                    b.HasOne("FalconBackend.Models.MailReceived", "MailReceived")
                        .WithMany("MailTags")
                        .HasForeignKey("MailReceivedId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Tag", "Tag")
                        .WithMany("MailTags")
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("MailReceived");

                    b.Navigation("Tag");
                });

            modelBuilder.Entity("FalconBackend.Models.Recipient", b =>
                {
                    b.HasOne("FalconBackend.Models.Mail", "Mail")
                        .WithMany("Recipients")
                        .HasForeignKey("MailId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Mail");
                });

            modelBuilder.Entity("FalconBackend.Models.Reply", b =>
                {
                    b.HasOne("FalconBackend.Models.Mail", "Mail")
                        .WithMany()
                        .HasForeignKey("RepliedToMailId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Mail");
                });

            modelBuilder.Entity("FalconBackend.Models.UserCreatedTag", b =>
                {
                    b.HasOne("FalconBackend.Models.AppUser", "CreatedByUser")
                        .WithMany()
                        .HasForeignKey("CreatedByUserEmail")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("CreatedByUser");
                });

            modelBuilder.Entity("FalconBackend.Models.AppUser", b =>
                {
                    b.Navigation("Analytics")
                        .IsRequired();

                    b.Navigation("Contacts");

                    b.Navigation("FavoriteTags");

                    b.Navigation("MailAccounts");
                });

            modelBuilder.Entity("FalconBackend.Models.Mail", b =>
                {
                    b.Navigation("Attachments");

                    b.Navigation("Recipients");
                });

            modelBuilder.Entity("FalconBackend.Models.MailAccount", b =>
                {
                    b.Navigation("Mails");
                });

            modelBuilder.Entity("Tag", b =>
                {
                    b.Navigation("FavoriteTags");

                    b.Navigation("MailTags");
                });

            modelBuilder.Entity("FalconBackend.Models.MailReceived", b =>
                {
                    b.Navigation("MailTags");
                });
#pragma warning restore 612, 618
        }
    }
}
