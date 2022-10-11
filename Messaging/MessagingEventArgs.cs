namespace Telegram.Messaging.Messaging
{
	public enum MessageEventTypes
	{
		ChangePage,
		CommandReceived,
		SurveyCancelled,
		QuestionAnswered,
		DiceRolled,
		InvalidInteraction,
		PayReceived,
		QuestionChanged,
		SurveyCompleted
	}
	public class MessagingEventArgs : EventArgs
	{
		public MessageManager Manager { get; set; }
		public MessageEventTypes MessageEventType { get; set; }
		public MessagingEventArgs(MessageEventTypes messageEventType)
		{
			MessageEventType = messageEventType;
		}
	}
}
