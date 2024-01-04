using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Xml;

namespace Telegram.Messaging.Db
{
	public class MessagingDb : DbContext
	{
		const string DATABASE_NAME = "MessagingDb";
		const string CONFIG_FILENAME = "\\connections.tg.msg.config";

		public DbSet<Question> Questions { get; set; }
		public DbSet<DbFieldType> FieldTypes { get; set; }
		public DbSet<Survey> Surveys { get; set; }

		public MessagingDb() : base()
		{

		}
		public MessagingDb(DbContextOptions<MessagingDb> options) : base(options)
		{

		}
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Survey>()
				.HasMany(s => s.Questions);

			modelBuilder.Entity<DbFieldType>()
			   .HasAlternateKey(f => f.Name);

			modelBuilder.Entity<Question>().Property(x => x.ImageUrl).HasDefaultValue(null);

			modelBuilder.Entity<Question>().HasOne(q => q.Survey).WithMany(x => x.Questions);
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
			{
				optionsBuilder.EnableDetailedErrors();
				optionsBuilder.EnableSensitiveDataLogging();
				string? conn = null;
				var connString = System.Configuration.ConfigurationManager.ConnectionStrings[DATABASE_NAME];
				if (connString != null)
					conn = connString.ConnectionString;
				if (conn == null)
				{
					var configuration = new ConfigurationBuilder()
										.SetBasePath(Directory.GetCurrentDirectory())
										.AddJsonFile("appsettings.json")
										.Build();

					conn = configuration.GetConnectionString(DATABASE_NAME);
				}
				if (conn == null)
				{
					string dir = AppDomain.CurrentDomain.BaseDirectory;
					if (File.Exists(dir + CONFIG_FILENAME) == false)
						dir = Directory.GetCurrentDirectory();
					XmlDocument xml = new XmlDocument();
					xml.Load(dir + CONFIG_FILENAME);
					var conns = xml.DocumentElement.SelectNodes($"add[@name='{DATABASE_NAME}']");
					conn = conns[0].Attributes["connectionString"].Value;
				}
				optionsBuilder.UseSqlServer(conn);
				return;
			}
		}
	}
}
