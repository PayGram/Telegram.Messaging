using log4net;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Messaging.CallbackHandlers;
using Telegram.Messaging.Db;
using Telegram.Messaging.Types;
using Utilities.String.Json.Extentions;

namespace Telegram.Messaging.Messaging
{
    public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e);

    public class MessageManager
    {
        static readonly ILog log = LogManager.GetLogger(typeof(MessageManager));

        /// <summary>
        /// The name of the bot
        /// </summary>
        public string BotName { get; private set; }
        /// <summary>
        /// The available valid commands for this bot, name-label
        /// </summary>
        public Dictionary<string, string> ValidCommands { get; private set; }
        /// <summary>
        /// The current message that we have just received from the user
        /// </summary>
        public TelegramMessage CurrentMessage { get; private set; }
        /// <summary>
        /// The current flow of questions and answers with the client
        /// </summary>
        public Survey CurrentSurvey { get; private set; }
        public string BotToken { get; private set; }
        readonly ITelegramBotClient tClient;
        long _chatId;
        /// <summary>
        /// The Chat identifier where this Manager is addressing the messages. It has always precedence on the CurrentMessage.Chat.Id
        /// when it is specified. If not specified, this Id will be assigned after a message is processed
        /// </summary>
        public long ChatId { get => _chatId != 0 ? _chatId : CurrentMessage?.ChatId ?? 0; set => _chatId = value; }

        /// <summary>
        /// The username or the first name or an empty string if they are not available of the user with whom we are interacting
        /// </summary>
        public string UsernameOrFirstName => CurrentMessage?.From?.Username ?? CurrentMessage?.From?.FirstName ?? "";

        readonly long _tid;

        /// <summary>
        /// The telegram ID of the user with whom we are interacting or 0 if not found
        /// </summary>
        public long TId => _tid != 0 ? _tid : CurrentMessage?.From?.Id ?? 0;

        public event EventHandler<CommandReceivedEventArgs> OnCommand;
        public event EventHandler<QuestionAnsweredEventArgs> OnQuestionAnswered;
        public event EventHandler<QuestionChangedEventArgs> OnQuestionChanged;
        public event EventHandler<ChangePageEventArgs> OnPageChanged;
        public event EventHandler<SurveyCancelledEventArgs> OnSurveyCancelled;

        public event EventHandler<SurveyCompletedEventArgs> OnSurveyCompleted;
        public event EventHandler<PayReceivedEventArgs> OnPayPressed;
        public event EventHandler<InvalidInteractionEventArgs> OnInvalidInteraction;
        public event EventHandler<DiceRolledEventArgs> OnDiceRolled;

        public event AsyncEventHandler<CommandReceivedEventArgs> OnCommandAsync;
        public event AsyncEventHandler<QuestionAnsweredEventArgs> OnQuestionAnsweredAsync;
        public event AsyncEventHandler<QuestionChangedEventArgs> OnQuestionChangedAsync;
        public event AsyncEventHandler<ChangePageEventArgs> OnPageChangedAsync;
        public event AsyncEventHandler<SurveyCancelledEventArgs> OnSurveyCancelledAsync;
        public event AsyncEventHandler<SurveyCompletedEventArgs> OnSurveyCompletedAsync;
        public event AsyncEventHandler<PayReceivedEventArgs> OnPayPressedAsync;
        public event AsyncEventHandler<InvalidInteractionEventArgs> OnInvalidInteractionAsync;
        public event AsyncEventHandler<DiceRolledEventArgs> OnDiceRolledAsync;


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
        public MessageManager(string botName, string botToken, ITelegramBotClient client, long chatId = 0, long userId = 0)
        {
            ValidCommands = new Dictionary<string, string>();
            ChatId = chatId;
            BotName = botName;
            BotToken = botToken;
            tClient = client ?? new TelegramBotClient(botToken);
            _tid = userId;
            setEventsCallback();
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
                var callbackHandler = Activator.CreateInstance(key) as QuestionAnswerCallbackHandler;
                callbackHandler.Manager = this;
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

            string skey = forAction.Method.DeclaringType.ToString();
            IQuestionAnswerCallbackHandler target = null;
            AsyncEventHandler<MessagingEventArgs> toret = null;
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
                    // so next time we will use it
                    target = (IQuestionAnswerCallbackHandler)forAction.Target;
                    target.Manager = this;
                    callBackhandlers.Add(skey, target);
                    toret = forAction;
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
        async Task RaiseOnDiceRolled(DiceRolledEventArgs e)
        {
            e.Manager = this;
            List<Task> tasks = new List<Task>();
            if (OnDiceRolled != null)
                Array.ForEach(OnDiceRolled.GetInvocationList(), (d) => configureAndAddTask(tasks, d, e));
            if (OnDiceRolledAsync != null)
                Array.ForEach(OnDiceRolledAsync.GetInvocationList(), (d) => configureAndAddTask(tasks, d, e));
            if (e?.OriginatingQuestion?.CallbackHandler != null)
            {
                var callbackHandler = GetHandler(e.OriginatingQuestion.CallbackHandler) as QuestionAnswerCallbackHandler;
                var t = callbackHandler.OnDiceRolledAsync(this, e);
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
                bool b = m.WaitOne(1);
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
        public async Task<TelegramMessage> ProcessUpdate(Update upd)
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
        public async Task<TelegramMessage> ProcessUpdate(Message message)
        {
            CurrentMessage = new TelegramMessage(message, BotName, ValidCommands.Keys.ToArray());
            return await ProcessCurrentMessage();
        }

        /// <summary>
        /// Creates a list of Choices pickable by the user based on the commands
        /// </summary>
        /// <returns></returns>
        private List<TelegramChoice> CreateChoicesFromCommands()
        {
            List<TelegramChoice> choices = new List<TelegramChoice>();
            foreach (string key in ValidCommands.Keys)
                choices.Add(new TelegramChoice(ValidCommands[key], key));
            return choices;
        }

        /// <summary>
        /// Process an incoming telegram CallbackQuery and returns a TelegramMessage which tracks the current 
        /// flow of communication with the user
        /// </summary>
        /// <param name="message">The CallbackQuery coming from telgram</param>
        /// <returns></returns>
        public async Task<TelegramMessage> ProcessUpdate(CallbackQuery message)
        {
            CurrentMessage = new TelegramMessage(message, BotName, ValidCommands.Keys.ToArray());
            return await ProcessCurrentMessage();
        }

        /// <summary>
        /// Process the command previously parsed and loaded
        /// </summary>
        /// <returns>The associated TelegramMessage or null</returns>
        private async Task<TelegramMessage> ProcessCurrentMessage()
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
                // the question that was supposedly answered
                Question mostRecent = CurrentSurvey?.MostRecentQuestion;

                TelegramChoice pickedChoice = CurrentMessage.PickedChoice;
                long originatingMsgId = CurrentMessage.OriginatingMessageId != 0 ? CurrentMessage.OriginatingMessageId : DashboardMsgId;// CurrentSurvey?.TelegramMessageId ;
                long originatingQuestId = pickedChoice?.QuestionId ?? mostRecent?.Id ?? 0;

                // let's see the previous question, maybe we can get the messageId that is still displaying a menu, in this case we remove it
                if (mostRecent == null && (/*originatingMsgId == 0 ||*/ originatingQuestId == 0))
                {
                    var mostRecentExpired = await Question.GetMostRecentAsync(TId);
                    if (mostRecentExpired != null)
                    {
                        //if (originatingMsgId == 0)
                        //	originatingMsgId = mostRecentExpired.Survey.TelegramMessageId ?? 0;
                        if (originatingQuestId == 0)
                            originatingQuestId = mostRecentExpired.Id;
                    }
                }

                // pickedChoice will be different from null if the user pressed an inline button
                if (pickedChoice != null)
                {
                    // TelegramChoice.MessageId has been removed from the serialization of the TelegramChoice, so we must update it
                    pickedChoice.MessageId = originatingMsgId;
                    if (pickedChoice.MessageId == 0)
                        log.Debug($"{TId}:{UsernameOrFirstName}. We could not find the originating message id for question {pickedChoice.QuestionId}. Maybe this survey was removed from db?");
                }


                bool currentSurveyIsNull = CurrentSurvey == null;
                bool originatingQuestIdMismatch = mostRecent != null && mostRecent.Id != originatingQuestId;
                bool originatingMsgIdMismatch = CurrentSurvey != null && CurrentSurvey.TelegramMessageId != originatingMsgId;
                bool dashboardScrolledUp = CurrentMessage.IsCallbackQuery == false || recentMessageSent;


                // if the following happens, we must delete the message, it's too old or it scrolled up or it mismatch with the current situation
                if (originatingMsgId != 0 && (currentSurveyIsNull || dashboardScrolledUp || originatingQuestIdMismatch || originatingMsgIdMismatch))
                {
                    await RemoveMessageAsync(originatingMsgId);
                }

                // something did not go as expected
                if (currentSurveyIsNull || originatingQuestIdMismatch || originatingMsgIdMismatch)
                {
                    log.Debug($"{this}. Invalid answer given or expired survey. Raise invalid interaction and then process eventual commands");
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
                TelegramAnswer answer = null;
                if (mostRecent != null && pickedChoice != null)
                    answer = new TelegramAnswer(mostRecent, pickedChoice);
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
            if (CurrentMessage.IsDice)
            {
                await RaiseOnDiceRolled(new DiceRolledEventArgs() { TelegramMessage = CurrentMessage, Value = CurrentMessage.Message.Dice.Value, OriginatingQuestion = mostRecent });
            }
            else
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
            bool HitCancel = TelegramChoice.CancelAnswer.Equals(answer.PickedChoice);
            bool IsSkept = TelegramChoice.SkipAnswer.Equals(answer.PickedChoice);
            bool HitBack = CurrentSurvey.Questions.Count > 1 && TelegramChoice.BackAnswer.Equals(answer.PickedChoice);

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
                else if (TelegramChoice.PayAnswer.Equals(answer.PickedChoice))
                {
                    mostRecent.IsCompleted = true;
                    await RaiseOnPayPressed(new PayReceivedEventArgs() { CurrentQuestion = mostRecent });
                }
                else if (TelegramChoice.CurrPageAnswer.Equals(answer.PickedChoice))
                {
                    var currPage = pickedChoice != null ? int.Parse(pickedChoice.Param) : 0;
                    await RaiseOnPageChanged(new ChangePageEventArgs() { CurrentQuestion = mostRecent, CurrentPage = currPage, RequestedPage = currPage });
                }
                else if (TelegramChoice.NextPageAnswer.Equals(answer.PickedChoice))
                {
                    var currPage = pickedChoice != null ? int.Parse(pickedChoice.Param) : 0;
                    await RaiseOnPageChanged(new ChangePageEventArgs() { CurrentQuestion = mostRecent, CurrentPage = currPage, RequestedPage = currPage + 1 });
                }
                else if (TelegramChoice.PrevPageAnswer.Equals(answer.PickedChoice))
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

        private async Task processCommand(Question mostRecent, TelegramAnswer answer)
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
        public async Task<Question> AddQuestion(int qid, string text, string followUp = null,
                                                bool isMandatory = false,
                                                bool isPay = false,
                                                bool pickOnlyDefAnswers = false,
                                                FieldTypes type = FieldTypes.String,
                                                List<TelegramChoice> defAnswers = null,
                                                List<TelegramConstraint> defConstraints = null,
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
                defAnswers.Add(new TelegramChoice(TelegramChoice.PrevPageAnswer) { Param = currentPage.ToString() });
            if (hasPrevPage || hasNextPage)
                defAnswers.Add(new TelegramChoice(TelegramChoice.CurrPageAnswer) { Param = currentPage.ToString() });
            if (hasNextPage)
                defAnswers.Add(new TelegramChoice(TelegramChoice.NextPageAnswer) { Param = currentPage.ToString() });

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
            if (CurrentSurvey.IsActive == false) { log.Debug($"ChangePage question on non active survey. Survey: {CurrentSurvey.Id}, Completed: {CurrentSurvey.IsCompleted}, Cancelled: {CurrentSurvey.IsCancelled}"); }

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
                defAnswers.Add(new TelegramChoice(TelegramChoice.PrevPageAnswer) { Param = currentPage.ToString() });
            if (hasPrevPage || hasNextPage)
                defAnswers.Add(new TelegramChoice(TelegramChoice.CurrPageAnswer) { Param = currentPage.ToString() });
            if (hasNextPage)
                defAnswers.Add(new TelegramChoice(TelegramChoice.NextPageAnswer) { Param = currentPage.ToString() });

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
        /// <param name="text">The question text to display. Pass null to keep the previous one, pass empty string to remove it.</param>
        /// <param name="followUp">The follow up text to display. Pass null to keep the previous one, pass empty string to remove it.</param>
        /// <param name="defAnswers">Pass null to keep the previous choices, changing the system commands only. Pass an empty list to remove all the choices and keep the system commands. 
        /// Pass a populated list to display the choices on the list + the eventually generated system commands.</param>
        public async Task<Question> FollowUp(Question question, string followUp, string newQuestionText = null, bool reuseQuestion = true, List<TelegramChoice> defAnswers = null,
                                                int currentPage = 0, bool hasNextPage = false, bool hasPrevPage = false,
                                                bool showSkip = false, bool showBack = false, bool showCancel = false, bool? expectsCommand = null)
        {
            if (question == null) return null;
            if (CurrentSurvey == null) CurrentSurvey = question.Survey;
            if (CurrentSurvey.IsActive == false) { log.Debug($"{TId}:{UsernameOrFirstName}. FollowUp question on non active survey. Survey: {CurrentSurvey.Id}, Completed: {CurrentSurvey.IsCompleted}, Cancelled: {CurrentSurvey.IsCancelled}"); }

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
        public async Task<List<TelegramAnswer>?> CompleteSurvey()
        {
            if (CurrentSurvey == null) return null;
            CurrentSurvey.IsCompleted = true;
            CurrentSurvey.IsActive = false;
            await CurrentSurvey.UpdateSurvey(false);

            var answers = (from questions in CurrentSurvey.Questions
                           select questions.LastAnswer).ToList();

            var tmpSrv = CurrentSurvey;

            await RaiseOnSurveyCompleted(new SurveyCompletedEventArgs() { Survey = tmpSrv, GivenAnswers = answers });

            CurrentSurvey = null;
            return answers;
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
            List<TelegramChoice> onlyChoices = new List<TelegramChoice>();//choices.Except(TelegramChoice.SystemChoices).ToList(); // this returns only one occurency of NewKeyboardLine as the instance is the same
            foreach (var c in choices)
                if (c.IsSystemChoice == false)
                    onlyChoices.Add(c);
            List<TelegramChoice> onlyCommands = choices.Intersect(TelegramChoice.SystemChoices).ToList();

            int itemsPerRow = GetElementsPerRow(onlyChoices.Count);
            if (maxButtPerRow > 0 && itemsPerRow > maxButtPerRow /*&& choices.Contains(TelegramChoice.NewKeyboardLine) == false*/)
                itemsPerRow = maxButtPerRow;
            List<List<InlineKeyboardButton>> rows = new List<List<InlineKeyboardButton>>();
            List<InlineKeyboardButton> currentRow = new List<InlineKeyboardButton>();
            int numOfEl = 1;
            foreach (TelegramChoice a in onlyChoices)
            {
                if (a.Equals(TelegramChoice.NewKeyboardLine))
                {
                    numOfEl = 1;
                    rows.Add(currentRow);
                    currentRow = new List<InlineKeyboardButton>();
                    continue;
                }

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
                if (a.Equals(TelegramChoice.PayAnswer)) // pay must be the first
                {
                    if (rows.Count == 0) currentRow = new List<InlineKeyboardButton>();
                    else currentRow = rows[0];
                    currentRow.Insert(0, new InlineKeyboardButton(a.Label) { Pay = true, CallbackData = a.ToJsonSpecial() });
                    currentRow = null;
                }
                // these will stay on a row
                else if ((a.Equals(TelegramChoice.PrevPageAnswer) || a.Equals(TelegramChoice.CurrPageAnswer) || a.Equals(TelegramChoice.NextPageAnswer)) && pageNavAdded == false)
                {
                    pageNavAdded = true;
                    currentRow = new List<InlineKeyboardButton>();
                }
                // these will stay on a row
                else if ((a.Equals(TelegramChoice.BackAnswer) || a.Equals(TelegramChoice.SkipAnswer) || a.Equals(TelegramChoice.CancelAnswer)) && questionNavAdded == false)
                {
                    questionNavAdded = true;
                    currentRow = new List<InlineKeyboardButton>();
                }
                if (currentRow == null) // only if there is a command which is not in the IFs
                    currentRow = new List<InlineKeyboardButton>();
                currentRow.Add(new InlineKeyboardButton(a.Label) { CallbackData = a.ToJsonSpecial() });
            }
            if (currentRow != null && currentRow.Count != 0)
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

        private async Task RemoveMessageAsync(long originatingMsgId)
        {
            try
            {
                semSend.WaitOne();
                recentMessageSent = false;
                DashboardMsgId = 0;

#if DEBUG_
						log.Debug($"{TId}:{UsernameOrFirstName}. editing message: {originatingMsgId}, {originatingQuestId}, {mostRecent}, {CurrentSurvey}");
						//let's delete the old message
						await tClient.EditMessageTextAsync(new ChatId(ChatId), (int)originatingMsgId, "Message no longer valid, choose an option from the bot.");
#else
                log.Debug($"{TId}:{UsernameOrFirstName}. deleting message: {originatingMsgId}, {CurrentSurvey}");
                await tClient.DeleteMessageAsync(ChatId, (int)originatingMsgId);
#endif
            }
            catch (Exception e)
            {
#if DEBUG_
						log.Debug($"{TId}:{UsernameOrFirstName}. can't edit invalid message {originatingMsgId}", e);
						try
						{
							await tClient.DeleteMessageAsync(new ChatId(ChatId), (int)originatingMsgId);
						}
						catch (Exception) { log.Debug($"{TId}:{UsernameOrFirstName}. can't delete invalid message {originatingMsgId}"); }
#else
                log.Debug($"{TId}:{UsernameOrFirstName}. can't delete message {originatingMsgId}", e);
                try
                {
                    await tClient.EditMessageTextAsync(new ChatId(ChatId), (int)originatingMsgId, "Please use the new dashboard below.");
                }
                catch (Exception ex)
                {
                    log.Debug($"{TId}:{UsernameOrFirstName}. editing message: {originatingMsgId}, {CurrentSurvey}", ex);
                }
#endif
            }
            finally
            {
                semSend.Release();
            }
        }

        /// <summary>
        /// If the question passed is currently shown, it is updated
        /// </summary>
        /// <param name="currQuestion"></param>
        /// <returns></returns>
        public async Task UpdateShownQuestion(Question currQuestion)
        {
            if (currQuestion == null) return;

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
                    await doSendQuestion(tClient, this, currQuestion, true, false);
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

            // this is needed by Telegram to close  properly the communication flow
            // at this time all the events should have been handled
            if (CurrentMessage?.IsCallbackQuery == true)
                try
                {
                    await tClient.AnswerCallbackQueryAsync(CurrentMessage.Query.Id);
                }
                catch (Exception) { log.Debug($"{TId}:{UsernameOrFirstName}. Exception while updating the callbackquery for {CurrentMessage}-{CurrentMessage.Query.Id}"); }

            try
            {
                return await doSendQuestion(tClient, this, question, false, CurrentMessage.IsCallbackQuery);
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

        /// <summary>
        /// Sends the question. Must be called from a synchronized context semSend.WaitOne()
        /// </summary>
        /// <param name="tClient"></param>
        /// <param name="mngr"></param>
        /// <param name="question"></param>
        /// <param name="updating"></param>
        /// <param name="isCallbackQuery"></param>
        /// <returns></returns>
        private static async Task<Message?> doSendQuestion(ITelegramBotClient tClient, MessageManager mngr, Question question, bool updating, bool isCallbackQuery)
        {
            log.Debug($"Send question {mngr}, updating: {updating}, iscallback: {isCallbackQuery}, {question}");
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
                // might happen if called from updatingshownquestion
                log.Debug($"{mngr}. Sending question on non active survey. Updating: {updating}, Survey: {surv.Id}, Completed: {surv.IsCompleted}, Cancelled: {surv.IsCancelled}. ");
            }

            Message sent = null;

            if (mngr.DashboardMsgId == 0)
            {
                try
                {
                    mngr.recentMessageSent = false;
                    sent = await tClient.SendTextMessageAsync(
                                mngr.ChatId
                                , "hold on.. monkeys are loading bananas..");
                }
                catch (Exception ex)
                {
                    log.Debug($"{mngr}. Error sending the monkeys loading message.", ex);
                    sent = null;
                }
            }

            if (sent == null && mngr.DashboardMsgId == 0)
            {
                log.Debug($"{mngr} sent message is null and surv.TelegramMessageId is null. {surv}");
                return null;
            }
            mngr.DashboardMsgId = sent?.MessageId ?? mngr.DashboardMsgId;

            //long updateMessageId = sent?.MessageId ?? surv.TelegramMessageId ?? 0;
            //if (updateMessageId == 0)
            //	updateMessageId = mngr.DashboardMsgId;

            // now let's update the choices setting the message id and question id to this message, so when 
            // the user answers, we know which message the callback comes from
            var DefaultAnswersList = question.DefaultAnswersList;
            if (DefaultAnswersList != null)
                foreach (var x in DefaultAnswersList)
                {
                    x.MessageId = mngr.DashboardMsgId;
                    x.QuestionId = question.Id;
                }
            InlineKeyboardMarkup keyboard = CreateInlineMarkupKeyboard(DefaultAnswersList, question.MaxButtonsPerRow);

            string qText = $"<u>{question.QuestionText}</u>";
            if (string.IsNullOrWhiteSpace(question.FollowUp) == false)
            {
                if (question.FollowUpSeparator != null)
                    qText = $"{qText}{Environment.NewLine}{question.FollowUpSeparator}";
                qText = $"{qText}{Environment.NewLine}{question.FollowUp}";
            }

            bool messageNotModified = false;
            try
            {
                sent = await tClient.EditMessageTextAsync(
                                mngr.ChatId
                                , (int)mngr.DashboardMsgId
                                , qText
                                , parseMode: ParseMode.Html
                                , replyMarkup: keyboard);
            }
            catch (ApiRequestException ar)
            {
                if (ar.Message.Contains("message is not modified"))
                    messageNotModified = true;
                else
                    log.Debug($"{mngr}", ar);
            }
            catch (Exception ex)
            {
                // that's strange
                log.Warn($"{mngr} could not update {mngr.DashboardMsgId}. Sending a new one", ex);
                // if the message couldn't be updated, it might because it was deleted or too long time has passed.
                // in this case we send a new one
                try
                {
                    sent = await tClient.SendTextMessageAsync(
                            mngr.ChatId
                            , qText
                            , replyMarkup: keyboard
                            , parseMode: ParseMode.Html);
                    mngr.recentMessageSent = false;
                }
                catch (Exception exx)
                {
                    log.Debug($"{mngr}. error sending a new one..", exx);
                    sent = null;
                }
            }

            if (sent == null)
            {
                log.Debug($"{mngr} sent message is null. Maybe messageNotModified? {messageNotModified}");
                return null;
            }

            mngr.DashboardMsgId = sent.MessageId;

            if (surv.TelegramMessageId != mngr.DashboardMsgId)
            {
                log.Debug($"Survey {surv.Id} changed message from {surv.TelegramMessageId} to {mngr.DashboardMsgId}");
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

        /// <summary>  Access to this variable must be synchronized with semSend</summary>
        bool recentMessageSent = false;

        /// <summary>
        /// Sends a message to the chat with the user
        /// </summary>
        /// <param name="message">The message to send, html is accepted</param>
        /// <param name="removeMenu">If set to true, removes any system menu shown to the user after the message has been sen</param>
        /// <returns></returns>
        public async Task<Message?> SendMessage(string message, IReplyMarkup markup = null)
        {
            try
            {
                semSend.WaitOne();
                var m = await tClient.SendTextMessageAsync(new ChatId(ChatId), message, parseMode: ParseMode.Html, replyMarkup: markup);
                recentMessageSent = true;
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

        public async Task<Message?> SendPhoto(string caption, Stream stream)
        {
            try
            {
                semSend.WaitOne();
                var m = await tClient.SendPhotoAsync(new ChatId(ChatId), new InputOnlineFile(stream), caption);
                recentMessageSent = true;
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
