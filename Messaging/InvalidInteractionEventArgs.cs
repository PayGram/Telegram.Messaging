using Telegram.Messaging.Db;
using Telegram.Messaging.Types;

namespace Telegram.Messaging.Messaging
{
	public class InvalidInteractionEventArgs : MessagingEventArgs
	{
		public TelegramMessage TelegramMessage { get; internal set; }
		public string Answer { get; internal set; }
		public TelegramChoice? PickedChoice { get; internal set; }
		public Question OriginatingQuestion { get; internal set; }
		public InvalidInteractionEventArgs() : base(MessageEventTypes.InvalidInteraction)
		{

		}
	}
}
