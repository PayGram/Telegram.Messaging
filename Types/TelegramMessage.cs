using log4net;
using Telegram.Bot.Types;

namespace Telegram.Messaging.Types
{
	public class TelegramMessage
	{
		ILog log = LogManager.GetLogger(typeof(TelegramMessage));
		readonly string[] AvailableCommands;
		public CallbackQuery Query { get; set; }
		public string BotName { get; internal set; }
		public bool IsCallbackQuery { get => Query != null; }
		public TelegramCommand Command { get; set; }
		public TelegramChoice PickedChoice { get; set; }

		public Message Message { get; set; }
		public User From { get => Query?.From ?? Message?.From; }
		public string OriginalInputText { get => (Query?.Data ?? Message?.Text) ?? (Message?.Dice?.Value)?.ToString(); }
		public string Username { get { return From?.Username; } }
		public long ChatId { get => Query?.Message?.Chat?.Id ?? Message?.Chat?.Id ?? 0; }
		public bool IsDice { get => Message?.Dice != null; }
		/// <summary>
		/// if the message represents a callback, gets the message id that originated the callback, otherwise 0
		/// </summary>
		public long OriginatingMessageId => Query?.Message?.MessageId ?? 0;

		public bool CallbackQueryAnswered { get; set; }

		internal TelegramMessage(Message message, string botName, string[] availableCommands)
		{
			AvailableCommands = availableCommands;
			Message = message;
			BotName = botName;
			Command = new TelegramCommand();
			ParseCommand();
		}

		internal TelegramMessage(CallbackQuery message, string botName, string[] availableCommands)
		{
			AvailableCommands = availableCommands;
			Query = message;
			BotName = botName;
			Command = new TelegramCommand();
			ParseCommand();
		}

		void ParseCommand()
		{
			string inputText = OriginalInputText;
			if (string.IsNullOrWhiteSpace(inputText)) return;

			PickedChoice = TelegramChoice.FromJsonSpecial(inputText);
			if (PickedChoice != null)
				inputText = PickedChoice.Value;

			if (inputText[0] == '/')
				inputText = inputText.Substring(1);

			if (string.IsNullOrWhiteSpace(inputText)) return;

			string[] parms = inputText.Split(new char[] { TelegramCommand.PARAMS_SPACE_SEP }, StringSplitOptions.RemoveEmptyEntries);
			if (parms.Length == 0) return;

			if (parms.Length > 2)
			{
				// this should not happen
				log.Debug($"Found command with multiple parameters separated by space {string.Join(",", parms)}");
				//return;
			}

			//parms[0]  is this is the command name
			for (int i = 1; i < parms.Length; i++)
			{
				var unesc = TelegramCommand.UnEscapeCommandValue(parms[i]); // these are the rest of the parameters separated in an old fashion way name2=val1&name2=value2
				if (unesc == parms[i]) // this parameter was in plain text
					Command.AddParameter(i.ToString(), unesc);
				else
					Command.AddParameters(unesc);
			}

			string command = PickedChoice?.Value ?? parms[0];
			// it must end with the bot name, otherwise, if we only do the indexof, we might accept commands that contain our botname, 
			// ie. if our botname is mariomario, we would accept @mariomario123 which is not us.
			int iOm = command.IndexOf($"@{BotName}");
			if ((iOm != -1 && command.EndsWith($"@{BotName}")) || iOm == -1)
			{
				if (iOm != -1)
					command = command.Substring(0, iOm);
				Command.IsValidCommand = true;
			}
			else
			{
				Command.IsValidCommand = false;
				Command.IsForAnotherBot = true;
			}

			if (Command.IsValidCommand)
				if (AvailableCommands == null)
					Command.IsValidCommand = true;
				else
					if (command.Equals(TelegramCommand.START, StringComparison.InvariantCultureIgnoreCase) == false &&
						command.Equals(TelegramCommand.STARTGROUP, StringComparison.InvariantCultureIgnoreCase) == false) // these are always good
					Command.IsValidCommand = AvailableCommands.Where(x => x.Equals(command, StringComparison.InvariantCultureIgnoreCase)).Count() > 0;//perhaps ==1

			Command.Name = command;

		}

		public override string ToString()
		{
			return $"{From?.Id}|{ChatId}|{IsCallbackQuery}|{OriginalInputText}|{Command}";/*|{AnsweredQuestion}*/
		}
	}
}
