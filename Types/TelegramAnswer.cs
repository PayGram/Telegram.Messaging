using Newtonsoft.Json;
using System.Globalization;
using Telegram.Messaging.Db;
using Utilities.String.Json.Extentions;

namespace Telegram.Messaging.Types
{
	public class TelegramAnswer
	{
		[JsonIgnore]
		public Question AnsweredQuestion { get; internal set; }
		[JsonProperty("v")]
		public bool IsValidAnswer { get; set; }
		string _answer;
		[JsonProperty("a")]
		public string Answer
		{
			get
			{
				if (PickedChoice != null && PickedChoice.IsSystemChoice == false && PickedChoice.Value != null)
					return PickedChoice.Value;
				return _answer;
			}
			set => _answer = value;
		}
		[JsonProperty("du")]
		public DateTime AnswerDateUtc { get; set; }
		[JsonProperty("pc")]
		public TelegramChoice PickedChoice { get; set; }
        [JsonConstructor]
		public TelegramAnswer()
		{
		}

		public TelegramAnswer(Question answeredQuestion, string answer)
		{
			AnsweredQuestion = answeredQuestion;
			AnswerDateUtc = DateTime.UtcNow;
			Answer = answer;
		}
		//public TelegramAnswer(Question answeredQuestion, TelegramChoice answer)
		//{
		//	AnsweredQuestion = answeredQuestion;
		//	AnswerDateUtc = DateTime.UtcNow;
		//	PickedChoice = answer;
		//}
		public TelegramAnswer(Question answeredQuestion, TelegramMessage message)
		{
			AnsweredQuestion = answeredQuestion;
			AnswerDateUtc = DateTime.UtcNow;
			if (message.PickedChoice != null)
				PickedChoice = message.PickedChoice;
			else if (message.IsPhoto)
				Answer = message.Message.Photo[0].FileId;
		}
		public bool EnforceConstraints()
		{
			if (AnsweredQuestion == null)
			{
				IsValidAnswer = false;
				return false;
			}
			if (AnsweredQuestion.IsMandatory && string.IsNullOrWhiteSpace(Answer))
			{
				IsValidAnswer = false;
				return false;
			}
			if (AnsweredQuestion.IsMandatory == false && string.IsNullOrWhiteSpace(Answer))
			{
				IsValidAnswer = true;
				return true;
			}

			if (AnsweredQuestion.PickOnlyDefaultAnswers && AnsweredQuestion.DefaultAnswers != null && AnsweredQuestion.DefaultAnswers.Length > 0)
			{
				IsValidAnswer = AnsweredQuestion.DefaultAnswersList.Where(x => x.Value != null && x.Value.Equals(Answer, StringComparison.CurrentCultureIgnoreCase)).Count() > 0;
				if (IsValidAnswer == false) return IsValidAnswer;
			}

			foreach (TelegramConstraint constraint in AnsweredQuestion.TelegramConstraints)
			{
				IsValidAnswer = constraint.Validate(Answer);
				if (IsValidAnswer == false) return false;
			}
			IsValidAnswer = true;
			return true;
		}

		public T? GetAnswer<T>()
		{
			if (IsValidAnswer == false) return default;

			if (typeof(bool) == typeof(T) && AnsweredQuestion.FieldTypeId == FieldTypes.Bool) return (T)Convert.ChangeType(Answer, typeof(T), CultureInfo.InvariantCulture);
			if (typeof(int) == typeof(T) && AnsweredQuestion.FieldTypeId == FieldTypes.Int) return (T)Convert.ChangeType(Answer, typeof(T), CultureInfo.InvariantCulture);
			if (typeof(DateTime) == typeof(T) && AnsweredQuestion.FieldTypeId == FieldTypes.DateTime) return (T)Convert.ChangeType(DateTime.Parse(Answer, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal), typeof(T));
			if (typeof(string) == typeof(T) && AnsweredQuestion.FieldTypeId == FieldTypes.String) return (T)Convert.ChangeType(Answer, typeof(T));
			if (typeof(double) == typeof(T) && AnsweredQuestion.FieldTypeId == FieldTypes.Double) return (T)Convert.ChangeType(Answer, typeof(T), CultureInfo.InvariantCulture);
			if (typeof(decimal) == typeof(T) && AnsweredQuestion.FieldTypeId == FieldTypes.Decimal) return (T)Convert.ChangeType(Answer, typeof(T), CultureInfo.InvariantCulture);

			return default;
		}

		public override string ToString()
		{
			return JsonConvertExt.SerializeIgnoreAndPopulate(this);
		}
	}
}
