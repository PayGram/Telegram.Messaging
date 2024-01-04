using log4net;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Web;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Messaging.CallbackHandlers;
using Telegram.Messaging.Db;
using Telegram.Messaging.Types;
using Utilities.Localization.Extentions;
using Utilities.String.Json.Extentions;

namespace Telegram.Messaging.Messaging
{
	public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e);

	public class MessageManager
	{
		#region Localization 
		const string MESSAGE_MANAGER_USE_NEW_DASHBOARD = "MessageManagerUseNewDashboard";
		#endregion
		static readonly ILog log = LogManager.GetLogger(typeof(MessageManager));

		/// <summary>
		/// The name of the bot
		/// </summary>
		public string BotName { get; private set; }
		/// <summary>
		/// The available valid commands for this bot, name-label
		/// </summary>
		public List<TelegramCommandDef> ValidCommands { get; private set; }
		/// <summary>
		/// The current message that we have just received from the user
		/// </summary>
		public TelegramMessage CurrentMessage { get; private set; }
		/// <summary>
		/// The current flow of questions and answers with the client
		/// </summary>
		public Survey? CurrentSurvey { get; private set; }
		public string? Language { get; set; }
		public string BotToken { get; private set; }
		//readonly ITelegramBotClient tClient;
		long _chatId;
		/// <summary>
		/// The Chat identifier where this Manager is addressing the messages. It has always precedence on the CurrentMessage.Chat.Id
		/// when it is specified. If not specified, this Id will be assigned after a message is processed
		/// </summary>
		public long ChatId { get => _chatId != 0 ? _chatId : CurrentMessage?.ChatId ?? 0; set => _chatId = value; }

		/// <summary>
		/// The username or the first name or an empty string if they are not available of the user with whom we are interacting
		/// gets an empty string if null
		/// </summary>
		public string UsernameOrFirstName => CurrentMessage?.From?.Username ?? CurrentMessage?.From?.FirstName ?? "";
		public string UsernameOrFirstNameHtmlEncoded => HttpUtility.HtmlEncode(UsernameOrFirstName);

		readonly long _tid;

		/// <summary>
		/// The telegram ID of the user with whom we are interacting or 0 if not found
		/// </summary>
		public long TId => _tid != 0 ? _tid : CurrentMessage?.From?.Id ?? 0;

		/// <summary>Represents how many messages have been sent after the dashboard.  Access to this variable must be synchronized with semSend</summary>
		int recentMessageSent;

		public event EventHandler<CommandReceivedEventArgs> OnCommand;
		public event EventHandler<QuestionAnsweredEventArgs> OnQuestionAnswered;
		public event EventHandler<QuestionChangedEventArgs> OnQuestionChanged;
		public event EventHandler<ChangePageEventArgs> OnPageChanged;
		public event EventHandler<SurveyCancelledEventArgs> OnSurveyCancelled;

		public event EventHandler<SurveyCompletedEventArgs> OnSurveyCompleted;
		public event EventHandler<PayReceivedEventArgs> OnPayPressed;
		public event EventHandler<InvalidInteractionEventArgs> OnInvalidInteraction;
		//public event EventHandler<DiceRolledEventArgs> OnDiceRolled;

		public event AsyncEventHandler<CommandReceivedEventArgs> OnCommandAsync;
		public event AsyncEventHandler<QuestionAnsweredEventArgs> OnQuestionAnsweredAsync;
		public event AsyncEventHandler<QuestionChangedEventArgs> OnQuestionChangedAsync;
		public event AsyncEventHandler<ChangePageEventArgs> OnPageChangedAsync;
		public event AsyncEventHandler<SurveyCancelledEventArgs> OnSurveyCancelledAsync;
		public event AsyncEventHandler<SurveyCompletedEventArgs> OnSurveyCompletedAsync;
		public event AsyncEventHandler<PayReceivedEventArgs> OnPayPressedAsync;
		public event AsyncEventHandler<InvalidInteractionEventArgs> OnInvalidInteractionAsync;
		//public event AsyncEventHandler<DiceRolledEventArgs> OnDiceRolledAsync;

		IServiceProvider serviceProvider;

		/// <summary>
		/// Creates a new MessageManager for the bot
		/// </summary> 
		/// <param name="botName">The name of the bot</param>
		/// <param name="botToken">The token of the bot</param>
		/// <param name="chatId">The chat id where this manager will address the messages. If it is left to its default value, bot will only be able to reply
		/// messages after an incoming message is processed</param>
		public MessageManager(string botName, string botToken, long chatId = 0, long userId = 0)
			: this(botName, botToken, null, chatId, userId)
		{
		}

		/// <summary>
		/// Creates a new MessageManager for the bot
		/// </summary> 
		/// <param name="botName">The name of the bot</param>
		/// <param name="botToken">The token of the bot</param>
		/// <param name="client">The client to use to reply to users
		/// <param name="chatId">The chat id where this manager will address the messages. If it is left to its default value, bot will only be able to reply
		/// messages after an incoming message is processed</param>
		public MessageManager(string botName, string botToken, IServiceProvider serviceProvider, long chatId = 0, long userId = 0)
		{
			ValidCommands = new();
			ChatId = chatId;
			BotName = botName;
			BotToken = botToken;
			//tClient = client ?? new TelegramBotClient(botToken);
			this.serviceProvider = serviceProvider;
			_tid = userId;
			recentMessageSent = 1; // will cause the dashboard to reload
			setEventsCallback();
		}
		/// <summary>
		/// Initializes an empty MessageManager
		/// </summary>
		internal MessageManager()
		{
		}

		void setEventsCallback()
		{
			OnCommand += MessageManager_OnCommand;
			OnQuestionAnswered += MessageManager_OnQuestionAnswered;
			OnQuestionChanged += MessageManager_OnQuestionChanged;
			OnPageChanged += MessageManager_OnPageChanged;
			OnSurveyCancelled += MessageManager_OnSurveyCancelled;
			OnSurveyCompleted += MessageManager_OnSurveyCompleted;
			OnPayPressed += MessageManager_OnPayPressed;
			OnInvalidInteraction += MessageManager_OnInvalidInteraction;
		}

		#region events logging
		private void MessageManager_OnInvalidInteraction(object? sender, InvalidInteractionEventArgs e)
		{
			log.Debug($"{TId}:{UsernameOrFirstName}. OnInvalidInteraction. Answer: {e.Answer}, PickedChoice: {e.PickedChoice?.ToJsonSpecial()}, TelegramMessage: {e.TelegramMessage}");
		}

		private void MessageManager_OnPayPressed(object? sender, PayReceivedEventArgs e)
		{
			log.Debug($"{TId}:{UsernameOrFirstName}. OnPayPressed. Answer: {e.CurrentQuestion}");
		}

		private void MessageManager_OnSurveyCompleted(object? sender, SurveyCompletedEventArgs e)
		{
			string answers = e.GivenAnswers != null ? JsonConvertExt.SerializeIgnoreAndPopulate(e.GivenAnswers) : "";
			log.Debug($"{TId}:{UsernameOrFirstName}. OnSurveyCompleted. Survey: {e.Survey}, Answers: {answers}");
		}


		private void MessageManager_OnSurveyCancelled(object? sender, SurveyCancelledEventArgs e)
		{
			string answers = e.GivenAnswers != null ? JsonConvertExt.SerializeIgnoreAndPopulate(e.GivenAnswers) : "";
			log.Debug($"{TId}:{UsernameOrFirstName}. OnSurveyCancelled. Survey: {e.Survey}, Answers: {answers}");
		}

		private void MessageManager_OnPageChanged(object? sender, ChangePageEventArgs e)
		{
			log.Debug($"{TId}:{UsernameOrFirstName}. OnPageChanged. CurrentQuestion: {e.CurrentQuestion}, CurrentPage: {e.CurrentPage}, RequestePage: {e.RequestedPage}");
		}

		private void MessageManager_OnQuestionChanged(object? sender, QuestionChangedEventArgs e)
		{
			log.Debug($"{TId}:{UsernameOrFirstName}. OnQuestionChanged. CurrentQuestion: {e.CurrentQuestion}, HitBack: {e.HitBack}, HitSkip: {e.HitSkip}");
		}

		private void MessageManager_OnQuestionAnswered(object? sender, QuestionAnsweredEventArgs e)
		{
			log.Debug($"{TId}:{UsernameOrFirstName}. OnQuestionAnswered. AnsweredQuestion: {e.AnsweredQuestion}, Answer: {e.Answer}");
		}

		private void MessageManager_OnCommand(object? sender, CommandReceivedEventArgs e)
		{
			log.Debug($"{TId}:{UsernameOrFirstName}. OnCommand. TelegramMessage: {e.TelegramMessage}, Command: {e.Command}");
		}
		#endregion

		#region Events firing
		readonly Dictionary<string, IQuestionAnswerCallbackHandler> callBackhandlers = new Dictionary<string, IQuestionAnswerCallbackHandler>();
		readonly object syncHndlrs = new object();
		public T GetHandler<T>() where T : QuestionAnswerCallbackHandler
		{
			return (T)GetHandler(typeof(T));
		}
		/// <summary>
		/// Gets or adds if not found the handler (instance) for the Question.CallbackHandler
		/// Target-instances are shared between the two different types of handlers
		/// </summary>
		/// <param name="key">The type of object whose instance is needed</param>
		/// <returns>the handler (instance) for the Question.CallbackHandler</returns>
		public QuestionAnswerCallbackHandler GetHandler(Type key)
		{
			string skey = key.ToString();
			lock (syncHndlrs)
			{
				if (callBackhandlers.ContainsKey(skey)) return callBackhandlers[skey] as QuestionAnswerCallbackHandler;
				var callbackHandler = Activator.CreateInstance(key, this) as QuestionAnswerCallbackHandler;
				//callbackHandler.Manager = this;
				callBackhandlers.Add(skey, callbackHandler);
				return callbackHandler;
			}
		}
		/// <summary>
		/// Gets a delegate whose target is the same handler that was eventually created by preceeding calls.
		/// Target-instances are shared between the two different types of handlers
		/// </summary>
		/// <param name="forAction">The Question.OnEvent whose instance target is needed</param>
		/// <returns>a delegate whose target is the same handler that was eventually created by preceeding calls</returns>
		// public Action<MessagingEventArgs>? GetDelegateOnTarget(Action<MessagingEventArgs> forAction)
		public AsyncEventHandler<MessagingEventArgs>? GetDelegateOnTarget(AsyncEventHandler<MessagingEventArgs> forAction)
		{
			if (forAction == null) return null;
			if (forAction.Method.IsStatic) return forAction;

			string? skey = forAction.Method.DeclaringType?.ToString();
			if (skey == null) return null;

			IQuestionAnswerCallbackHandler target;
			AsyncEventHandler<MessagingEventArgs> toret;
			lock (syncHndlrs)
			{
				if (callBackhandlers.ContainsKey(skey))
				{
					// we already have a target-handler for this action, we must create  a new delegate
					// that takes as target the object that we already have
					target = callBackhandlers[skey];
					toret = (AsyncEventHandler<MessagingEventArgs>)Delegate.CreateDelegate(typeof(AsyncEventHandler<MessagingEventArgs>), target, forAction.Method);
				}
				else
				{
					// we don't have a target-handler for this action, we can add the target to our list of handlers
					// so next time we will use it, but first we must check that this target is not a dummy (see Question.MethodNameOnEvent)
					target = (IQuestionAnswerCallbackHandler)forAction.Target;
					if (target.Manager == null)//dummy, drop it
					{
						target = Activator.CreateInstance(forAction.Method.DeclaringType, this) as QuestionAnswerCallbackHandler;
						toret = (AsyncEventHandler<MessagingEventArgs>)Delegate.CreateDelegate(typeof(AsyncEventHandler<MessagingEventArgs>), target, forAction.Method); // we recreate a new one because the constructor might do some initializations that assigning the manager through the property wouldnt happens
					}
					else
						toret = forAction;
					callBackhandlers.Add(skey, target);
				}
			}
			return toret;
		}
		async Task RaiseOnCommand(CommandReceivedEventArgs e)
		{
			e.Manager = this;
			List<Task> tasks = new List<Task>();
			if (OnCommand != null)
				Array.ForEach(OnCommand.GetInvocationList(), (d) => configureAndAddTask(tasks, d, e));
			if (OnCommandAsync != null)
				Array.ForEach(OnCommandAsync.GetInvocationList(), (d) => configureAndAddTask(tasks, d, e));

			if (e?.OriginatingQuestion?.CallbackHandler != null)
			{
				var callbackHandler = GetHandler(e.OriginatingQuestion.CallbackHandler);
				//var t = Task.Run(() => callbackHandler.OnCommand(this, e));
				var t = callbackHandler.OnCommandAsync(this, e);
				t.ConfigureAwait(false);
				tasks.Add(t);
			}
			if (e?.OriginatingQuestion?.OnEventAsync != null)
			{
				var onEvent = GetDelegateOnTarget(e.OriginatingQuestion.OnEventAsync);
				//Task t = Task.Run(() => onEvent(e));
				var t = onEvent(this, e);
				t.ConfigureAwait(false);
				tasks.Add(t);
			}
			await Task.WhenAll(tasks);
		}
		//async Task RaiseOnDiceRolled(DiceRolledEventArgs e)
		//{
		//	e.Manager = this;
		//	List<Task> tasks = new List<Task>();
		//	if (OnDiceRolled != null)
		//		Array.ForEach(OnDiceRolled.GetInvocationList(), (d) => configureAndAddTask(tasks, d, e));
		//	if (OnDiceRolledAsync != null)
		//		Array.ForEach(OnDiceRolledAsync.GetInvocationList(), (d) => configureAndAddTask(tasks, d, e));
		//	if (e?.OriginatingQuestion?.CallbackHandler != null)
		//	{
		//		var callbackHandler = GetHandler(e.OriginatingQuestion.CallbackHandler) as QuestionAnswerCallbackHandler;
		//		var t = callbackHandler.OnDiceRolledAsync(this, e);
		//		t.ConfigureAwait(false);
		//		tasks.Add(t);
		//	}
		//	if (e?.OriginatingQuestion?.OnEventAsync != null)
		//	{
		//		var onEvent = GetDelegateOnTarget(e.OriginatingQuestion.OnEventAsync);
		//		//Task t = Task.Run(() => onEvent(e));
		//		Task t = onEvent(this, e);
		//		t.ConfigureAwait(false);
		//		tasks.Add(t);
		//	}
		//	await Task.WhenAll(tasks);
		//}
		async Task RaiseOnQuestionAnswered(QuestionAnsweredEventArgs e)
		{
			e.Manager = this;
			List<Task> tasks = new List<Task>();
			if (OnQuestionAnswered != null)
				Array.ForEach(OnQuestionAnswered.GetInvocationList(), (d) => configureAndAddTask(tasks, d, e));
			if (OnQuestionAnsweredAsync != null)
				Array.ForEach(OnQuestionAnsweredAsync.GetInvocationList(), (d) => configureAndAddTask(tasks, d, e));
			if (e?.AnsweredQuestion?.CallbackHandler != null)
			{
				var callbackHandler = GetHandler(e.AnsweredQuestion.CallbackHandler) as QuestionAnswerCallbackHandler;
				var t = callbackHandler.OnQuestionAnsweredAsync(this, e);
				t.ConfigureAwait(false);
				tasks.Add(t);
			}
			if (e?.AnsweredQuestion?.OnEventAsync != null)
			{
				var onEvent = GetDelegateOnTarget(e.AnsweredQuestion.OnEventAsync);
				//                Task t = Task.Run(() => onEvent(e));
				Task t = onEvent(this, e);
				t.ConfigureAwait(false);
				tasks.Add(t);
			}
			await Task.WhenAll(tasks);
		}
		async Task RaiseOnQuestionChanged(QuestionChangedEventArgs e)
		{
			e.Manager = this;
			List<Task> tasks = new List<Task>();
			if (OnQuestionChanged != null)
				Array.ForEach(OnQuestionChanged.GetInvocationList(), (d) => configureAndAddTask(tasks, d, e));
			if (OnQuestionChangedAsync != null)
				Array.ForEach(OnQuestionChangedAsync.GetInvocationList(), (d) => configureAndAddTask(tasks, d, e));
			if (e?.CurrentQuestion?.CallbackHandler != null)
			{
				var callbackHandler = GetHandler(e.CurrentQuestion.CallbackHandler) as QuestionAnswerCallbackHandler;
				var t = callbackHandler.OnQuestionChangedAsync(this, e);
				t.ConfigureAwait(false);
				tasks.Add(t);
			}
			if (e?.CurrentQuestion?.OnEventAsync != null)
			{
				var onEvent = GetDelegateOnTarget(e.CurrentQuestion.OnEventAsync);
				//Task t = Task.Run(() => onEvent(e));
				Task t = onEvent(this, e);
				t.ConfigureAwait(false);
				tasks.Add(t);
			}
			await Task.WhenAll(tasks);
		}
		async Task RaiseOnPageChanged(ChangePageEventArgs e)
		{
			e.Manager = this;
			List<Task> tasks = new List<Task>();
			if (OnPageChanged != null)
				Array.ForEach(OnPageChanged.GetInvocationList(), (d) => configureAndAddTask(tasks, d, e));
			if (OnPageChangedAsync != null)
				Array.ForEach(OnPageChangedAsync.GetInvocationList(), (d) => configureAndAddTask(tasks, d, e));
			if (e?.CurrentQuestion?.CallbackHandler != null)
			{
				var callbackHandler = GetHandler(e.CurrentQuestion.CallbackHandler) as QuestionAnswerCallbackHandler;
				var t = callbackHandler.OnPageChangedAsync(this, e);
				t.ConfigureAwait(false);
				tasks.Add(t);
			}
			if (e?.CurrentQuestion?.OnEventAsync != null)
			{
				var onEvent = GetDelegateOnTarget(e.CurrentQuestion.OnEventAsync);
				//Task t = Task.Run(() => onEvent(e));
				Task t = onEvent(this, e);

				t.ConfigureAwait(false);
				tasks.Add(t);
			}
			await Task.WhenAll(tasks);
		}
		async Task RaiseOnSurveyCancelled(SurveyCancelledEventArgs e)
		{
			e.Manager = this;
			List<Task> tasks = new List<Task>();
			if (OnSurveyCancelled != null)
				Array.ForEach(OnSurveyCancelled.GetInvocationList(), (d) => configureAndAddTask(tasks, d, e));
			if (OnSurveyCancelledAsync != null)
				Array.ForEach(OnSurveyCancelledAsync.GetInvocationList(), (d) => configureAndAddTask(tasks, d, e));
			if (e?.CurrentQuestion?.CallbackHandler != null)
			{
				var callbackHandler = GetHandler(e.CurrentQuestion.CallbackHandler) as QuestionAnswerCallbackHandler;
				var t = callbackHandler.OnSurveyCancelledAsync(this, e);
				t.ConfigureAwait(false);
				tasks.Add(t);
			}
			if (e?.CurrentQuestion?.OnEventAsync != null)
			{
				var onEvent = GetDelegateOnTarget(e.CurrentQuestion.OnEventAsync);
				//Task t = Task.Run(() => onEvent(e));
				Task t = onEvent(this, e);
				t.ConfigureAwait(false);
				tasks.Add(t);
			}
			await Task.WhenAll(tasks);
		}
		async Task RaiseOnSurveyCompleted(SurveyCompletedEventArgs e)
		{
			e.Manager = this;
			List<Task> tasks = new List<Task>();
			if (OnSurveyCompleted != null)
				Array.ForEach(OnSurveyCompleted.GetInvocationList(), (d) => configureAndAddTask(tasks, d, e));
			if (OnSurveyCompletedAsync != null)
				Array.ForEach(OnSurveyCompletedAsync.GetInvocationList(), (d) => configureAndAddTask(tasks, d, e));
			if (e?.Survey?.MostRecentQuestion?.CallbackHandler != null)
			{
				var callbackHandler = GetHandler(e.Survey.MostRecentQuestion.CallbackHandler) as QuestionAnswerCallbackHandler;
				var t = callbackHandler.OnSurveyCompletedAsync(this, e);
				t.ConfigureAwait(false);
				tasks.Add(t);
			}
			if (e?.Survey?.MostRecentQuestion?.OnEventAsync != null)
			{
				var onEvent = GetDelegateOnTarget(e.Survey.MostRecentQuestion.OnEventAsync);
				//Task t = Task.Run(() => onEvent(e));
				Task t = onEvent(this, e);
				t.ConfigureAwait(false);
				tasks.Add(t);
			}
			await Task.WhenAll(tasks);
		}
		async Task RaiseOnPayPressed(PayReceivedEventArgs e)
		{
			e.Manager = this;
			List<Task> tasks = new List<Task>();
			if (OnPayPressed != null)
				Array.ForEach(OnPayPressed.GetInvocationList(), (d) => configureAndAddTask(tasks, d, e));
			if (OnPayPressedAsync != null)
				Array.ForEach(OnPayPressedAsync.GetInvocationList(), (d) => configureAndAddTask(tasks, d, e));
			if (e?.CurrentQuestion?.CallbackHandler != null)
			{
				var callbackHandler = GetHandler(e.CurrentQuestion.CallbackHandler) as QuestionAnswerCallbackHandler;
				var t = callbackHandler.OnPayPressedAsync(this, e);
				t.ConfigureAwait(false);
				tasks.Add(t);
			}
			if (e?.CurrentQuestion?.OnEventAsync != null)
			{
				var onEvent = GetDelegateOnTarget(e.CurrentQuestion.OnEventAsync);
				//Task t = Task.Run(() => onEvent(e));
				Task t = onEvent(this, e);
				t.ConfigureAwait(false);
				tasks.Add(t);
			}
			await Task.WhenAll(tasks);
		}
		async Task RaiseOnInvalidInteraction(InvalidInteractionEventArgs e)
		{
			e.Manager = this;
			List<Task> tasks = new List<Task>();
			if (OnInvalidInteraction != null)
				Array.ForEach(OnInvalidInteraction.GetInvocationList(), (d) => configureAndAddTask(tasks, d, e));
			if (OnInvalidInteractionAsync != null)
				Array.ForEach(OnInvalidInteractionAsync.GetInvocationList(), (d) => configureAndAddTask(tasks, d, e));
			if (e?.OriginatingQuestion?.CallbackHandler != null)
			{
				var callbackHandler = GetHandler(e.OriginatingQuestion.CallbackHandler);
				var t = callbackHandler.OnInvalidInteractionAsync(this, e);
				t.ConfigureAwait(false);
				tasks.Add(t);
			}
			if (e?.OriginatingQuestion?.OnEventAsync != null)
			{
				var onEvent = GetDelegateOnTarget(e.OriginatingQuestion.OnEventAsync);
				//Task t = Task.Run(() => onEvent(e));
				Task t = onEvent(this, e);
				t.ConfigureAwait(false);
				tasks.Add(t);
			}
			await Task.WhenAll(tasks);
		}
		/// <summary>
		/// Adds the task corresponding to the passed delegate to the list of tasks and invokes it
		/// </summary>
		/// <param name="tasks">The list of tasks</param>
		/// <param name="d">The delegate to invoke</param>
		/// <param name="e">The EventArgs for the delegate</param>
		void configureAndAddTask(List<Task> tasks, Delegate d, EventArgs e)
		{
			Task t;
			if (d.Method.ReturnType == typeof(Task))
				t = (Task)d.DynamicInvoke(this, e);
			else
				t = Task.Run(() => d.DynamicInvoke(this, e));
			t.ConfigureAwait(false);
			tasks.Add(t);
		}
		#endregion

		#region processing
		readonly Semaphore m = new Semaphore(1, 1);

		/// <summary>
		/// Releases the lock and allows other calls to StartProcessing
		/// </summary>
		public void EndProcessing()
		{
			m.Release();
			//log.Debug("previous count " + pc);
		}

		/// <summary>
		/// Acquires a lock and make other call block until EndProcessing is called
		/// </summary>
		public bool StartProcessing()
		{
			try
			{
				bool b = m.WaitOne(50);
				return b;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Process the incoming update from telegram
		/// </summary>
		/// <param name="upd">The update to process</param>
		/// <returns>The TelegramMessage describing the communication flow with the user</returns>
		public async Task<TelegramMessage?> ProcessUpdate(Update upd)
		{
			if (upd == null) return null;

			if (upd.CallbackQuery != null)
				return await ProcessUpdate(upd.CallbackQuery);

			else if (upd.Message != null)
				return await ProcessUpdate(upd.Message);

			return null;
		}

		/// <summary>
		/// Process an incoming telegram Message and returns a TelegramMessage which tracks the current 
		/// flow of communication with the user
		/// </summary>
		/// <param name="message">The message coming from telegram</param>
		/// <returns></returns>
		public async Task<TelegramMessage?> ProcessUpdate(Message message)
		{
			CurrentMessage = new TelegramMessage(message, BotName, ValidCommands.Select(x => x.Name).ToArray());
			return await ProcessCurrentMessage();
		}

		/// <summary>
		/// Creates a list of Choices pickable by the user based on the commands
		/// </summary>
		/// <returns></returns>
		private List<TelegramChoice> CreateChoicesFromCommands()
		{
			List<TelegramChoice> choices = new List<TelegramChoice>();
			foreach (var comm in ValidCommands)
				if (comm.ShowOnDashboard)
					choices.Add(new TelegramChoice(comm.Label, comm.Name));
			return choices;
		}

		/// <summary>
		/// Process an incoming telegram CallbackQuery and returns a TelegramMessage which tracks the current 
		/// flow of communication with the user
		/// </summary>
		/// <param name="message">The CallbackQuery coming from telgram</param>
		/// <returns></returns>
		public async Task<TelegramMessage?> ProcessUpdate(CallbackQuery message)
		{
			CurrentMessage = new TelegramMessage(message, BotName, ValidCommands.Select(x => x.Name).ToArray());
			return await ProcessCurrentMessage();
		}

		/// <summary>
		/// Process the command previously parsed and loaded
		/// </summary>
		/// <returns>The associated TelegramMessage or null</returns>
		private async Task<TelegramMessage?> ProcessCurrentMessage()
		{
			//checked some setup
			if (CurrentMessage == null)
			{
				log.Debug($"{TId}:{UsernameOrFirstName}. CurrentMessage is null {BotName}:{BotToken}");
				return null;
			}

			try
			{
				// let's see if there is an open survey
				CurrentSurvey = await Survey.GetCurrentSurvey(ChatId);
				if (CurrentSurvey == null)//user is new or db was cleaned, let's create a new survey for him
					await CreateNewSurvey();

				// the question that was supposedly answered
				Question? mostRecent = CurrentSurvey?.MostRecentQuestion;

				TelegramChoice pickedChoice = CurrentMessage.PickedChoice;
				long originatingMsgId = CurrentMessage.OriginatingMessageId != 0 ? CurrentMessage.OriginatingMessageId : DashboardMsgId;
				long originatingQuestId = pickedChoice?.QuestionId ?? mostRecent?.Id ?? 0;

				// let's see the previous question, maybe we can get the messageId that is still displaying a menu, in this case we remove it
				if (mostRecent == null && originatingQuestId == 0)
				{
					var mostRecentExpired = await Question.GetMostRecentAsync(TId);
					if (mostRecentExpired != null)
					{
						if (originatingQuestId == 0)
							originatingQuestId = mostRecentExpired.Id;
					}
				}

				//bool currentSurveyIsNull = CurrentSurvey == null;
				bool originatingQuestIdMismatch = mostRecent != null && mostRecent.Id != originatingQuestId;
				bool originatingMsgIdMismatch = CurrentSurvey != null && CurrentSurvey.TelegramMessageId != originatingMsgId;
				bool dashboardScrolledUp = CurrentMessage.IsCallbackQuery == false || recentMessageSent > 0;

				// if the following happens, we must delete the message, it's too old or it scrolled up or it mismatch with the current situation
				if (originatingMsgId != 0 && (/*currentSurveyIsNull ||*/ dashboardScrolledUp || originatingQuestIdMismatch || originatingMsgIdMismatch))
				{
					log.Debug($"{this} originatingMsgId: {originatingMsgId}, mostRecent.Id: {mostRecent?.Id}, originatingQuestId: {originatingQuestId}, CurrentSurvey.TelegramMessageId: {CurrentSurvey?.TelegramMessageId}, originatingMsgId: {originatingMsgId}");
					log.Debug($"{this}. removing clicked message. originatingMsgId: {originatingMsgId}, dashboardScrolledUp: {dashboardScrolledUp}, originatingQuestIdMismatch {originatingQuestIdMismatch}, originatingMsgIdMismatch: {originatingMsgIdMismatch}.");
					if (await RemoveMessageAsync(originatingMsgId) == false)
						CurrentMessage.AddCallbackQueryMessage(MESSAGE_MANAGER_USE_NEW_DASHBOARD.Translate(Language));
				}

				//CurrentMessage.AddCallbackQueryMessage("hello world");

				// something did not go as expected
				if (/*currentSurveyIsNull ||*/ originatingQuestIdMismatch || originatingMsgIdMismatch)
				{
					log.Debug($"{this} originatingMsgId: {originatingMsgId}, mostRecent.Id: {mostRecent?.Id}, originatingQuestId: {originatingQuestId}, CurrentSurvey.TelegramMessageId: {CurrentSurvey?.TelegramMessageId}, originatingMsgId: {originatingMsgId}");
					log.Debug($"{this}. Invalid answer, originatingQuestIdMismatch: {originatingQuestIdMismatch}, originatingMsgIdMismatch: {originatingMsgIdMismatch}. Raise invalid interaction and then process eventual commands");
					await RaiseOnInvalidInteraction(new InvalidInteractionEventArgs()
					{
						TelegramMessage = CurrentMessage,
						PickedChoice = pickedChoice,
						Answer = CurrentMessage.OriginalInputText,
						OriginatingQuestion = pickedChoice != null && mostRecent != null && pickedChoice.QuestionId != mostRecent.InternalId ? null : mostRecent
					});
					// always process the command
					if (CurrentMessage.Command.IsValidCommand)
						await processCommand(null, null);
					return CurrentMessage;
				}

				// Create the answer descriptor
				TelegramAnswer? answer = null;
				if (mostRecent != null && (pickedChoice != null || CurrentMessage.IsPhoto))
					answer = new TelegramAnswer(mostRecent, CurrentMessage);
				else if (mostRecent != null)
					answer = new TelegramAnswer(mostRecent, CurrentMessage.OriginalInputText);

				// Now we start the real processing
				// Processing ORDER:
				// 1. check if message is a SystemEvent 
				// 2. check if it's a command and eventually process it
				// 3. check if the answer is valid for the current question and eventually process it

				if (pickedChoice != null && pickedChoice.IsSystemChoice)
				{
					// process if it's a system choice
					await processSystemChoice(mostRecent, answer);
				}
				else if (CurrentMessage.Command.IsValidCommand)
				{
					// process command
					await processCommand(mostRecent, answer);
				}
				else
				{
					// process answer
					await processAnswer(mostRecent, answer);
				}

				if (CurrentSurvey != null && // == null if the survey was just cancelled && 
					mostRecent?.IsCompleted == true && CurrentSurvey.MostRecentQuestion == mostRecent)
					await CompleteSurvey();

				if (CurrentSurvey != null) // update the survey, so we update the lastInteractionDate automatically
					await CurrentSurvey.UpdateSurvey(false);

				log.Debug($"{TId}:{UsernameOrFirstName}. MessageManager finished now {DateTime.Now:ss.fff}");

				return CurrentMessage;
			}
			catch (Exception e)
			{
				log.Error($"{TId}:{UsernameOrFirstName}. Error processing current command", e);
				return null;
			}
		}

		private async Task processAnswer(Question mostRecent, TelegramAnswer answer)
		{
			if (mostRecent == null || answer == null || mostRecent.IsCompleted || mostRecent.ExpectsCommand)
			{
				log.Debug($"{TId}:{UsernameOrFirstName}. invalidInteraction on processAnswer: {mostRecent}-{answer}");
				await RaiseOnInvalidInteraction(new InvalidInteractionEventArgs()
				{
					Answer = CurrentMessage.OriginalInputText, // the passed answer is an answer to an existing question and it can be null, so we use the inputtext
					OriginatingQuestion = mostRecent,
					PickedChoice = answer?.PickedChoice,
					TelegramMessage = CurrentMessage
				});
				return;
			}
			mostRecent.AddAnswer(answer);
			await mostRecent.UpdateQuestion();
			//if (CurrentMessage.IsDice)
			//{
			//	await RaiseOnDiceRolled(new DiceRolledEventArgs() { TelegramMessage = CurrentMessage, Value = CurrentMessage.Message.Dice.Value, OriginatingQuestion = mostRecent });
			//}
			//else
			await RaiseOnQuestionAnswered(new QuestionAnsweredEventArgs() { Answer = answer, AnsweredQuestion = mostRecent });
		}

		private async Task processSystemChoice(Question mostRecent, TelegramAnswer answer)
		{
			if (mostRecent == null || mostRecent.IsCompleted || answer == null)
			{
				log.Debug($"{TId}:{UsernameOrFirstName}. Invalid interaction on processSystemChoice: {mostRecent}-{answer}");
				await RaiseOnInvalidInteraction(new InvalidInteractionEventArgs()
				{
					Answer = CurrentMessage.OriginalInputText, // the passed answer is an answer to an existing question and it can be null, so we use the inputtext
					OriginatingQuestion = mostRecent,
					PickedChoice = answer?.PickedChoice,
					TelegramMessage = CurrentMessage
				});
				return;
			}

			TelegramChoice pickedChoice = answer?.PickedChoice;
			bool HitCancel = TelegramChoice._CancelAnswer.Equals(answer.PickedChoice);
			bool IsSkept = TelegramChoice._SkipAnswer.Equals(answer.PickedChoice);
			bool HitBack = CurrentSurvey.Questions.Count > 1 && TelegramChoice._BackAnswer.Equals(answer.PickedChoice);

			if (HitBack)
			{
				answer.Answer = null;
				CurrentSurvey.Questions.RemoveAt(CurrentSurvey.Questions.Count - 1);
				await mostRecent.Delete();
				if (CurrentSurvey.MostRecentQuestion != null)
				{
					CurrentSurvey.MostRecentQuestion.IsCompleted = false;
					await CurrentSurvey.MostRecentQuestion.UpdateQuestion();
				}
				await RaiseOnQuestionChanged(new QuestionChangedEventArgs() { CurrentQuestion = mostRecent, HitBack = true });
			}
			else
			{
				mostRecent.AddAnswer(answer);
				if (IsSkept)
				{
					answer.Answer = "";
					mostRecent.IsCompleted = mostRecent.IsMandatory == false;
					if (mostRecent.IsCompleted)
						await RaiseOnQuestionChanged(new QuestionChangedEventArgs() { CurrentQuestion = mostRecent, HitSkip = true });
					else
					{
						log.Debug($"{TId}:{UsernameOrFirstName}. Invalid interaction on processSystemChoice-isSkept: {mostRecent}-{answer}");
						await RaiseOnInvalidInteraction(new InvalidInteractionEventArgs()
						{
							Answer = CurrentMessage.OriginalInputText,
							OriginatingQuestion = mostRecent,
							PickedChoice = answer?.PickedChoice,
							TelegramMessage = CurrentMessage
						});
					}
				}
				else if (HitCancel)
				{
					answer.Answer = "";
					await CancelSurvey();
				}
				else if (TelegramChoice._PayAnswer.Equals(answer.PickedChoice))
				{
					mostRecent.IsCompleted = true;
					await RaiseOnPayPressed(new PayReceivedEventArgs() { CurrentQuestion = mostRecent });
				}
				else if (TelegramChoice._CurrPageAnswer.Equals(answer.PickedChoice))
				{
					var currPage = pickedChoice != null ? int.Parse(pickedChoice.Param) : 0;
					await RaiseOnPageChanged(new ChangePageEventArgs() { CurrentQuestion = mostRecent, CurrentPage = currPage, RequestedPage = currPage });
				}
				else if (TelegramChoice._NextPageAnswer.Equals(answer.PickedChoice))
				{
					var currPage = pickedChoice != null ? int.Parse(pickedChoice.Param) : 0;
					await RaiseOnPageChanged(new ChangePageEventArgs() { CurrentQuestion = mostRecent, CurrentPage = currPage, RequestedPage = currPage + 1 });
				}
				else if (TelegramChoice._PrevPageAnswer.Equals(answer.PickedChoice))
				{
					var currPage = pickedChoice != null ? int.Parse(pickedChoice.Param) : 0;
					await RaiseOnPageChanged(new ChangePageEventArgs() { CurrentQuestion = mostRecent, CurrentPage = currPage, RequestedPage = currPage - 1 });
				}
				else // unhandled sysChoice
				{
					log.Debug($"{TId}:{UsernameOrFirstName}. Unhandled system choice {answer?.PickedChoice}");
					await RaiseOnInvalidInteraction(new InvalidInteractionEventArgs()
					{
						Answer = CurrentMessage.OriginalInputText,
						OriginatingQuestion = mostRecent,
						PickedChoice = answer?.PickedChoice,
						TelegramMessage = CurrentMessage
					});
				}
				await mostRecent.UpdateQuestion();
			}
		}

		private async Task processCommand(Question? mostRecent, TelegramAnswer? answer)
		{
			if (mostRecent != null && (mostRecent.ExpectsCommand == false || mostRecent.IsCompleted))
			{
				log.Debug($"{TId}:{UsernameOrFirstName}. Invalid interaction on processCommand: {mostRecent}-{answer}");
				// in this case the user sent a command, breaking the communication flow
				// we leave to the caller what how to handle this situation
				await RaiseOnInvalidInteraction(new InvalidInteractionEventArgs()
				{
					Answer = CurrentMessage.OriginalInputText,
					OriginatingQuestion = mostRecent,
					PickedChoice = answer?.PickedChoice,
					TelegramMessage = CurrentMessage
				});
			}

			// however we always notify that a command was received
			if (mostRecent != null && mostRecent.ExpectsCommand && mostRecent.IsCompleted == false)
			{
				mostRecent.AddAnswer(answer);
				await mostRecent.UpdateQuestion();
			}

			await RaiseOnCommand(new CommandReceivedEventArgs() { Command = CurrentMessage.Command, OriginatingQuestion = mostRecent, TelegramMessage = CurrentMessage });
		}

		#endregion

		#region managing
		/// <summary>
		/// Adds a question to the existing context, taking the existing open survey from it.
		/// If there is no existing survey, a new one is created
		/// </summary>
		/// <param name="qid">It's an identifier for this question that can be used later to determine the next question. Pass 0 if you are asking one question only</param>
		/// <param name="isMandatory">Whether the question is mandatory</param>
		/// <param name="text">The text of the question</param>
		/// <param name="pickOnlyDefAnswers">Whether the user can only choose from the selected answers</param>
		/// <param name="type">The data type of the answer</param>
		/// <param name="defAnswers">The default answers</param>
		/// <returns>The sent question</returns>
		public async Task<Question> AddQuestion(int qid, string text, string? followUp = null,
												bool isMandatory = false,
												bool isPay = false,
												bool pickOnlyDefAnswers = false,
												FieldTypes type = FieldTypes.String,
												List<TelegramChoice>? defAnswers = null,
												List<TelegramConstraint>? defConstraints = null,
												int currentPage = 0, bool hasNextPage = false, bool hasPrevPage = false,
												bool showSkip = false, bool showBack = false, bool showCancel = false, bool expectsCommand = false)
		{
			if (CurrentSurvey == null)
				CurrentSurvey = await CreateNewSurvey();
			if (CurrentSurvey == null) return null;

			if (defAnswers == null)
				defAnswers = new List<TelegramChoice>();

			// back page, current page and next page should be placed on a new row at the end
			if (hasPrevPage)
				defAnswers.Add(new TelegramChoice(TelegramChoice._PrevPageAnswer) { Param = currentPage.ToString() });
			if (hasPrevPage || hasNextPage)
				defAnswers.Add(new TelegramChoice(TelegramChoice._CurrPageAnswer) { Param = currentPage.ToString() });
			if (hasNextPage)
				defAnswers.Add(new TelegramChoice(TelegramChoice._NextPageAnswer) { Param = currentPage.ToString() });

			// back, cancel and skip should be at the end because they will be placed on a new row
			if (showBack)
				defAnswers.Add(TelegramChoice.BackAnswer);
			if (showCancel)
				defAnswers.Add(TelegramChoice.CancelAnswer);

			if (isMandatory == false && showSkip)
				defAnswers.Add(TelegramChoice.SkipAnswer);
			if (isPay)
				defAnswers.Insert(0, TelegramChoice.PayAnswer); // the pay button must stay on top

			Question q = new Question(qid)
			{
				IsMandatory = isMandatory,
				FieldTypeId = type,
				Survey = CurrentSurvey,
				PickOnlyDefaultAnswers = pickOnlyDefAnswers,
				QuestionText = text,
				ExpectsCommand = expectsCommand,
				FollowUp = followUp
			};
			q.DeriveConstraintFromFieldType();
			if (defConstraints != null)
				q.TelegramConstraints.AddRange(defConstraints);
			if (defAnswers != null)
				q.AddDefaultAnswers(defAnswers);
			CurrentSurvey.Questions.Add(q);
			await CurrentSurvey.UpdateSurvey(true);
			return q;
		}
		/// <summary>
		/// Creates a question which takes command as answers
		/// </summary>
		/// <param name="qid">The internal question id or 0</param>
		/// <param name="title">The title of the question</param>
		/// <param name="text">The text to display on the question</param>
		/// <param name="type">The type of the expected result</param>
		/// <param name="currentPage">If paged, the current page</param>
		/// <param name="hasNextPage">If there is a next page</param>
		/// <param name="hasPrevPage">If there is a previous page</param>
		/// <returns>The created question</returns>
		public async Task<Question> CreateCommandsQuestion(int qid, string title, string text = null,
												FieldTypes type = FieldTypes.String,
												int currentPage = 0, bool hasNextPage = false, bool hasPrevPage = false)
		{
			Question q = await AddQuestion(qid, title, text, true, false, true, type: type, defAnswers: CreateChoicesFromCommands(),
				currentPage: currentPage,
				hasNextPage: hasNextPage, hasPrevPage: hasPrevPage, expectsCommand: true);

			return q;
		}

		/// <summary>
		/// Changes the page content of a given question. If the question is completed, it will be marked as not completed. 
		/// </summary>
		/// <param name="question">The question that must be show a new page</param>
		/// <param name="text">The question text to display. Pass null to keep the previous one, pass empty string to remove it.</param>
		/// <param name="followUp">The follow up text to display. Pass null to keep the previous one, pass empty string to remove it.</param>
		/// <param name="defAnswers">Pass null to keep the previous choices, changing the system commands only. Pass an empty list to remove all the choices and keep the system commands. 
		/// Pass a populated list to display the choices on the list + the eventually generated system commands.</param>
		/// <param name="currentPage"></param>
		/// <param name="hasNextPage"></param>
		/// <param name="hasPrevPage"></param>
		/// <param name="showSkip"></param>
		/// <param name="showBack"></param>
		/// <param name="showCancel"></param>
		/// <returns></returns>
		public async Task ChangePage(Question question, string text, string followUp = null, List<TelegramChoice> defAnswers = null,
												int currentPage = 0, bool hasNextPage = false, bool hasPrevPage = false,
												bool showSkip = false, bool showBack = false, bool showCancel = false, bool? expectsCommand = null)
		{
			if (question == null) return;
			if (CurrentSurvey == null) CurrentSurvey = question.Survey;
			if (CurrentSurvey.IsActive == false) { log.Warn($"ChangePage question on non active survey. Survey: {CurrentSurvey.Id}, Completed: {CurrentSurvey.IsCompleted}, Cancelled: {CurrentSurvey.IsCancelled}"); }

			// if defAnswers is null it means that we have to display the same choices as before, except for the system commands that might have changed
			// so we take a list of the choices we had before, but the system commands
			List<TelegramChoice> prevChoices = new List<TelegramChoice>();
			if (defAnswers == null)
			{
				foreach (var a in question.DefaultAnswersList)
				{
					if (a.IsSystemChoice == false)
						prevChoices.Add(a);
				}
				// using except will include only one occurency for one instance
				//prevChoices = question.DefaultAnswersList.Except(TelegramChoice.SystemChoices).ToList();
				defAnswers = new List<TelegramChoice>();
				defAnswers.AddRange(prevChoices);
			}

			// back page, current page and next page should be placed on a new row at the end
			if (hasPrevPage)
				defAnswers.Add(new TelegramChoice(TelegramChoice._PrevPageAnswer) { Param = currentPage.ToString() });
			if (hasPrevPage || hasNextPage)
				defAnswers.Add(new TelegramChoice(TelegramChoice._CurrPageAnswer) { Param = currentPage.ToString() });
			if (hasNextPage)
				defAnswers.Add(new TelegramChoice(TelegramChoice._NextPageAnswer) { Param = currentPage.ToString() });

			// back, cancel and skip should be at the end because they will be placed on a new row
			if (showBack)
				defAnswers.Add(TelegramChoice.BackAnswer);
			if (showCancel)
				defAnswers.Add(TelegramChoice.CancelAnswer);

			if (question.IsMandatory == false && showSkip)
				defAnswers.Add(TelegramChoice.SkipAnswer);

			question.ClearDefaultAnswersList(false);
			question.AddDefaultAnswers(defAnswers);
			question.QuestionText = text ?? question.QuestionText;
			question.FollowUp = followUp ?? question.FollowUp;
			question.IsCompleted = false;
			question.ExpectsCommand = expectsCommand ?? question.ExpectsCommand;

			await CurrentSurvey.UpdateSurvey(true);
			return;
		}

		/// <summary>
		/// Prepares the passed question to display a followup message. If the question is completed, it will be marked as not completed.
		/// The newly generated question will have InternalId = question.InternalId + 1. If the question is reused, the InternalId will not be changed. 
		/// </summary>
		/// <param name="question">the question that must show a follow up message</param>
		/// <param name="followUp">The follow up text to display. Pass null to keep the previous one, pass empty string to remove it.</param>
		/// <param name="defAnswers">Pass null to keep the previous choices, changing the system commands only. Pass an empty list to remove all the choices and keep the system commands. 
		/// Pass a populated list to display the choices on the list + the eventually generated system commands.</param>
		public async Task<Question> FollowUp(Question question, string followUp, string newQuestionText = null, bool reuseQuestion = true,
												//bool pickOnlyDefAnswers = false,
												//FieldTypes type = FieldTypes.String,
												List<TelegramChoice>? defAnswers = null,
												//List<TelegramConstraint>? defConstraints = null,
												int currentPage = 0, bool hasNextPage = false, bool hasPrevPage = false,
												bool showSkip = false, bool showBack = false, bool showCancel = false, bool? expectsCommand = null)
		{
			if (question == null) return null;
			if (CurrentSurvey == null) CurrentSurvey = question.Survey;
			if (CurrentSurvey.IsActive == false) { log.Warn($"{TId}:{UsernameOrFirstName}. FollowUp question on non active survey. Survey: {CurrentSurvey.Id}, Completed: {CurrentSurvey.IsCompleted}, Cancelled: {CurrentSurvey.IsCancelled}"); }

			Question newQuestion;

			if (reuseQuestion == false)
			{
				// populate the existing answers or add the defaults
				var answers = new List<TelegramChoice>();
				if (defAnswers != null)
					answers.AddRange(defAnswers);
				else
					foreach (var a in question.DefaultAnswersList)
						if (a.IsSystemChoice == false)
							answers.Add(a);
				newQuestion = await AddQuestion(question.InternalId + 1, newQuestionText ?? question.QuestionText, followUp, question.IsMandatory, false, question.PickOnlyDefaultAnswers,
					(FieldTypes)question.FieldTypeId, answers, question.TelegramConstraints, currentPage, hasNextPage, hasPrevPage,
					showSkip, showBack, showCancel, expectsCommand ?? question.ExpectsCommand);
			}
			else
			{
				newQuestion = question;
				await ChangePage(newQuestion, newQuestionText, followUp, defAnswers, currentPage, hasNextPage, hasPrevPage, showSkip, showBack, showCancel, expectsCommand);
			}

			return newQuestion;
		}

		/// <summary> keeps track of the message showing the dashboard. it is reset when the Survey.TelegramMsgId is set to null 
		/// because it means that the dashboard was deleted</summary>
		int DashboardMsgId = 0;
		readonly object sync_init = new object();
		public bool IsInited { get; set; }
		/// <summary>
		/// inits this manager and sets its dashboard message id
		/// </summary>
		public void Init()
		{
			lock (sync_init)
			{
				if (IsInited) return;

				// try to find the message id from the last question shown ever
				var mostRecent = Question.GetMostRecent(TId);
				if (mostRecent != null)
					DashboardMsgId = mostRecent.Survey.TelegramMessageId ?? 0;

				IsInited = true;
			}
			return;
		}


		/// <summary>
		/// Creates a new survey taking the existing message id if any
		/// </summary>
		/// <returns>Returns the survey just created</returns>
		public async Task<Survey?> CreateNewSurvey()
		{
			CurrentSurvey = await Survey.AddSurvey(DashboardMsgId, ChatId);
			return CurrentSurvey;
		}

		/// <summary>
		/// Marks the current survey as completed and then sets it to null
		/// </summary>
		/// <returns></returns>
		public async Task CompleteSurvey()
		{
			if (CurrentSurvey == null) return;
			CurrentSurvey.IsCompleted = true;
			CurrentSurvey.IsActive = false;
			await CurrentSurvey.UpdateSurvey(false);

			//var answers = (from questions in CurrentSurvey.Questions
			//               select questions.LastAnswer).ToList();

			var tmpSrv = CurrentSurvey;

			await RaiseOnSurveyCompleted(new SurveyCompletedEventArgs() { Survey = tmpSrv, GivenAnswers = CurrentSurvey.LastAnswers });

			CurrentSurvey = null;
		}

		/// <summary>
		/// Cancels the surveys and sets it to null
		/// </summary>
		/// <returns></returns>
		public async Task<List<TelegramAnswer>?> CancelSurvey()
		{
			if (CurrentSurvey == null) return null;
			CurrentSurvey.IsCancelled = true;
			CurrentSurvey.IsActive = false;
			await CurrentSurvey.UpdateSurvey(false);

			var answers = (from questions in CurrentSurvey.Questions
						   select questions.LastAnswer).ToList();

			var tmpSrv = CurrentSurvey;
			CurrentSurvey = null;

			await RaiseOnSurveyCancelled(new SurveyCancelledEventArgs() { Survey = tmpSrv, GivenAnswers = answers, CurrentQuestion = tmpSrv.MostRecentQuestion });
			return answers;
		}

		/// <summary>
		/// Creates a InlineKeyboardMarkup based on the passed choices
		/// </summary>
		/// <param name="choices">The choices to add to the keyboard</param>
		/// <param name="maxButtPerRow">If zero, the number of buttons per row will be determined automatically</param>
		/// <returns>The InlineKeyboardMarkup based on the passed choices</returns>
		static InlineKeyboardMarkup? CreateInlineMarkupKeyboard(TelegramChoice[] choices, int maxButtPerRow = 0)
		{
			if (choices == null || choices.Length == 0) return null;

			// we seprate the system commands from the other commands, so we can create a
			// nice layout for the keyboard according to the effective number of choices 
			List<TelegramChoice> onlyChoices = new List<TelegramChoice>(); // this returns only one occurency of NewKeyboardLine as the instance is the same
			foreach (var c in choices)
				if (c.IsSystemChoice == false)
					onlyChoices.Add(c);
			List<TelegramChoice> onlyCommands = choices.Intersect(TelegramChoice.SystemChoices).ToList();

			int itemsPerRow = GetElementsPerRow(onlyChoices.Count);
			if (maxButtPerRow > 0 && itemsPerRow > maxButtPerRow)
				itemsPerRow = maxButtPerRow;
			List<List<InlineKeyboardButton>> rows = new List<List<InlineKeyboardButton>>();
			List<InlineKeyboardButton>? currentRow = new List<InlineKeyboardButton>();
			int numOfEl = 1;
			foreach (TelegramChoice a in onlyChoices)
			{
				if (a.Equals(TelegramChoice._NewKeyboardLine))
				{
					numOfEl = 1;
					rows.Add(currentRow);
					currentRow = new List<InlineKeyboardButton>();
					continue;
				}

				if (a.Label == null)
				{
					log.Error($"{a} has null label");
				}
				else
					currentRow.Add(new InlineKeyboardButton(a.Label) { CallbackData = a.ToJsonSpecial(), Url = a.IsUrl ? a.Value : null });

				if (numOfEl++ == itemsPerRow)
				{
					numOfEl = 1;
					rows.Add(currentRow);
					currentRow = new List<InlineKeyboardButton>();
				}
			}
			if (currentRow.Count != 0)
				rows.Add(currentRow);

			// now let's add the commands
			bool questionNavAdded = false;
			bool pageNavAdded = false;
			currentRow = null;
			foreach (TelegramChoice a in onlyCommands)
			{
				if (a.Equals(TelegramChoice._PayAnswer)) // pay must be the first
				{
					var payButton = new InlineKeyboardButton(a.Label) { Pay = true, CallbackData = a.ToJsonSpecial() };
					if (rows.Count == 0)
					{
						currentRow = currentRow ?? new();
						currentRow.Insert(0, payButton);
					}
					else
					{
						rows[0].Insert(0, payButton);
					}
				}
				else
				{
					// these will stay on a row
					if ((a.Equals(TelegramChoice._PrevPageAnswer) || a.Equals(TelegramChoice._CurrPageAnswer) || a.Equals(TelegramChoice._NextPageAnswer)) && pageNavAdded == false)
					{
						if (currentRow != null)
							rows.Add(currentRow);
						pageNavAdded = true;
						currentRow = new List<InlineKeyboardButton>();
					}
					// these will stay on a row
					else if ((a.Equals(TelegramChoice._BackAnswer) || a.Equals(TelegramChoice._SkipAnswer) || a.Equals(TelegramChoice._CancelAnswer)) && questionNavAdded == false)
					{
						if (currentRow != null)
							rows.Add(currentRow);
						questionNavAdded = true;
						currentRow = new List<InlineKeyboardButton>();
					}
					if (currentRow == null) // only if there is a command which is not in the IFs
						currentRow = new List<InlineKeyboardButton>();
					currentRow.Add(new InlineKeyboardButton(a.Label) { CallbackData = a.ToJsonSpecial() });
				}
			}
			if (currentRow != null)
				if (currentRow.Count == 0)
				{
				}
				else
					rows.Add(currentRow);

			InlineKeyboardMarkup rkm = new InlineKeyboardMarkup(rows);

			return rkm;
		}

		const int SPLITROW_BY_ITEMS = 3;
		const int MAX_NUMBER_OF_ROWS = 10;
		/// <summary>
		/// Returns the optimal number of elements per row
		/// </summary>
		/// <param name="totalElementCount"></param>
		/// <returns></returns>
		static int GetElementsPerRow(int totalElementCount)
		{
			int numOfRows = 0;
			for (int i = 0; totalElementCount > SPLITROW_BY_ITEMS && numOfRows < MAX_NUMBER_OF_ROWS; i++)
			{
				totalElementCount /= 2;
				numOfRows *= 2;
			}
			return totalElementCount < SPLITROW_BY_ITEMS ? SPLITROW_BY_ITEMS : totalElementCount;
		}
		#endregion

		#region sending

		readonly Semaphore semSend = new Semaphore(1, 1);
		/// <summary>
		/// Removes or updates (if removal could not be made because too old) a message and returns true if the operation completes.
		/// Returns false if there was an unexpected error from telegram
		/// </summary>
		/// <param name="originatingMsgId"></param>
		/// <returns></returns>
		private async Task<bool> RemoveMessageAsync(long originatingMsgId)
		{
			using var scope = serviceProvider.CreateScope();

			var tClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

			try
			{
				semSend.WaitOne();
				if (originatingMsgId == DashboardMsgId || CurrentSurvey == null)
					DashboardMsgId = 0; //this will force a new dashboard to be sent

				log.Debug($"{TId}:{UsernameOrFirstName}. deleting message: {originatingMsgId}, {CurrentSurvey}");
				await tClient.DeleteMessageAsync(ChatId, (int)originatingMsgId);
			}
			catch (Exception e)
			{
				if (e.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
					return true; //maybe already deleted?

				//log.Debug($"{TId}:{UsernameOrFirstName}. can't delete message {originatingMsgId}", e);
				try
				{
					await tClient.EditMessageTextAsync(ChatId, (int)originatingMsgId, MESSAGE_MANAGER_USE_NEW_DASHBOARD.Translate(Language));
				}
				catch (Exception ex)
				{
					//	log.Debug($"{TId}:{UsernameOrFirstName}. editing message: {originatingMsgId}, {CurrentSurvey}", ex);

					if (ex.Message.Contains("is not modified", StringComparison.OrdinalIgnoreCase))
						return false;

					//try to edit the caption, maybe the message is not a text message
					try
					{
						await tClient.EditMessageCaptionAsync(ChatId, (int)originatingMsgId, MESSAGE_MANAGER_USE_NEW_DASHBOARD.Translate(Language));
					}
					catch (Exception exx)
					{
						if (ex.Message.Contains("is not modified", StringComparison.OrdinalIgnoreCase))
							return false;

						log.Warn($"{this} - Could not delete, edit text and edit caption. {e.Message}\r\n{ex.Message}", exx);
						return false;
					}
				}
			}
			finally
			{
				semSend.Release();
			}
			return true;
		}

		/// <summary>
		/// If the question passed is currently shown, it is updated
		/// </summary>
		/// <param name="currQuestion"></param>
		/// <returns></returns>
		public async Task UpdateShownQuestion(Question currQuestion)
		{
			if (currQuestion == null) return;
			using var scope = serviceProvider.CreateScope();

			var tClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
			semSend.WaitOne();
			try
			{
				// we must make sure that the question we are about to update is really the one which was shown lastly
				var lastQuestionAsked = CurrentSurvey?.MostRecentQuestion ?? await Question.GetMostRecentAsync(TId);
				if (lastQuestionAsked == null)
				{
					log.Debug($"{this} last question asked is null");
					return;
				}

				if (currQuestion.InternalId == lastQuestionAsked.InternalId && currQuestion.SurveyId == lastQuestionAsked.SurveyId)
				{
					log.Debug($"{this}. really updating question {currQuestion}.");
					await doSendQuestion(tClient, this, currQuestion);
				}
				else
					log.Debug($"{this}. don't update shown question because, {currQuestion.InternalId} != {lastQuestionAsked.InternalId}");
			}
			catch (Exception e)
			{
				log.Error($"{this}. UpdateShownQuestion {currQuestion}", e);
			}
			finally
			{
				semSend.Release();
			}
		}
		/// <summary>
		/// Deletes the current question and send the previous one
		/// </summary>
		/// <returns></returns>
		public async Task<Message?> SendPreviousQuestion(int howMany = 1)
		{
			if (CurrentSurvey == null || howMany + 1 > CurrentSurvey.Questions.Count) return null;

			for (int i = 0; i < howMany; i++)
			{
				var toDel = CurrentSurvey.Questions[CurrentSurvey.Questions.Count - 1];
				CurrentSurvey.Questions.Remove(toDel);
				await toDel.Delete();
			}

			CurrentSurvey.MostRecentQuestion.IsCompleted = false;

			return await SendQuestion(CurrentSurvey.MostRecentQuestion);
		}
		/// <summary>
		/// Send a previously created question and creates an inline keyboard that accepts answers based on the default answers.
		/// </summary>
		/// <param name="question">The question to send</param>
		/// <returns></returns>
		public async Task<Message?> SendQuestion(Question question)
		{
			if (question == null)
			{
				log.Debug($"{this} question to send is null");
				return null;
			}

			semSend.WaitOne();

			if (CurrentMessage == null)
			{
				log.Debug($"{this} Current message is null, question: {question}");
				return null;
			}

			try
			{
				using var scope = serviceProvider.CreateScope();

				var tClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

				return await doSendQuestion(tClient, this, question);
			}
			catch (Exception e)
			{
				log.Error($"{this}. SendQuestion {question}", e);
				return null;
			}
			finally
			{
				semSend.Release();
			}
		}

		public async Task AnswerCurrentCallbackQueryAsync()
		{
			using var scope = serviceProvider.CreateScope();

			var tClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

			if (CurrentMessage?.IsCallbackQuery == true && CurrentMessage.CallbackQueryAnswered == false)
				try
				{
					string msg = CurrentMessage.CallbackQueryAnswer;
					CurrentMessage.ClearCallbackQueryMessage();
					CurrentMessage.CallbackQueryAnswered = true;
					await tClient.AnswerCallbackQueryAsync(CurrentMessage.Query.Id, msg, string.IsNullOrEmpty(msg) == false);
				}
				catch (Exception e) { log.Debug($"{TId}:{UsernameOrFirstName}. Exception while updating the callbackquery for {CurrentMessage}-{CurrentMessage.Query.Id}", e); }
		}

		/// <summary>
		/// Sends the question. Must be called from a synchronized context semSend.WaitOne()
		/// </summary>
		/// <param name="tClient"></param>
		/// <param name="mngr"></param>
		/// <param name="question"></param>
		/// <returns></returns>
		private static async Task<Message?> doSendQuestion(ITelegramBotClient tClient, MessageManager mngr, Question question)
		{
			log.Debug($"{mngr} - Send question, {question}");
			if (mngr == null)
			{
				log.Debug($"{mngr}. mngr is null");
				return null;
			}
			if (question == null)
			{
				log.Debug($"{mngr}. Question {question} is null");
				return null;
			}
			Survey surv = question.Survey;

			if (surv == null)
			{
				log.Debug($"{mngr}. Survey {surv} is null, taking question.survey {question.Survey}");
				return null;
			}

			await question.UpdateQuestion();
			if (surv.IsActive == false)
			{
				// might happen if called from UpdateShownQuestion
				log.Warn($"{mngr}. Sending question on non active survey. Survey: {surv.Id}, Completed: {surv.IsCompleted}, Cancelled: {surv.IsCancelled}. ");
			}


			// now let's update the choices setting the message id and question id to this message, so when 
			// the user answers, we know which message the callback comes from
			var DefaultAnswersList = question.DefaultAnswersList;
			if (DefaultAnswersList != null)
				foreach (var x in DefaultAnswersList)
				{
					x.QuestionId = question.Id;
					if (x.IsSystemChoice)
					{
						x.Label = x.RawLabel.Translate(mngr.Language);
					}
				}
			var keyboard = DefaultAnswersList == null ? null : CreateInlineMarkupKeyboard(DefaultAnswersList, question.MaxButtonsPerRow);

			string qText = $"<u>{question.QuestionText}</u>";
			if (string.IsNullOrWhiteSpace(question.FollowUp) == false)
			{
				if (question.FollowUpSeparator != null)
					qText = $"{qText}{Environment.NewLine}{question.FollowUpSeparator}";
				qText = $"{qText}{Environment.NewLine}{question.FollowUp}";
			}

			Message? sent = null;

			bool messageNotModified = false;

			if (string.IsNullOrWhiteSpace(question.ImageUrl) == false && mngr.DashboardMsgId != 0)
			{
				// otherwise we cant update this message with the new image
				try { await tClient.DeleteMessageAsync(mngr.ChatId, mngr.DashboardMsgId); } catch { }
				mngr.DashboardMsgId = 0;
			}

			if (mngr.DashboardMsgId != 0)
				try
				{
					sent = await tClient.EditMessageTextAsync(
									mngr.ChatId
									, mngr.DashboardMsgId
									, qText
									, parseMode: ParseMode.Html
									, replyMarkup: keyboard
									, disableWebPagePreview: question.DisableWebPagePreview);
				}
				catch (ApiRequestException ar)
				{
					if (ar.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))
					{
						messageNotModified = true;
						//Debug.WriteLine("message is not modified");
					}
					else if (ar.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
					{
						//nothing, we will send a new one. but it is a strange case. maybe the user clicked very fast and we receive the same callback and this message was deleted on a previous call
					}
					else if (ar.Message.Contains("no text in the message", StringComparison.OrdinalIgnoreCase))
					{
						// this case when trying to edit a photo
						try
						{
							await tClient.DeleteMessageAsync(mngr.ChatId, mngr.DashboardMsgId);
						}
						catch { }
					}
					else
						log.Error($"{mngr} - while editing msg.Id: '{mngr.DashboardMsgId}'. sending a new one. qText: [{qText}] - replyMarkup: {JsonConvert.SerializeObject(keyboard)}", ar);
				}
				catch (Exception ex)
				{
					// that's strange
					log.Error($"{mngr} could not update {mngr.DashboardMsgId}. qText: [{qText}] - replyMarkup: {JsonConvert.SerializeObject(keyboard)} - Sending a new one", ex);
				}


			if (sent == null && messageNotModified == false)
			{
				// if the message couldn't be updated, it might because it was deleted or too long time has passed.
				// in this case we send a new one

				if (string.IsNullOrWhiteSpace(question.ImageUrl) == false)
					try
					{
						sent = await tClient.SendPhotoAsync(mngr.ChatId
							, new InputOnlineFile(question.ImageUrl)
							, qText
							, replyMarkup: keyboard
							, parseMode: ParseMode.Html
							);
					}
					catch (Exception e)
					{
						log.Error($"{mngr} sending photo. qText: [{qText}], imageUrl: [{question.ImageUrl}], keyBoard: {keyboard?.SerializeIgnoreAndPopulate()} ", e);
					}

				//backup on plain text
				if (sent == null)
					try
					{
						sent = await tClient.SendTextMessageAsync(
								mngr.ChatId
								, qText
								, replyMarkup: keyboard
								, parseMode: ParseMode.Html
								, disableWebPagePreview: question.DisableWebPagePreview);

					}
					catch (Exception exx)
					{
						log.Fatal($"{mngr} - sending message. qText: [{qText}], imageUrl: [{question.ImageUrl}], keyBoard: {keyboard?.SerializeIgnoreAndPopulate()} ", exx);
						sent = null;
						// this is bad!!
					}

				if (sent != null)
				{
					mngr.recentMessageSent = 0;
					log.Debug($"{mngr} sent.Id: '{sent.MessageId}' - survid: {surv.Id}. q: {question}");
				}
			}

			if (sent == null)
			{
				if (messageNotModified == false)
					log.Error($"{mngr} sent message is null. Maybe messageNotModified? {messageNotModified}");
				return null;
			}

			mngr.DashboardMsgId = sent.MessageId;

			if (surv.TelegramMessageId != mngr.DashboardMsgId)
			{
				log.Debug($"{mngr} Survey {surv.Id} changed message from {surv.TelegramMessageId} to {mngr.DashboardMsgId}");
				surv.TelegramMessageId = mngr.DashboardMsgId;
				await surv.UpdateSurvey(false);
			}
			return sent;
		}

		/// <summary>
		/// Sends the user the list of the default commands previously set
		/// </summary>
		/// <param name="caption"></param>
		/// <param name="followUpText"></param>
		/// <param name="currentQuestion">Whether the commands should be sent within the current question</param>
		/// <returns></returns>
		public async Task<Question?> GetQuestionDefaultCommands(string caption, string followUpText = null, bool currentQuestion = false, int maxButtonsPerRow = 0, Type callbackHandler = null)
		{
			Question q;
			if (currentQuestion == false || CurrentSurvey == null || CurrentSurvey.IsActive == false)
			{
				await CreateNewSurvey();
				q = await CreateCommandsQuestion(0, caption, followUpText);
				q.MaxButtonsPerRow = maxButtonsPerRow;
			}
			else
			{
				q = CurrentSurvey.MostRecentQuestion;
				if (maxButtonsPerRow != 0) q.MaxButtonsPerRow = maxButtonsPerRow;
				await ChangePage(q, caption, followUp: followUpText, defAnswers: CreateChoicesFromCommands(), expectsCommand: true);
			}

			if (q == null)
			{
				log.Debug($"{TId}:{UsernameOrFirstName}. Opss... question is null");
				return null;
			}
			q.CallbackHandler = callbackHandler;
			return q;
		}


		/// <summary>
		/// Sends a message to the chat with the user
		/// </summary>
		/// <param name="message">The message to send, html is accepted</param>
		/// <returns></returns>
		public async Task<Message?> SendMessage(string message, IReplyMarkup? markup = null, bool? disableWebPagePreview = false)
		{
			return await SendMessage(message, ChatId, markup, disableWebPagePreview);
		}
		private async Task<Message?> SendMessage(string message, long tid, IReplyMarkup? markup = null, bool? disableWebPagePreview = false)
		{
			try
			{
				using var scope = serviceProvider.CreateScope();

				var tClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

				semSend.WaitOne();
				var m = await tClient.SendTextMessageAsync(new ChatId(tid), message, parseMode: ParseMode.Html, replyMarkup: markup, disableWebPagePreview: disableWebPagePreview);
				recentMessageSent++;
				return m;
			}
			catch (Exception ex)
			{
				log.Debug($"{TId}:{UsernameOrFirstName}. Error sending message on ChatId {tid}", ex);
			}
			finally
			{
				semSend.Release();
			}
			return null;
		}
		public async Task<Message?> SendPhoto(string caption, Stream stream)
		{
			try
			{
				using var scope = serviceProvider.CreateScope();

				var tClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

				semSend.WaitOne();
				var m = await tClient.SendPhotoAsync(new ChatId(ChatId), new InputOnlineFile(stream), caption);
				recentMessageSent++;
				return m;
			}
			catch (Exception ex)
			{
				log.Debug($"{TId}:{UsernameOrFirstName}. Error sending message on ChatId {ChatId}", ex);
			}
			finally
			{
				semSend.Release();
			}
			return null;
		}

		/// <summary>
		/// If the CurrentSurvey.TelegramMessageId is not null, the corresponding UI message is deleted
		/// and it is set to null.
		/// This is used to force the recreation of a menu
		/// </summary>
		/// <returns></returns>
		//async Task setTelegramMsgIdNullAndDeletePrevMsg()
		//{
		//	if (CurrentSurvey == null || CurrentSurvey.TelegramMessageId == null) return;
		//	try
		//	{
		//		await tClient.DeleteMessageAsync(ChatId, (int)CurrentSurvey.TelegramMessageId);
		//	}
		//	catch { }
		//	CurrentSurvey.TelegramMessageId = null; // this will force further SendQuestion to recreate the main message
		//	await CurrentSurvey.UpdateSurvey(false);
		//}
		#endregion

		public override string ToString()
		{
			return $"{TId}:{UsernameOrFirstName}";
		}

	}
}
