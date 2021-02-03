using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Telegram.Messaging.Db
{
	public class FieldType
	{
		[Key]
		public FieldTypes Id { get; set; }
		public string Name { get; set; }

		public static async Task<FieldType> CreateOrGet(string name)
		{
			using (var db = new MessagingDb())
			{
				var ft = new FieldType() { Name = name };
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
