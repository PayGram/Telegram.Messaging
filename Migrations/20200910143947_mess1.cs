using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Telegram.Messaging.Migrations
{
    public partial class mess1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CallbackHandlerAssemblyName",
                table: "Questions",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            
            migrationBuilder.DropColumn(
                name: "CallbackHandlerAssemblyName",
                table: "Questions");

        }
    }
}
