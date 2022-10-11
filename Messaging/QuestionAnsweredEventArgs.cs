using Telegram.Messaging.Db;
using Telegram.Messaging.Types;

namespace Telegram.Messaging.Messaging
{
	public class QuestionAnsweredEventArgs : MessagingEventArgs
	{
		public Question AnsweredQuestion { get; internal set; }
		public TelegramAnswer Answer { get; internal set; }
		public QuestionAnsweredEventArgs() : base(MessageEventTypes.QuestionAnswered)
		{

		}
	}
}
