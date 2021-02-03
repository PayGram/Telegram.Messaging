using Telegram.Messaging.Db;
using Telegram.Messaging.Types;

namespace Telegram.Messaging.Messaging
{
	public class CommandReceivedEventArgs : MessagingEventArgs
	{
		public TelegramMessage TelegramMessage { get; internal set; }
		public TelegramCommand Command { get; internal set; }
		public Question OriginatingQuestion { get; internal set; }
	}
}
