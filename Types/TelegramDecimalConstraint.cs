using Telegram.Messaging.Db;

namespace Telegram.Messaging.Types
{
	public class TelegramDecimalConstraint : TelegramConstraint
	{
		public decimal? Min { get; set; }
		public decimal? Max { get; set; }
		public decimal? Exact { get; set; }

		public TelegramDecimalConstraint(decimal? min, decimal? max, decimal? exact) : base(FieldTypes.Decimal)
		{
			Min = min;
			Max = max;
			Exact = exact;
		}

		public override bool Validate(string value)
		{
			bool isNum = decimal.TryParse(value, out decimal num);
			if (isNum == false) return false;

			if (Min != null && num < Min) return false;
			if (Max != null && num > Max) return false;
			if (Exact != null && num != Exact) return false;

			return true;
		}
	}
}
