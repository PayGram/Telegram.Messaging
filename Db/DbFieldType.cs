using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Telegram.Messaging.Db
{
	public class DbFieldType
	{
		[Key]
		public FieldTypes Id { get; set; }
		public string Name { get; set; }

		public static async Task<DbFieldType?> CreateOrGet(string name)
		{
			using (var db = new MessagingDb())
			{
				var ft = new DbFieldType() { Name = name };
				try
				{
					db.FieldTypes.Add(ft);
					await db.SaveChangesAsync();
				}
				catch (Exception)
				{
					ft = await (from fts in db.FieldTypes where fts.Name == name select fts).SingleOrDefaultAsync();
				}
				return ft;
			}
		}
	}
}
