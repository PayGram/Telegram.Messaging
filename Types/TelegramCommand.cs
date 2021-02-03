using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
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
		public string GetParameterValue(string name)
		{
			if (parameters.ContainsKey(name)) return parameters[name];
			return null;
		}

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

		public static string EscapeCommandValue(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return null;
			return Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(value));
		}

		public static string UnEscapeCommandValue(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return null;
			try
			{
				return UTF8Encoding.UTF8.GetString(Convert.FromBase64String(value));
			}
			catch (Exception)
			{
				return value;
			}
		}
	}
}
