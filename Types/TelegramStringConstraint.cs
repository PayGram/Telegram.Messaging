using System.Text.RegularExpressions;
using Telegram.Messaging.Db;

namespace Telegram.Messaging.Types
{
	public class TelegramStringConstraint : TelegramConstraint
	{
		public int? Min { get; set; }
		public int? Max { get; set; }
		public int? Exact { get; set; }
		public string Regex { get; set; }

		public TelegramStringConstraint(int? min, int? max, int? exact, string regEx) : base(FieldTypes.String)
		{
			Min = min;
			Max = max;
			Exact = exact;
			Regex = regEx;
		}

		public override bool Validate(string value)
		{
			if (Min != null && (value == null || value.Length < Min)) return false;
			if (Max != null && (value == null || value.Length > Max)) return false;
			if (Exact != null && (value.Length != Exact)) return false;
			if (Regex != null)
			{
				Regex r = new Regex(Regex);
				return r.IsMatch(value);
			}
			return true;
		}
	}
}
