using Microsoft.EntityFrameworkCore;
using System;
using System.Configuration;
using System.IO;
using System.Xml;

namespace Telegram.Messaging.Db
{
	public class MessagingDb : DbContext
	{
		const string DATABASE_NAME = "MessagingDb";
		const string CONFIG_FILENAME = "\\connections.tg.msg.config";

		public DbSet<Question> Questions { get; set; }
		public DbSet<FieldType> FieldTypes { get; set; }
		public DbSet<Survey> Surveys { get; set; }


		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Survey>()
				.HasMany(s => s.Questions);

			modelBuilder.Entity<FieldType>()
			   .HasAlternateKey(f => f.Name);

			modelBuilder.Entity<Question>().HasOne(q => q.Survey).WithMany(x => x.Questions);
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
			{
				optionsBuilder.EnableDetailedErrors();
				optionsBuilder.EnableSensitiveDataLogging();
				var connString = ConfigurationManager.ConnectionStrings[DATABASE_NAME];
				string conn;
				if (connString != null)
					conn = connString.ConnectionString;
				else
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
