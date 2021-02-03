using Newtonsoft.Json;
using Telegram.Messaging.Db;

namespace Telegram.Messaging.Types
{
	[JsonConverter(typeof(BaseConstraintConverter))]
	public abstract class TelegramConstraint
	{
		public FieldTypes Type { get; set; }

		public TelegramConstraint(FieldTypes type)
		{
			Type = type;
		}

		[JsonConstructor]
		public TelegramConstraint()
		{

		}

		public static TelegramConstraint FromTypeName(string typeName)
		{
			if (string.IsNullOrWhiteSpace(typeName)) return null;

			switch (typeName.ToLower())
			{
				case "bool": return new TelegramBoolConstraint();
				case "datetime": return new TelegramDateTimeConstraint(null, null, null);
				case "int": return new TelegramIntConstraint(null, null, null);
				case "string": return new TelegramStringConstraint(null, null, null, null);
				case "double": return new TelegramIntConstraint(null, null, null);
			}
			return null;
		}
		public static TelegramConstraint FromTypeId(FieldTypes type)
		{

			switch (type)
			{
				case FieldTypes.Bool: return new TelegramBoolConstraint();
				case FieldTypes.DateTime: return new TelegramDateTimeConstraint(null, null, null);
				case FieldTypes.Int: return new TelegramIntConstraint(null, null, null);
				case FieldTypes.String: return new TelegramStringConstraint(null, null, null, null);
				case FieldTypes.Double: return new TelegramDoubleConstraint(null, null, null);
				case FieldTypes.Decimal: return new TelegramDecimalConstraint(null, null, null);
			}
			return null;
		}


		public abstract bool Validate(string value);
	}
}
