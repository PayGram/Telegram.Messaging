using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Telegram.Messaging.Db
{
	/// <summary>
	/// Used only by the dotnet-ef tools (e.g. "dotnet ef migrations add X --framework net10.0") to build the model without a real
	/// connection string: the parameterless MessagingDb constructor would otherwise look for connections.tg.msg.config in the current directory.
	/// Never used at runtime.
	/// </summary>
	public class MessagingDbDesignTimeFactory : IDesignTimeDbContextFactory<MessagingDb>
	{
		public MessagingDb CreateDbContext(string[] args)
		{
			var options = new DbContextOptionsBuilder<MessagingDb>()
				.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=MessagingDb;Trusted_Connection=True;")
				.Options;
			return new MessagingDb(options);
		}
	}
}
