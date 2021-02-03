using Telegram.Messaging.Db;

namespace Telegram.Messaging.Types
{
	public class TelegramIntConstraint : TelegramConstraint
	{
		public int? Min { get; set; }
		public int? Max { get; set; }
		public int? Exact { get; set; }

		public TelegramIntConstraint(int? min, int? max, int? exact) : base(FieldTypes.Int)
		{
			Min = min;
			Max = max;
			Exact = exact;
		}

		public override bool Validate(string value)
		{
			bool isNum = int.TryParse(value, out int num);
			if (isNum == false) return false;

			if (Min != null && num < Min) return false;
			if (Max != null && num > Max) return false;
			if (Exact != null && num != Exact) return false;

			return true;
		}
	}
}
