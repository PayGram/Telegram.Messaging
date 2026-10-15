using Newtonsoft.Json.Linq;
using Telegram.Messaging.Messaging;

namespace Telegram.Messaging.CallbackHandlers
{
	public class QuestionAnswerCallbackHandler : IQuestionAnswerCallbackHandler
	{
		public MessageManager Manager { get; set; }

		/// <summary>
		/// Returns a json-serializable snapshot of the in-memory state of this handler, or null if the handler has nothing to persist.
		/// The MessageManager stores it in <see cref="Db.Survey.HandlersState"/> every time the current survey is saved and gives it back
		/// through <see cref="SetState"/> when the handler is instantiated again, typically after a restart of the server.
		/// Keep it small: ids, page indexes, the current action. Data that comes from an api must be reloaded lazily instead.
		/// </summary>
		public virtual object? GetState() => null;
		/// <summary>
		/// Restores the state previously returned by <see cref="GetState"/>. Called once, right after the handler is created by the MessageManager,
		/// only when a state for this handler type was found in the current survey. Must tolerate states written by older versions of the handler.
		/// </summary>
		public virtual void SetState(JToken state) { }

		//public virtual async Task OnDiceRolledAsync(MessageManager mngr, DiceRolledEventArgs e) { }
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
