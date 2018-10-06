using System.Collections.Generic;
using UnityEngine;

/* ==============================================================================================
 * NMS (New Maschine Standart) is the programming language of our univers.
 * The goal is to enable the player to do some useful things using this language in their console
 * ============================================================================================== */

namespace NMS
{
	/// <summary> What kind of error it is </summary>
	public enum ErrorType
	{
		syntax,
		oo_range,
		default_
	}

	/// <summary> An error (exception) which occured and also should be thrown </summary>
	public struct Error
	{
		public string message;
		public uint line;
		public string file;
		public ErrorType type;

		public string Display () {
			return string.Format("{0}Error at line {1} in {2}: {3}", type, line, file, message);
		}
	}

	/// <summary>
	///		THe Interpreter, which interprets text or sngle lines
	///		and executes the corresponding commands directly
	/// </summary>
	public class Interpreter
	{
		public Dictionary<string, IValue> global = new Dictionary<string, IValue>();
		public Dictionary<string, IValue> local = new Dictionary<string, IValue>();

		public Dictionary<string, uint> labels = new Dictionary<string, uint>();

		public ushort recursion;
		public OS.OperatingSystem os;

		/// <summary>
		///		Processes one line
		/// </summary>
		/// <param name="command"> The line to be processed </param>
		/// <param name="result">
		///		The out of the process in form of an array of bytes (in case of a function).
		///		An empty array is returned in case of no result
		/// </param>
		/// <returns> The text-based response of the interpreter(confirmation or error) </returns>
		public string Process (string command, out byte [] result) {
			result = new byte [0];
			if (command == string.Empty) {
				os.ThrowError("command cannot be \"\"");
			}
			string [] suff_split = command.Replace("\t", "").Split(' ');
			List<string> act_command = new List<string>(suff_split).GetRange(1, suff_split.Length-1);
			switch (command [0]) {
			// Empty (comment)
			case 'E':
				return "> ";

			// Declare
			case 'D':
				string type_str = act_command[0];
				string name_str;
				string value_str;
				string comm_1 = act_command [1];
				if (comm_1.Contains("[") && comm_1.Contains("]")) {
					name_str = comm_1.Substring(0, comm_1.IndexOf("["));
					value_str = comm_1.Substring(comm_1.IndexOf("[") + 1, comm_1.IndexOf("]") - comm_1.IndexOf("[") - 1);
				} else {
					name_str = comm_1;
					value_str = null;
				}
				Declare(type_str, name_str, value_str);
				return string.Format("> {0} = {1}", name_str, value_str);

			// Set
			case 'S':
				string value_name = act_command [0];
				if (act_command [1] != "->") {
					os.ThrowError("\"->\" expected");
				}
				value_str = string.Empty;
				for (int i = 2; i < act_command.Count - 2; i++) { value_str += act_command [i] + " "; }
				IValue value = Analyze(value_str);
				if (local.ContainsKey(value_name)) {
					local [value_name] = value;
					return string.Format("local value {0} altered to {1}", value_name, value.ToString());
				}
				if (global.ContainsKey(value_name)) {
					global [value_name] = value;
					return string.Format("global value {0} altered to {1}", value_name, value.ToString());
				}
				os.ThrowError(value_name + " does not exist");
				return "";

			// Command
			case 'C':
				break;

			// Trigger
			case 'T':
				break;

			// Label
			case 'L':
				break;

			// Function
			case 'F':
				break;

			default:
				if (command [0] == '+' || command [0] == '-' || command [0] == '~') {
					return "";
				}
				break;
			}
			return string.Format("> Work in Progress, you Typed \"{0}\"", command);
		}

		/// <summary>
		///		Declares a variable
		/// </summary>
		private void Declare (string type, string name, string value = null) {
			Dictionary<string, IValue> dict = recursion > 0 ? local : global;
			switch (type) {
			case "In":
				byte [] datai = value == null ? new byte [2] { 0x00, 0x00 } : Converter.String2Scalar(value, true);
				dict.Add(name, new Single(VType.integer, datai));
				break;
			case "Fl":
				byte [] dataf = value == null ? new byte [2] { 0x00, 0x00 } : Converter.String2Scalar(value, false);
				dict.Add(name, new Single(VType.floating, dataf));
				Debug.Log(Converter.ShowBinary(dataf));
				break;
			case "St":
				byte [] datas = value == null ? new byte [1] { 0x00 } : Converter.DisectString(value);
				dict.Add(name, new Array(VType.character, datas));
				break;
			case "Bi":
				byte _byte = (byte) ((value.ToUpper() == "True" || value == "1") ? 0x01 : 0x00);
				dict.Add(name, new Single(VType.boolean, new byte [1] { _byte }));
				break;
			}
		}

