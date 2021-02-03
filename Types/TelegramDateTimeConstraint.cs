using System;
using System.Globalization;
using Telegram.Messaging.Db;

namespace Telegram.Messaging.Types
{
	public class TelegramDateTimeConstraint : TelegramConstraint
	{
		public DateTime? Min { get; set; }
		public DateTime? Max { get; set; }
		public DateTime? Exact { get; set; }

		public TelegramDateTimeConstraint(DateTime? min, DateTime? max, DateTime? exact) : base(FieldTypes.DateTime)
		{
			Min = min;
			Max = max;
			Exact = exact;
		}

		public override bool Validate(string value)
		{
			bool isDate = DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime parsed);

			if (isDate == false) return false;

			if (Min != null && parsed < Min) return false;
			if (Max != null && parsed > Max) return false;
			if (Exact != null && parsed != Exact) return false;

			return true;
		}
	}
}
