using Telegram.Messaging.Messaging;

namespace Telegram.Messaging.CallbackHandlers
{
	public class QuestionAnswerCallbackHandler : IQuestionAnswerCallbackHandler
	{
		public MessageManager Manager { get; set; }

		public virtual void OnDiceRolled(MessageManager mngr, DiceRolledEventArgs e) { }
		public virtual void OnCommand(MessageManager mngr, CommandReceivedEventArgs e) { }
		public virtual void OnQuestionAnswered(MessageManager mngr, QuestionAnsweredEventArgs e) { }
		public virtual void OnQuestionChanged(MessageManager mngr, QuestionChangedEventArgs e) { }
		public virtual void OnPageChanged(MessageManager mngr, ChangePageEventArgs e) { }
		public virtual void OnSurveyCancelled(MessageManager mngr, SurveyCancelledEventArgs e) { }
		public virtual void OnSurveyCompleted(MessageManager mngr, SurveyCompletedEventArgs e) { }
		public virtual void OnPayPressed(MessageManager mngr, PayReceivedEventArgs e) { }
		public virtual void OnInvalidInteraction(MessageManager mngr, InvalidInteractionEventArgs e) { }
	}
}
