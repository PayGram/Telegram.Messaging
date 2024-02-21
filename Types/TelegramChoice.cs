using Newtonsoft.Json;
using Utilities.String.Json.Extentions;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Telegram.Messaging.Types
{

	public class TelegramChoice
	{
		internal readonly static TelegramChoice _CancelAnswer = new TelegramChoice("Home", "__Cancel__");
		internal readonly static TelegramChoice _SkipAnswer = new TelegramChoice("Skip", "__Skip__");
		internal readonly static TelegramChoice _BackAnswer = new TelegramChoice("Back", "__Back__");
		internal readonly static TelegramChoice _PayAnswer = new TelegramChoice("Pay", "__Pay__");
		internal readonly static TelegramChoice _PrevPageAnswer = new TelegramChoice("Prev. Page", "__PrevPage__");
		internal readonly static TelegramChoice _CurrPageAnswer = new TelegramChoice("Page {0}", "__CurrPage__");
		internal readonly static TelegramChoice _NextPageAnswer = new TelegramChoice("Next. Page", "__NextPage__");
		internal readonly static TelegramChoice _NewKeyboardLine = new TelegramChoice("", "__NewKeyboardLine__");
		public static TelegramChoice CancelAnswer => new(_CancelAnswer);
		public static TelegramChoice SkipAnswer => new(_SkipAnswer);
		public static TelegramChoice BackAnswer => new(_BackAnswer);
		public static TelegramChoice PayAnswer => new(_PayAnswer);
		public static TelegramChoice PrevPageAnswer => new(_PrevPageAnswer);
		public static TelegramChoice CurrPageAnswer => new(_CurrPageAnswer);
		public static TelegramChoice NextPageAnswer => new(_NextPageAnswer);

		/// <summary>
		/// This choice will never be added on the keyboard. It is used as hint and when the keyboard builder finds it, 
		/// it will add a new row. It is not considered a system choice.
		/// </summary>
		public static TelegramChoice NewKeyboardLine => new(_NewKeyboardLine);
		internal static readonly TelegramChoice[] SystemChoices;

		static TelegramChoice()
		{
			SystemChoices = new TelegramChoice[] { CancelAnswer, SkipAnswer, BackAnswer, PayAnswer, PrevPageAnswer, CurrPageAnswer, NextPageAnswer };
		}
		[JsonIgnore]
		public bool IsSystemChoice { get => SystemChoices.Where(x => x.Equals(this)).Count() > 0; }

		string? label;
		[JsonProperty("l")]
		public string? Label { get => Param != null && label != null ? string.Format(label, Param) : label; set => label = value; }

		[JsonIgnore]
		public string? RawLabel => label;

		[JsonProperty("v")]
		public string Value { get; set; }
		[JsonProperty("p")]
		public string? Param { get; set; }
		//[JsonIgnoreAttribute]
		//[JsonProperty("m")]
		//public long MessageId { get; set; }
		[JsonProperty("q")]
		public long QuestionId { get; set; }


		[JsonIgnore]
		public bool IsUrl { get => Uri.TryCreate(Value, UriKind.Absolute, out _); }

		/// <summary>
		/// Creates a new default answer where the label and the value are the same
		/// </summary>
		/// <param name="label">The displayed label and the value associated with this answer. If null is passed, an ArgumentNullException is thrown</param>
		public TelegramChoice(string? label)
			: this(label, label, null)
		{

		}

		/// <summary>
		/// Creates a new default answer
		/// </summary>
		/// <param name="label">The displayed label of this answer</param>
		/// <param name="value">The value associated with this answer. If null is passed, an ArgumentNullException is thrown</param>
		/// <param name="param">The parameter assigned to this choice. If parameter is set, and Label is formattable, label will be formatted with the parameter</param>
		public TelegramChoice(string? label, string? value, string? param = null)
		{
			Value = value;
			Label = label;
			Param = param;
			if (value == null)
				throw new ArgumentNullException("Value of 'value' cannot be null @ DefaultAnswer");
		}

		/// <summary>
		/// This is the Json used constructor
		/// </summary>
		[JsonConstructor]
		public TelegramChoice() : this("", "")
		{

		}

		public TelegramChoice(TelegramChoice choice) : this(choice?.Label, choice?.Value, choice?.Param)
		{
			if (choice == null) throw new ArgumentNullException("Choice can't be null");
		}

		/// <summary>
		/// Compares a DefaultAnswer or a string with this DefaultAnswer.
		/// Comparisons are case insensitive.
		/// </summary>
		/// <param name="obj">If the passed object is a string, it will be compared against the value of this DefaultAnswer. 
		/// Otherwise, if the passed object is a DefaultAnswer the passed DefaultAnswer.Value will be compared against this.Value</param>
		/// <returns></returns>
		public override bool Equals(object? obj)
		{
			if (obj == null) return false;

			string rightValue = obj is string ? obj.ToString() : obj is TelegramChoice ? ((TelegramChoice)obj).Value : null;
			if (rightValue == null) return false;

			return rightValue.Equals(Value, System.StringComparison.InvariantCultureIgnoreCase);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		/// <summary>
		/// doesn't include the label on the serialization and escapes the value of the command
		/// </summary>
		/// <returns></returns>
		public string ToJsonSpecial()
		{
			// ugly way to exclude the label from the serialization
			var oldLabel = Label;
			Label = null;
			var oldValue = Value;
			Value = EscapeCommandValue(Value);
			var j = JsonConvertExt.SerializeIgnoreAndPopulate(this);
			Label = oldLabel;
			Value = oldValue;
			return j;
		}

		public static TelegramChoice? FromJsonSpecial(string? json)
		{
			TelegramChoice tc = JsonConvertExt.DeserializeObject<TelegramChoice>(json);
			if (tc == null) return null;
			tc.Value = UnEscapeCommandValue(tc.Value);
			return tc;
		}

		internal void UnescapeValue()
		{
			this.Value = UnEscapeCommandValue(Value);
		}

		public const char PARAMS_SPACE_SEP = ' ';
		public const char PARAMS_UNDER_SEP = '_';
		public const char ESCAPED_SPACE = '\t';
		public const char ESCAPED_UNDER = '\u001B';

		public static string EscapeCommandValue(string? value)
		{
			if (string.IsNullOrWhiteSpace(value)) return null;

			return value.Replace(PARAMS_SPACE_SEP, ESCAPED_SPACE).Replace(PARAMS_UNDER_SEP, ESCAPED_UNDER);
		}

		public static string UnEscapeCommandValue(string? value)
		{
			if (string.IsNullOrWhiteSpace(value)) return null;

			string unesc = value.Replace(ESCAPED_SPACE, PARAMS_SPACE_SEP).Replace(ESCAPED_UNDER, PARAMS_UNDER_SEP);
			return unesc;
		}
		public static bool operator ==(TelegramChoice? a, TelegramChoice? b)
		{
			if (a is null && b is null) return true;
			return a?.Equals(b) == true; //a.Value==b.Value && a.Label==b.Label;	
		}
		public static bool operator !=(TelegramChoice? a, TelegramChoice? b)
		{
			if (a is null && b is null) return false;
			return a?.Equals(b) == false;// a.Value != b.Value || a.Label != b.Label;
		}
		public override string ToString()
		{
			return $"QID:{QuestionId}:{Label}:{Value}";
		}
	}
}
