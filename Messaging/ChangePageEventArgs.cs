using Telegram.Messaging.Db;

namespace Telegram.Messaging.Messaging
{
	public class ChangePageEventArgs : MessagingEventArgs
	{
		public Question CurrentQuestion { get; set; }
		public int CurrentPage { get; set; }
		public int RequestedPage { get; set; }
		public ChangePageEventArgs() : base(MessageEventTypes.ChangePage)
		{

		}
	}
}
