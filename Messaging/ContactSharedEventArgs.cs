using Telegram.Bot.Types;

namespace Telegram.Messaging.Messaging
{
	public class ContactSharedEventArgs : MessagingEventArgs
	{
		public string PhoneNumber { get; internal set; } = string.Empty;
		public string? FirstName { get; internal set; }
		public long UserId { get; internal set; }
		public long? ContactUserId { get; internal set; }
		public long ChatId { get; internal set; }
		public Contact? Contact { get; internal set; }

		public ContactSharedEventArgs() : base(MessageEventTypes.ContactShared)
		{
		}
	}
}

