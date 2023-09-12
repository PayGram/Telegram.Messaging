using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Telegram.Messaging.Types
{
	public class TelegramCommandDef
	{
		public string Label { get; set; }
		public string Name { get; set; }
		public bool ShowOnDashboard { get; set; }
		public TelegramCommandDef(string name, string label, bool showOnDashboard = false)
		{
			Label = label;
			Name = name;
			ShowOnDashboard = showOnDashboard;
		}
	}
}
