using Telegram.Messaging.Db;

namespace Telegram.Messaging.Messaging
{
	public class QuestionChangedEventArgs : MessagingEventArgs
	{
		public Question CurrentQuestion { get; internal set; }
		public bool HitSkip { get; internal set; }
		public bool HitBack { get; internal set; }
		public QuestionChangedEventArgs() : base(MessageEventTypes.QuestionChanged)
		{

		}
	}
}
