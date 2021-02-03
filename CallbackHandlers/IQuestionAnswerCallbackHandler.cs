using Telegram.Messaging.Messaging;

namespace Telegram.Messaging.CallbackHandlers
{
	public interface IQuestionAnswerCallbackHandler
	{
		MessageManager Manager { get; set; }
	}
}
