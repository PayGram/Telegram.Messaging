using Telegram.Messaging.Db;

namespace Telegram.Messaging.Messaging
{
	public class PayReceivedEventArgs : MessagingEventArgs
	{
		public Question CurrentQuestion { get; internal set; }
		public PayReceivedEventArgs() : base(MessageEventTypes.PayReceived)
		{

		}
	}
}
