using Telegram.Messaging.Db;

namespace Telegram.Messaging.Types
{
	public class TelegramDoubleConstraint : TelegramConstraint
	{
		public double? Min { get; set; }
		public double? Max { get; set; }
		public double? Exact { get; set; }

		public TelegramDoubleConstraint(double? min, double? max, double? exact) : base(FieldTypes.Double)
		{
			Min = min;
			Max = max;
			Exact = exact;
		}

		public override bool Validate(string value)
		{
			bool isNum = double.TryParse(value, out double num);
			if (isNum == false) return false;

			if (Min != null && num < Min) return false;
			if (Max != null && num > Max) return false;
			if (Exact != null && num != Exact) return false;

			return true;
		}
	}
}
