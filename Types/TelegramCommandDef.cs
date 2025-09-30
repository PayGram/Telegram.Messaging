namespace Telegram.Messaging.Types
{
	public class TelegramCommandDef
	{
		public string Label { get; set; }
		public string Name { get; set; }
		public bool ShowOnDashboard { get; set; }
		public bool IsWebApp { get; set; } = false;
		public TelegramCommandDef(string name, string label, bool showOnDashboard = false)
		{
			Label = label;
			Name = name;
			ShowOnDashboard = showOnDashboard;
		}

		public TelegramCommandDef(string name, string label, bool showOnDashboard, bool isWebApp) : this(name, label,
			showOnDashboard)
		{
			IsWebApp = isWebApp;
		}
	}
}
