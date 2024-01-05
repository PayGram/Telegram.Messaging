using log4net;
using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace Telegram.Messaging.Types
{
	public class TelegramCommand
	{
		ILog log = LogManager.GetLogger(typeof(TelegramCommand));
		public const char PARAMS_SPACE_SEP = ' ';
		public const char PARAMS_AND_SEP = '&';
		public const char PARAMS_EQUAL_SEP = '=';

		public const string START = "start";
		public const string STARTGROUP = "startgroup";

		public bool IsValidCommand { get; internal set; }
		public string Name { get; internal set; }
		public bool IsForAnotherBot { get; internal set; }
		public bool IsStartCommand { get { return Name != null && (Name.Equals(START, StringComparison.InvariantCultureIgnoreCase) || Name.Equals(STARTGROUP, StringComparison.InvariantCultureIgnoreCase)); } }
		Dictionary<string, string> parameters;

		public TelegramCommand()
		{
			parameters = new Dictionary<string, string>();
		}
		public TelegramCommand(string name) : this()
		{
			if (string.IsNullOrWhiteSpace(name)) return;
			if (name.StartsWith("/"))
				Name = name.Substring(1);
		}

		/// <summary>
		/// Adds or replace a parameter for this command
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		public void AddParameter(string name, string value)
		{
			if (parameters.ContainsKey(name))
				parameters[name] = value;
			else
				parameters.Add(name, value);
		}

		/// <summary>
		/// Adds parameters to this command
		/// </summary>
		/// <param name="namesValues">name1=value1&name2=value2....</param> 
		public void AddParameters(string namesValues)
		{
			if (string.IsNullOrWhiteSpace(namesValues)) return;

			var tuples = namesValues.Split(new char[] { PARAMS_AND_SEP }, StringSplitOptions.RemoveEmptyEntries);

			foreach (string tuple in tuples)
			{
				var nameAndValue = tuple.Split(new char[] { PARAMS_EQUAL_SEP });
				if (nameAndValue.Length == 2)
				{
					AddParameter(nameAndValue[0], nameAndValue[1]);
				}
			}
		}

		/// <summary>
		/// Removes a parameter with the given name
		/// </summary>
		/// <param name="name"></param>
		public void RemoveParameter(string name)
		{
			if (parameters.ContainsKey(name))
				parameters.Remove(name);
		}

		/// <summary>
		/// Returns the value for the specified parameter or null if it was not found
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public string? GetParameterValue(string name)
		{
			if (parameters.ContainsKey(name)) return parameters[name];
			return null;
		}
		public T? GetParameterValue<T>(string name)
		{
			var value = parameters.GetValueOrDefault(name);
			if (value == null) return default(T);
			return (T?)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
		}
		public T? GetParameterAt<T>(int idx)
		{
			if (parameters.Count <= idx) return default(T);
			var value = parameters.ElementAt(idx).Value;
			if (value == null) return default(T);
			try
			{
				return (T?)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
			}
			catch { return default(T); }
		}
		public int ParametersCount => parameters.Count;
		/// <summary>
		/// Get a string containing all the parameters and separating them by a space
		/// </summary>
		public string Query => string.Join(PARAMS_SPACE_SEP, parameters.Values);
		/// <summary>
		/// Returns a list of name=values separated by &
		/// </summary>
		public string GetNameValues()
		{
			return string.Join($"{PARAMS_AND_SEP}", parameters.Keys.Select(x => $"{x}{PARAMS_EQUAL_SEP}{parameters[x]}"));
		}

		public override string ToString()
		{
			string namesValues = GetNameValues();
			return Name + namesValues + " " + IsForAnotherBot;
		}

		public static string MakeBase64(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return value;
			return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
		}

		public static string FromBase64(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return value;
			Span<byte> buffer = new Span<byte>(new byte[value.Length]);
			bool success = Convert.TryFromBase64String(value, buffer, out int bytesParsed);
			if (success == false) return value;
			return Encoding.UTF8.GetString(buffer.Slice(0, bytesParsed));
		}
	}
}
