using Telegram.Messaging.Db;
using Telegram.Messaging.Types;

namespace Telegram.Messaging.Messaging
{
	public class SurveyCancelledEventArgs : MessagingEventArgs
	{
		public Question CurrentQuestion { get; internal set; }
		public List<TelegramAnswer> GivenAnswers { get; internal set; }
		public Survey Survey { get; internal set; }
		public SurveyCancelledEventArgs() : base(MessageEventTypes.SurveyCancelled)
		{

		}
	}
}
