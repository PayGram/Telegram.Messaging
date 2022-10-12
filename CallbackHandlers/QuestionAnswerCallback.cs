using Telegram.Messaging.Messaging;

namespace Telegram.Messaging.CallbackHandlers
{
	public class QuestionAnswerCallbackHandler : IQuestionAnswerCallbackHandler
	{
		public MessageManager Manager { get; set; }

		public virtual async Task OnDiceRolledAsync(MessageManager mngr, DiceRolledEventArgs e) { }
		public virtual async Task OnCommandAsync(MessageManager mngr, CommandReceivedEventArgs e) { }
		public virtual async Task OnQuestionAnsweredAsync(MessageManager mngr, QuestionAnsweredEventArgs e) { }
		public virtual async Task OnQuestionChangedAsync(MessageManager mngr, QuestionChangedEventArgs e) { }
		public virtual async Task OnPageChangedAsync(MessageManager mngr, ChangePageEventArgs e) { }
		public virtual async Task OnSurveyCancelledAsync(MessageManager mngr, SurveyCancelledEventArgs e) { }
		public virtual async Task OnSurveyCompletedAsync(MessageManager mngr, SurveyCompletedEventArgs e) { }
		public virtual async Task OnPayPressedAsync(MessageManager mngr, PayReceivedEventArgs e) { }
		public virtual async Task OnInvalidInteractionAsync(MessageManager mngr, InvalidInteractionEventArgs e) { }
		public QuestionAnswerCallbackHandler(MessageManager manager)
		{
			Manager = manager;
		}
	}
}
