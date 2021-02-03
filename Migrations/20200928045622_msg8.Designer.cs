﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Telegram.Messaging.Db;

namespace Telegram.Messaging.Migrations
{
    [DbContext(typeof(MessagingDb))]
    [Migration("20200928045622_msg8")]
    partial class msg8
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Telegram.Messaging.Db.FieldType", b =>
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
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

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

                    b.Property<bool>("ExpectsCommand")
                        .HasColumnType("bit");

                    b.Property<int>("FieldTypeId")
                        .HasColumnType("int");

                    b.Property<string>("FollowUp")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FollowUpSeparator")
                        .HasColumnType("nvarchar(10)")
                        .HasMaxLength(10);

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
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("SurveyId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("FieldTypeId");

                    b.HasIndex("SurveyId");

                    b.ToTable("Questions");
                });

            modelBuilder.Entity("Telegram.Messaging.Db.Survey", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

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

                    b.Property<long?>("TelegramMessageId")
                        .HasColumnType("bigint");

                    b.Property<long>("TelegramUserId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.ToTable("Surveys");
                });

            modelBuilder.Entity("Telegram.Messaging.Db.Question", b =>
                {
                    b.HasOne("Telegram.Messaging.Db.FieldType", "FieldType")
                        .WithMany()
                        .HasForeignKey("FieldTypeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Telegram.Messaging.Db.Survey", "Survey")
                        .WithMany("Questions")
                        .HasForeignKey("SurveyId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
