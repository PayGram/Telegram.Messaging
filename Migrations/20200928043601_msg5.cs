using Microsoft.EntityFrameworkCore.Migrations;

namespace Telegram.Messaging.Migrations
{
	public partial class msg5 : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<int>(name: "id1", "fieldtypes", defaultValue: 0, nullable: false);


			migrationBuilder.Sql("update fieldtypes set id1=id");

			migrationBuilder.Sql("insert into FieldTypes(id1,name) select 0, 'none' where not exists (select * from fieldtypes f where f.id=0)");
			migrationBuilder.Sql("insert into FieldTypes(id1,name) select 2, 'bool' where not exists (select * from fieldtypes f where f.id=2)");
			migrationBuilder.Sql("insert into FieldTypes(id1,name) select 3, 'DateTime' where not exists (select * from fieldtypes f where f.id=3)");
			migrationBuilder.Sql("insert into FieldTypes(id1,name) select 6, 'decimal' where not exists (select * from fieldtypes f where f.id=6)");
			migrationBuilder.Sql("insert into FieldTypes(id1,name) select 5, 'double' where not exists (select * from fieldtypes f where f.id=5)");
			migrationBuilder.Sql("insert into FieldTypes(id1,name) select 4, 'int' where not exists (select * from fieldtypes f where f.id=4)");
			migrationBuilder.Sql("insert into FieldTypes(id1,name) select 1, 'string' where not exists (select * from fieldtypes f where f.id=1)");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn("id1", "fieldtypes");
		}
	}
}