		private IValue Analyze (string str) {
			string [] commands = str.Split(' ');
			IValue current = Single.none;
			foreach (string cmd in commands) {
				IValue new_value = Single.none;
				if (cmd.StartsWith("&")) {
					string funcname = cmd.Substring(1);
					if (cmd.Contains("{") && !cmd.Contains("}")) {
						os.ThrowError("} expected", 0);
					}
					string args_str = cmd.Substring(cmd.IndexOf("{"), cmd.IndexOf("}") - cmd.IndexOf("{"));
					string [] args_strings = args_str.Split(',');

					if (!global.ContainsKey(funcname)) {
						os.ThrowError(funcname + " does not exist");
					}
					if (!(global [funcname] is Function)) {
						os.ThrowError(funcname + " is not a function");
					}

					IValue[] args = new IValue [args_strings.Length];
					for (int i = 0; i < args_strings.Length; i++) {
						args [i] = Analyze(args_strings [i]);
					}

					Function func = global[funcname] as Function;
					new_value = func.Execute(args);
					goto NEWVALUE;

				} else if (cmd.StartsWith("$")) {
					string variable_name = cmd.Substring(1);
					if (local.ContainsKey(variable_name)) {
						new_value = local [variable_name];
						goto NEWVALUE;
					}
					if (global.ContainsKey(variable_name)) {
						new_value = global [variable_name];
						goto NEWVALUE;
					}
				}
				if (cmd.EndsWith("]")) {

				}
				NEWVALUE:
				;
			}
			return current;
		}
	}

	/// <summary>
	///		Contains some static methods to convert variabloes into
	///		other variables
	/// </summary>
	public static class Converter
	{
		public static uint Str2Int (string str) {
			uint result = 0u;
			for (int i = str.Length - 1; i >= 0; i--) {
				if (str [i] < 48 || str [i] > 57) {
					Data.current_os.ThrowError("Inexpected character: " + str [i]);
				}
				ushort mantissa = (ushort)(str[i] - 48);
				ulong power = 1ul;
				for (int j = 0; j < str.Length - i - 1; j++) { power *= 10ul; }
				result += (uint) (mantissa * power);
			}
			return result;
		}

		public static byte [] String2Scalar (string str, bool is_int = true) {
			bool sign = true;
			string act_str = str;
			if (str [0] == '-') {
				sign = false;
				act_str = str.Substring(1);
			}

			if (is_int) {
				uint result = Str2Int(act_str);
				byte [] data = new byte [2] { (byte) ((sign ? 0x80 : 0x00) + ((result / 0x100) % 0x8000)), (byte) (result % 0x100) };
				return data;
			}
			string [] dec_split = act_str.Split('.');
			string whole = dec_split[0];
			string dec = dec_split[1];

			double mant = Str2Int(whole) + Str2Int(dec) / Mathf.Pow(10, dec.Length);
			uint exp = 0;
			while (mant % 1 != 0 && mant * 2 < 0x100 && exp + 1 < 0x80) {
				mant *= 2d;
				exp++;
			}
			return new byte [2] { (byte) ((sign ? 0x80 : 0x00) + exp), (byte) mant };
		}

		public static byte [] DisectString (string str) {
			byte [] data = new byte[str.Length];
			for (int i = 0; i < str.Length; i++) {
				data [i] = (byte) str [i];
			}
			return data;
		}

		public static string ShowBinary (byte [] data) {
			string res = "";
			for (int i = 0; i < data.Length; i++) {
				byte b = data[i];
				for (int j = 7; j >= 0; j--) {
					ushort power = 1;
					for (int k = 0; k < j; k++) { power *= 2; }
					res += (b / power);
					if (b >= power) { b -= (byte) power; }
				}
			}
			return res;
		}
	}
}