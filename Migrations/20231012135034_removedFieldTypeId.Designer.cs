﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Telegram.Messaging.Db;

#nullable disable

namespace Telegram.Messaging.Migrations
{
    [DbContext(typeof(MessagingDb))]
    [Migration("20231012135034_removedFieldTypeId")]
    partial class removedFieldTypeId
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("Telegram.Messaging.Db.DbFieldType", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasAlternateKey("Name");

                    b.ToTable("FieldTypes");
                });

            modelBuilder.Entity("Telegram.Messaging.Db.Question", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("Answers")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CallbackHandlerAssemblyName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Constraints")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedUtc")
                        .HasColumnType("datetime2");

                    b.Property<string>("DefaultAnswers")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool?>("DisableWebPagePreview")
                        .HasColumnType("bit");

                    b.Property<bool>("ExpectsCommand")
                        .HasColumnType("bit");

                    b.Property<int>("FieldTypeId")
                        .HasColumnType("int");

                    b.Property<string>("FollowUp")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FollowUpSeparator")
                        .HasMaxLength(10)
                        .HasColumnType("nvarchar(10)");

                    b.Property<string>("ImageUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("InternalId")
                        .HasColumnType("int");

                    b.Property<bool>("IsCompleted")
                        .HasColumnType("bit");

                    b.Property<bool>("IsMandatory")
                        .HasColumnType("bit");

                    b.Property<int>("MaxButtonsPerRow")
                        .HasColumnType("int");

                    b.Property<string>("MethodNameOnEvent")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("PickOnlyDefaultAnswers")
                        .HasColumnType("bit");

                    b.Property<string>("QuestionText")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("SurveyId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("SurveyId");

                    b.ToTable("Questions");
                });

            modelBuilder.Entity("Telegram.Messaging.Db.Survey", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<DateTime>("CreatedUtc")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<bool>("IsCancelled")
                        .HasColumnType("bit");

                    b.Property<bool>("IsCompleted")
                        .HasColumnType("bit");

                    b.Property<DateTime>("LastInteractionUtc")
                        .HasColumnType("datetime2");

                    b.Property<int?>("TelegramMessageId")
                        .HasColumnType("int");

                    b.Property<long>("TelegramUserId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.ToTable("Surveys");
                });

            modelBuilder.Entity("Telegram.Messaging.Db.Question", b =>
                {
                    b.HasOne("Telegram.Messaging.Db.Survey", "Survey")
                        .WithMany("Questions")
                        .HasForeignKey("SurveyId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Survey");
                });

            modelBuilder.Entity("Telegram.Messaging.Db.Survey", b =>
                {
                    b.Navigation("Questions");
                });
#pragma warning restore 612, 618
        }
    }
}
