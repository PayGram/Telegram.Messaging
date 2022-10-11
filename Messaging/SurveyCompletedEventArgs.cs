using Telegram.Messaging.Db;
using Telegram.Messaging.Types;

namespace Telegram.Messaging.Messaging
{
	public class SurveyCompletedEventArgs : MessagingEventArgs
	{
		public List<TelegramAnswer> GivenAnswers { get; internal set; }
		public Survey Survey { get; internal set; }
		public SurveyCompletedEventArgs() : base(MessageEventTypes.SurveyCompleted)
		{

		}
	}
}
