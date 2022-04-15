using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Telegram.Messaging.Migrations
{
    public partial class m4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("delete fieldtypes where id in (2,3,6,5,0) ");
            migrationBuilder.Sql("insert into FieldTypes(id,name) values(0,'None')");
            migrationBuilder.Sql("insert into FieldTypes(id,name) values(1,'String')");
            migrationBuilder.Sql("insert into FieldTypes(id,name) values(2,'Int')");
            migrationBuilder.Sql("insert into FieldTypes(id,name) values(3,'Bool')");
            migrationBuilder.Sql("insert into FieldTypes(id,name) values(4,'DateTime')");
            migrationBuilder.Sql("insert into FieldTypes(id,name) values(5,'Double')");
            migrationBuilder.Sql("insert into FieldTypes(id,name) values(6,'Decimal')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("delete fieldtypes where id in (0,1,2,3,4,5,6) ");
            migrationBuilder.Sql("insert into FieldTypes(id,name) values(2,'bool')");
            migrationBuilder.Sql("insert into FieldTypes(id,name) values(3,'DateTime')");
            migrationBuilder.Sql("insert into FieldTypes(id,name) values(6,'decimal')");
            migrationBuilder.Sql("insert into FieldTypes(id,name) values(5,'double')");
            migrationBuilder.Sql("insert into FieldTypes(id,name) values(0,'none')");

        }
    }
}
