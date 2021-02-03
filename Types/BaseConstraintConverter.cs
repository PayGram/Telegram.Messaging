using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Telegram.Messaging.Types
{
	public class BaseConstraintConverter : JsonConverter
	{
		static JsonSerializerSettings SpecifiedSubclassConversion = new JsonSerializerSettings() { ContractResolver = new BaseSpecifiedConcreteClassConverter() };

		public override bool CanConvert(Type objectType)
		{
			return (objectType == typeof(TelegramConstraint));
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			JObject jo = JObject.Load(reader);
			switch ((Db.FieldTypes)jo["Type"].Value<int>())
			{
				case Db.FieldTypes.Bool:
					return JsonConvert.DeserializeObject<TelegramBoolConstraint>(jo.ToString(), SpecifiedSubclassConversion);
				case Db.FieldTypes.Int:
					return JsonConvert.DeserializeObject<TelegramIntConstraint>(jo.ToString(), SpecifiedSubclassConversion);
				case Db.FieldTypes.String:
					return JsonConvert.DeserializeObject<TelegramStringConstraint>(jo.ToString(), SpecifiedSubclassConversion);
				case Db.FieldTypes.DateTime:
					return JsonConvert.DeserializeObject<TelegramDateTimeConstraint>(jo.ToString(), SpecifiedSubclassConversion);
				case Db.FieldTypes.Double:
					return JsonConvert.DeserializeObject<TelegramDoubleConstraint>(jo.ToString(), SpecifiedSubclassConversion);
				case Db.FieldTypes.Decimal:
					return JsonConvert.DeserializeObject<TelegramDecimalConstraint>(jo.ToString(), SpecifiedSubclassConversion);
				default:

					throw new Exception();
			}
			throw new NotImplementedException();
		}

		public override bool CanWrite
		{
			get { return false; }
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			throw new NotImplementedException(); // won't be called because CanWrite returns false
		}
	}
}
