using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Telegram.Messaging.Migrations
{
    public partial class m2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("insert into FieldTypes(id,name) values(2,'bool')");
            migrationBuilder.Sql("insert into FieldTypes(id,name) values(3,'DateTime')");
            migrationBuilder.Sql("insert into FieldTypes(id,name) values(6,'decimal')");
            migrationBuilder.Sql("insert into FieldTypes(id,name) values(5,'double')");
            migrationBuilder.Sql("insert into FieldTypes(id,name) values(0,'none')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("delete fieldtypes where id in (2,3,6,5,0) ");
        }
    }
}
