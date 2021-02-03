using Telegram.Messaging.Db;

namespace Telegram.Messaging.Types
{
	public class TelegramBoolConstraint : TelegramConstraint
	{
		public bool? Exact { get; set; }
		public TelegramBoolConstraint() : base(FieldTypes.Bool)
		{

		}

		public override bool Validate(string value)
		{
			bool isBool = bool.TryParse(value, out bool res);
			if (isBool && Exact == null) return true;
			return Exact == res;
		}
	}
}
