using Telegram.Bot.Types;
using Telegram.Messaging.Types;

namespace Telegram.Messaging.Messaging
{
	public class ContactSharedEventArgs : MessagingEventArgs
	{
		public TelegramMessage TelegramMessage { get; internal set; }
		public Contact Contact { get; internal set; }
		public ContactSharedEventArgs() : base(MessageEventTypes.ContactShared)
		{

		}
	}
}
