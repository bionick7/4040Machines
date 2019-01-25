using System.Collections.Generic;
using System.IO;
using System;
using System.Text.RegularExpressions;
using UnityEngine;

// C:\Users\Nick\AppData\LocalLow\DefaultCompany\spacesim

/* =================================================================================
 * Here you will find anything, that helps you to read files, store data and read it
 * ================================================================================== */

namespace FileManagement
{
	/// <summary> Contains methods for Filereading/-writing </summary>
	public static class FileReader
	{
		public const char placehloder = (char) 0xd1;

		public static string logfile;

		//Reads lines from a given path into a string array (and gives length into an int) returns true if it was succesfull
		public static bool ReadLines (string path, ref string [] in_txt) {
			if (File.Exists(path)) {
				in_txt = File.ReadAllLines(path);
				return true;
			}
			in_txt = new string [0];
			return false;
		}

		public static Int32 ParseInt (string content, string file_name, int line) {
			Int32 res = 0;
			if (content.StartsWith("x")) {
				res = 0;
				for (int i = 1; i < content.Length; i++) {
					char digit = content [content.Length - i];
					int mant = 0;
					if (48 <= digit && digit <= 57) mant = digit - 48;
					else if (65 <= digit && digit <= 70) mant = digit - 55;
					else if (97 <= digit && digit <= 102) mant = digit - 87;
					for (int j = 0; j < i - 1; j++) mant *= 16;
					res += mant;
				}
			} else {
				if (!Int32.TryParse(content, out res)) {
					DataStructure.Throw("\"" + content + "\" is not a valid integer", file_name, line);
					res = 0;
				}
			}
			return res;
		}

		public static UInt16 ParseShort (string content, string file_name, int line) {
			UInt16 res = 0;
			if (content.StartsWith("x") && content.Length <= 5) {
				res = 0;
				for (int i = 1; i < content.Length; i++) {
					char digit = content [i];
					int mant = 0;
					if (48 <= digit && digit <= 57) mant = digit - 48;
					else if (65 <= digit && digit <= 70) mant = digit - 55;
					else if (97 <= digit && digit <= 102) mant = digit - 87;
					for (int j = 0; j < digit - 1; j++) mant *= 16;
					res += (ushort) mant;
				}
			} else {
				if (!UInt16.TryParse(content, out res)) {
					DataStructure.Throw("\"" + content + "\" is not a valid short integer", file_name, line);
					res = 0;
				}
			}
			return res;
		}

		public static UInt64 ParseLong (string content, string file_name, int line) {
			UInt64 res = 0;
			if (!UInt64.TryParse(content, out res)) {
				DataStructure.Throw("\"" + content + "\" is not a valid short integer", file_name, line);
				res = 0;
			}
			return res;
		}

		public static Single ParseFloat (string content, string file_name, int line) {
			Single res = 0;
			if (!Single.TryParse(content, out res)) {
				DataStructure.Throw("\"" + content + "\" is not a valid 32-bit float", file_name, line);
				res = 0;
			}
			return res;
		}

		public static Double ParseDouble (string content, string file_name, int line) {
			Double res = 0;
			if (!Double.TryParse(content, out res)) {
				DataStructure.Throw("\"" + content + "\" is not a 64-bit float", file_name, line);
				res = 0;
			}
			return res;
		}

		///<summary> Trys to read vectors from a string array </summary>
		///<param name="txt_lines"> The lines of text, containing the vectors </param>
		///<param name="sep"> The sysmbol, with wich the numbers of the vector are seperated, default is ',' </param>
		///<returns> An array of te vectors </returns>
		public static Vector3 [] ParseVector (string [] txt_lines, char [] sep = null) {
			sep = sep ?? new char [1] { ',' };
			int length = txt_lines.Length;
			Vector3[] result = new Vector3[length];
			for (int i = 0; i < length; i++) {
				if (txt_lines [i] == "0") result [i] = Vector3.zero;
				else if (Regex.IsMatch(txt_lines [i], @"^rnd\s*-?\d+\s*,\s*-?\d+$")) {
					var matches = Regex.Matches(txt_lines [i], @"-?\d+");
					float min_value = Single.Parse(matches[0].Value);
					float max_value = Single.Parse(matches[1].Value);
					result [i] = HandyTools.RandomVector * (max_value - min_value) + new Vector3(min_value, min_value, min_value);
				} else {
					string [] txt = txt_lines[i].Split(sep);
					if (txt.Length != 3) {
						result [i] = Vector3.zero;
					} else {
						result [i] = new Vector3(Single.Parse(txt [0], System.Globalization.CultureInfo.InvariantCulture),
												 Single.Parse(txt [1], System.Globalization.CultureInfo.InvariantCulture),
												 Single.Parse(txt [2], System.Globalization.CultureInfo.InvariantCulture));
					}
				}
			}
			return result;
		}

		public static string ParseString (string input) {
			// Regular expressions at their finest
			if (Regex.IsMatch(input, "^\"{1,2}.*\"{1,2}$")) {
				// Multi line staff
				input = Regex.Replace(input, "^\"{1,2}\\s*", "");
				input = Regex.Replace(input, "\\s*\"{1,2}\\n?$", "");
				input = Regex.Replace(input, @"\s*Ñ\s*", "\n");
				if (input.Length > 2)
					input = input.Substring(1, input.Length - 2);
				else
					input = input.Substring(1);
				return input;
			}
			char[] inp_array = input.ToCharArray();
			for (int i = 0; i < input.Length; i++) {
				if (inp_array [i] == '_' && i > 0 && inp_array [i - 1] != 92) {
					inp_array [i] = ' ';
				}
			}
			input = new String(inp_array);
			input = input.Replace("\\\n", "\n");
			input = input.Replace(@"\_", "_");
			input = input.Replace(@"\\", @"\");
			return input;
		}

		/// <summary> Reads vectors straight from a file </summary>
		/// <param name="path"> The path of the file containing the Vectors </param>
		/// <returns> An array of the Vectors </returns>
		public static Vector3 [] ReadVectors (string path) {
			string [] txt_lines = new string[0];
			if (!ReadLines(path, ref txt_lines)) {
				return new Vector3 [0];
			}
			Vector3 [] result = ParseVector(txt_lines);
			return result;
		}

		/// <summary> Returns an array with all teh filenames </summary>
		/// <param name="path"> The file path </param>
		public static string [] AllFileNamesInDir (string path) {
			string[] file_paths = Directory.GetFiles(path + "/");
			List<string> fnames_list = new List<string>();
			for (int i = 0; i < file_paths.Length; i++) {
				if (!file_paths [i].EndsWith(".meta")) {
					fnames_list.Add(file_paths [i]);
				}
			}
			return fnames_list.ToArray();
		}

		/// <param name="path"> The path of the directory (without "\") </param>
		/// <returns> All the files in a directory as an array </returns>
		public static string [] [] AllFilesInDir (string path, out string [] fnames) {
			string[] file_paths = Directory.GetFiles(path + "/");
			List<string[]> end_arr = new List<string[]>();
			List<string> fnames_list = new List<string>();
			for (int i = 0; i < file_paths.Length; i++) {
				if (!file_paths [i].EndsWith(".meta")) {
					string[] out_str = new string[0];
					ReadLines(file_paths [i], ref out_str);
					end_arr.Add(out_str);
					fnames_list.Add(file_paths [i]);
				}
			}
			fnames = fnames_list.ToArray();
			return end_arr.ToArray();
		}

		/// <summary> Logs a line to the logfile </summary>
		/// <param name="log"> line to be logged </param>
		public static void FileLog (string log, FileLogType logtype) {
			var now = DateTime.Now;
			string prefix = "      ";
			switch (logtype) {
			case FileLogType.loader:
				prefix = "LOADER";
				break;
			case FileLogType.story:
				prefix = "STORY ";
				break;
			case FileLogType.error:
				prefix = "ERROR";
				break;
			default:
			case FileLogType.runntime:
				prefix = "RNTIME";
				break;
			case FileLogType.editor:
				prefix = "EDITOR";
				break;
			}

			string [] curr_content = File.ReadAllLines(logfile);
			string [] new_cont = new string [curr_content.Length + 1];
			for (int i = 0; i < curr_content.Length; i++) new_cont [i] = curr_content [i];
			new_cont [curr_content.Length] = String.Format("{0} | {2}: {1}", now.ToString("HH:mm:ss.fff"), log, prefix);
			File.WriteAllLines(logfile, new_cont);
		}
	}

	public enum FileLogType
	{
		error,
		loader,
		story,
		runntime,
		editor,
	}

	/// <summary> Means to store data </summary>
	public class DataStructure
	{
		public static string GeneralPath {
			get { return Globals.config_path; }
		}

		public const string extension = "cfgt";

		public static readonly DataStructure Empty = new DataStructure("empty");

		public string _name;
		/// <summary> The name of the DataStructure </summary>
		public string Name {
			get {
				if (_name [0] >= 48 && _name [0] <= 57)
					return _name.Substring(4);
				return _name;
			}
			set {
				_name = value;
				if (Parent != null) {
					ushort i = 0;
					while (Parent.children.ContainsKey(_name)) {
						_name = value + (++i).ToString("000");
					}
				}
			}
		}

		public DataStructure this [string name] {
			get { return children [name]; }
			set { children [name] = value; }
		}

		public DataStructure [] AllChildren {
			get { return new List<DataStructure>(children.Values).ToArray(); }
		}

		/// <summary> The parent Datastructure </summary>
		public DataStructure Parent { get; private set; }

		public int RecursionDepth {
			get {
				if (Parent == null) return 0;
				return Parent.RecursionDepth + 1;
			}
		}

		#region dictionaries

		// The following items are stored, as single values and as arrays (can be expanded to match needs):
		// --------------------------------------------------------------
		// integers (signed 32-bit)
		// short integers (unsigned 16-bit)
		// floating-point numbers (signed 32-bit)
		// floating-point numbers (signed 64-bit)
		// 3-dimensional Vector of signed 32-bit floating-point numbers (UnityEngine.Vector3)
		// A 3-dimensional rotation, represented as a 4-dimensional Vector (UnityEngine.Quaternion)
		// character array (string)
		// 1 simple bit (boolean)
		// A prefab from the resources folder saved as UnityEngine.GameObject
		// -------------------------------------------------------------

		public Dictionary<string, Int32> integers = new Dictionary<string, Int32>();
		public Dictionary<string, UInt16> short_integers = new Dictionary<string, UInt16>();
		public Dictionary<string, UInt64> long_integers = new Dictionary<string, UInt64>();
		public Dictionary<string, Single> floats32 = new Dictionary<string, Single>();
		public Dictionary<string, Double> floats64 = new Dictionary<string, Double>();
		public Dictionary<string, Vector3> vectors = new Dictionary<string, Vector3>();
		public Dictionary<string, Quaternion> rotations = new Dictionary<string, Quaternion> ();
		public Dictionary<string, String> strings = new Dictionary<string, String>();
		public Dictionary<string, Boolean> booleans = new Dictionary<string, Boolean>();
		public Dictionary<string, DSPrefab> prefabs = new Dictionary<string, DSPrefab>();
		public Dictionary<string, DSImage> images = new Dictionary<string, DSImage>();

		public Dictionary<string, Int32[]> integers_arr = new Dictionary<string, Int32[]>();
		public Dictionary<string, UInt16[]> short_integers_arr = new Dictionary<string, UInt16[]>();
		public Dictionary<string, UInt64[]> long_integers_arr = new Dictionary<string, UInt64[]>();
		public Dictionary<string, Single[]> floats32_arr = new Dictionary<string, Single[]>();
		public Dictionary<string, Double[]> floats64_arr = new Dictionary<string, Double[]>();
		public Dictionary<string, Vector3[]> vectors_arr = new Dictionary<string, Vector3[]>();
		public Dictionary<string, Quaternion[]> rotations_arr = new Dictionary<string, Quaternion[]>();
		public Dictionary<string, String[]> strings_arr = new Dictionary<string, String[]>();
		public Dictionary<string, Boolean[]> booleans_arr = new Dictionary<string, Boolean[]>();
		public Dictionary<string, DSPrefab[]> prefabs_arr = new Dictionary<string, DSPrefab[]>();
		public Dictionary<string, DSImage[]> images_arr = new Dictionary<string, DSImage[]>();

		//New DataStructures can be saved in a DataStructure, allowing for a recursive parents-children systrem.
		public Dictionary<string, DataStructure> children = new Dictionary<string, DataStructure>();

		#endregion

		/// <param name="name"> The name of the DataStructure </param>
		/// <param name="parent"> the parent of the Datastructure, null, if it has none </param>
		public DataStructure (string name = "", DataStructure parent = null) {
			Parent = parent;
			Name = name;
			if (parent != null) {
				parent.children.Add(_name, this);
			}
		}

		/// <summary>
		///		This creates a DataStructure from a textfile. These files have a specific syntax: 
		///		New children are indicated with a ">" followed by the child's name.
		///		Items inside of the children are then specified with a prefix, followed by their name, an "=" sign and
		///		finally their value.
		///		If an array shall be specified, the prexix is expended with a "*" and the length of the array, and the "=" is followed by
		///		more items separated by a newline
		///		This could look like this:
		/// </summary>
		/// <param name="txt_lines"> text_lines from which to read </param>
		/// <param name="name"> name of the datastructure </param>
		/// <param name="file_name"> name of the file </param>
		/// <param name="init_depht"> the initial recursion_dept of the datastructure </param>
		/// <returns> a new datastructure </returns>
		/// 
		/// <example>
		///		---------------------------------------
		///		 0
		///		 1		>child1
		///		 2			int integer_item = 5
		///		 3			bit*8-byte = 
		///		 4				0
		///		 5				1
		///		 6				1
		///		 7				1
		///		 8				0
		///		 9				0
		///		10				1
		///		11				0
		///		12		<
		///		13
		///		14		>child2
		///		15			chr string = Hello_World
		///		16			rot*2-rotations = 
		///		17				0, 0, 90
		///		18			 -270, 90, 3
		///		19		<
		///		20
		///		----------------------------------------				(Line numbers don't have to be specified)
		/// </example>
		/// 
		/// <remark>
		///		The prefixes are the following:
		///
		///			int -> integer
		///			snt -> short integer (unsigned)
		///			f32 -> 32-bit float
		///			f64 -> 64-bit float
		///			vc3 -> 3D vector (comma seperated)
		///			rot -> rotation in 3D space (as euler rotations, comma seperated)
		///			chr -> string (spaces, will be removed)
		///			bit -> booleans (0 for false or 1 for true)
		///			prf -> gameobject (indicated by path in the resources folder)
		///			img -> 2D Texture (indicated by path in teh config folder, must be png)
		/// </remark>
		public static DataStructure AnalyzeText (string [] txt_lines, string file_name, string name = "data", DataStructure parent = null) {
			int txt_size = txt_lines.Length;

			// Eliminate all comments, spaces and Tabs
			// Merges '§'-separated lines
			string[] txt_lines2 = new string[txt_size];
			// Set everything to empty
			for (int i = 0; i < txt_size; i++) txt_lines2 [i] = String.Empty;
			// What was added before previously
			string base_string = String.Empty;
			bool line_string_open = false;
			for (int i = 0; i < txt_size; i++) {
				string line = txt_lines[i];
				line = line.Replace('\t', ' ');
				if (!line_string_open) {
					line = line.Replace(" ", String.Empty);
				}
				if (line.Contains("//")) {
					line = line.Substring(0, line.IndexOf("//"));
				}
				if (line.Contains("\"\"")) {
					line_string_open = !line_string_open;
				}
				if (line.EndsWith("§")) {
					base_string += line.Substring(0, line.Length - 1);
					goto END;
				}
				if (line_string_open) {
					base_string += line + FileReader.placehloder;
					goto END;
				}
				txt_lines2 [i] = base_string + line;
				base_string = String.Empty;
				END:;
			}
			//Debug.Log(string.Join("\n", txt_lines2));

			//Identifiing empty lines and new commands
			foreach (string line in txt_lines2) {
				if (line == String.Empty) txt_size--;
			}

			//Removing empty lines
			string [] act_lines = new string[txt_size];
			int index2 = 0;
			for (int i = 0; i < txt_lines2.Length; i++) {
				if (txt_lines2 [i].Length != 0 || txt_lines2 [i].StartsWith(">")) {
					act_lines [index2++] = txt_lines2 [i];
					//act_lines[index2] = txt_lines2[i].Replace('\t', ' ').Replace(" ", "");
				}
			}

			DataStructure res = AnalyzeStructure(act_lines, file_name, 0, name, parent);
			return res;
		}

		private static DataStructure AnalyzeStructure (string [] txt_lines, string file_name, int beginning_line, string name = "data", DataStructure parent = null) {
			//For each command, do this:
			DataStructure res = new DataStructure (name, parent);
			int current_line = 0;
			bool still_running = true;
			uint counter = 0u;
			while (still_running) {
				int safty_counter = 0;

				if (current_line >= txt_lines.Length) {
					return res;
				}
				if (txt_lines [current_line] == null) {
					return res;
				}

				//For each item in the command, do this
				string line = txt_lines [current_line];

				if (line.StartsWith(">")) {
					// names are analyzed for special characters too
					string ch_name = String.Format("{0:0000}{1}", counter++, FileReader.ParseString(line.Substring(1)));
					byte recursion = 0x01;
					List<string> _lines_ = new List<string>();
					int child_beginning_line = current_line + 1;
					for (current_line++; recursion > 0; current_line++) {
						// Check the size of the DataStructure
						if (current_line >= txt_lines.Length) {
							Throw("Not matching \">\" with \"<\" ", file_name, current_line + beginning_line);
						} else if (txt_lines [current_line].StartsWith(">")) {
							recursion++;
						} else if (txt_lines [current_line].StartsWith("<")) {
							recursion--;
						}
						_lines_.Add(txt_lines [current_line]);
					}
					AnalyzeStructure(_lines_.ToArray(), file_name, child_beginning_line + beginning_line, ch_name, res);
					goto NOVALUE;
				}
				if (line.EndsWith("<")) {
					still_running = false;
				}

				if (!still_running) goto NOVALUE;
				//Get the type:
				string indicator_type;
				try {
					indicator_type = line.Substring(0, 3);
				} catch (ArgumentOutOfRangeException) {
					indicator_type = "";
				}

				//Get the number, if array
				int count = 0;
				bool isarray = false;
				if (line.Length < 4) Throw("index (3) out of range", file_name, current_line + beginning_line);
				if (line [3] == '*') {
					isarray = true;
					string written_count = "0";
					if (!line.Contains("-")) Throw("must contain '-'", file_name, current_line + beginning_line);
					try {
						written_count = line.Substring(4, line.IndexOf('-') - 4);
					} catch (ArgumentOutOfRangeException) {
						Throw(line + " is not valid", file_name, current_line + beginning_line);
					}
					try {
						count = Int16.Parse(written_count);
					} catch (FormatException) {
						Throw(written_count + " cannot be interpreted as an integer", file_name, current_line + beginning_line);
					}
				}

				//If there is an array
				if (isarray) {
					string item_name = FileReader.ParseString(String.Format("{0}", line.Substring(line.IndexOf("-") + 1, line.IndexOf("=") - line.IndexOf("-") - 1)));
					string[] content = new string[count];
					for (int j = 0; j < count; j++) {
						content [j] = txt_lines [j + current_line + 1];
					}
					switch (indicator_type) {
					case "int":
						int [] temp_arr_int = new int[count];
						for (int j = 0; j < count; j++) {
							temp_arr_int [j] = FileReader.ParseInt(content [j], file_name, beginning_line + current_line);
						}
						res.integers_arr [item_name] = temp_arr_int;
						break;

					case "snt":
						UInt16 [] temp_arr_uint = new UInt16[count];
						for (int j = 0; j < count; j++) {
							temp_arr_uint [j] = FileReader.ParseShort(content [j], file_name, beginning_line + current_line);
						}
						res.short_integers_arr [item_name] = temp_arr_uint;
						break;

					case "lng":
						UInt64 [] temp_arr_ulong = new UInt64[count];
						for (int j = 0; j < count; j++) {
							temp_arr_ulong [j] = FileReader.ParseLong(content [j], file_name, beginning_line + current_line);
						}
						res.long_integers_arr [item_name] = temp_arr_ulong;
						break;

					case "f32":
						float [] temp_arr_32 = new float[count];
						for (int j = 0; j < count; j++) {
							temp_arr_32 [j] = FileReader.ParseFloat(content [j], file_name, beginning_line + current_line);
						}
						res.floats32_arr [item_name] = temp_arr_32;
						break;

					case "f64":
						double [] temp_arr_64 = new double[count];
						for (int j = 0; j < count; j++) {
							temp_arr_64 [j] = FileReader.ParseDouble(content [j], file_name, beginning_line + current_line);
						}
						res.floats64_arr [item_name] = temp_arr_64;
						break;

					case "vc3":
						res.vectors_arr [item_name] = FileReader.ParseVector(content);
						break;

					case "rot":
						Vector3 [] rotation_vecs = FileReader.ParseVector(content);
						Quaternion [] temp_rotations = new Quaternion[rotation_vecs.Length];
						for (int j = 0; j < rotation_vecs.Length; j++) {
							temp_rotations [j] = Quaternion.Euler(rotation_vecs [j]);
						}
						res.rotations_arr [item_name] = temp_rotations;
						break;

					case "chr":
						res.strings_arr [item_name] = Array.ConvertAll(content, l => FileReader.ParseString(l));
						break;

					case "bit":
						bool [] temp_arr_bit = new bool[count];
						for (int j = 0; j < count; j++) {
							temp_arr_bit [j] = content [j] == "1";
						}
						res.booleans_arr [item_name] = temp_arr_bit;
						break;

					case "prf":
						DSPrefab [] temp_arr_pref = new DSPrefab[count];
						for (int j = 0; j < count; j++) {
							temp_arr_pref [j] = new DSPrefab(FileReader.ParseString(content [j]));

						}
						res.prefabs_arr [item_name] = temp_arr_pref;
						break;

					case "img":
						DSImage[] temp_arr_img = new DSImage[count];
						for (int j = 0; j < count; j++) {
							temp_arr_img [j] = new DSImage(FileReader.ParseString(content [j]));
						}
						res.images_arr [item_name] = temp_arr_img;
						break;

					case "dat":
						for (int j = 0; j < count; j++) {
							Load(FileReader.ParseString(content [j]), j.ToString(item_name + "000"), res);
						}
						break;

					default:
						Throw(string.Format("There is no prefix {0}", indicator_type), file_name, current_line + beginning_line);
						break;
					}
					current_line += count + 1;
				}
				//If there is a single item
				else {
					string [] item_split = line.Substring(3).Split('=');
					string item_name = FileReader.ParseString(item_split[0]);
					string content;
					if (item_split.Length < 2) {
						indicator_type = "";
						content = "";
					} else {
						content = item_split [1];
					}
					switch (indicator_type) {
					case "int":
						res.integers [item_name] = FileReader.ParseInt(content, file_name, beginning_line + current_line);
						break;

					case "snt":
						res.short_integers [item_name] = FileReader.ParseShort(content, file_name, beginning_line + current_line);
						break;

					case "lng":
						res.long_integers [item_name] = FileReader.ParseShort(content, file_name, beginning_line + current_line);
						break;

					case "f32":
						res.floats32 [item_name] = FileReader.ParseFloat(content, file_name, beginning_line + current_line);
						break;

					case "f64":
						res.floats64 [item_name] = FileReader.ParseDouble(content, file_name, beginning_line + current_line);
						break;

					case "vc3":
						string [] str_arr = new string [1] {content};
						Vector3[] vec_arr = FileReader.ParseVector(str_arr);
						res.vectors [item_name] = vec_arr [0];
						break;

					case "rot":
						string [] str_arr_ = new string [1] {content};
						Vector3[] vec_arr_ = FileReader.ParseVector(str_arr_);
						res.rotations [item_name] = Quaternion.Euler(vec_arr_ [0]);
						break;

					case "chr":
						res.strings [item_name] = FileReader.ParseString(content);
						break;

					case "bit":
						res.booleans [item_name] = content == "1";
						break;

					case "prf":
						res.prefabs [item_name] = new DSPrefab(FileReader.ParseString(content));
						break;

					case "img":
						res.images [item_name] = new DSImage(FileReader.ParseString(content));
						break;

					case "dat":
						Load(FileReader.ParseString(content), item_name, res);
						break;

					default:
						Throw(string.Format("There is no prefix \"{0}\"", indicator_type), file_name, current_line + beginning_line);
						break;
					}
					current_line++;
				}

				NOVALUE:
				// Safty stuff
				if (++safty_counter >= 100000) {
					Throw("Infinite loop", name, current_line);
					return res;
				}
			}
			return res;
		}

		/// <summary>
		///		Loads a datastructure as a file
		/// </summary>
		/// <param name="path"> The file name and directory </param>
		/// <param name="is_general"> If the filepath is a full filepath (true), or just the path inside the configs </param>
		/// <returns> The DataStructure, null if the file does not exists </returns>
		public static DataStructure Load (string path, string name = "data", DataStructure parent = null, bool is_general = false) {
			string [] lines = new string [0];
			string full_path = (is_general ? String.Empty : GeneralPath) + path + (is_general ? String.Empty : "." + extension);
			if (FileReader.ReadLines(full_path, ref lines)) {
				return AnalyzeText(lines, path, name, parent);
			}
			DeveloppmentTools.Log("Could not read: " + full_path);
			return null;
		}

		public static DataStructure LoadFromDir (string dir_path, string name = "data", DataStructure parent = null) {
			DataStructure res = new DataStructure(name, parent);
			string[] fnames = new string[0];
			string [][] docs = FileReader.AllFilesInDir(GeneralPath + dir_path, out fnames);
			for (int i = 0; i < fnames.Length; i++) {
				res += AnalyzeText(docs [i], fnames [i], name, parent);
			}
			return res;
		}

		/// <summary> Saves a datastructure as a file </summary>
		///  <param name="path"> The file name and directory </param>
		/// <param name="is_general"> If the filepath is a full filepath (true), or just the path inside the configs </param>
		/// <returns> True if saved sucessfully </returns>
		public bool Save (string path, bool is_general = false) {
			File.WriteAllLines((is_general ? string.Empty : GeneralPath) + path + (is_general ? String.Empty : "." + extension), ToText());
			return true;
		}

		public string [] ToText (bool orig = true) {
			List<string> lines = RecursionDepth == 0 ?
			new List<string>() { "//This is a computer-generated code" }
			: new List<string>();

			lines.AddRange(ToText(integers, "int"));
			lines.AddRange(ToText(short_integers, "snt"));
			lines.AddRange(ToText(long_integers, "lng"));
			lines.AddRange(ToText(floats32, "f32"));
			lines.AddRange(ToText(floats64, "f64"));
			lines.AddRange(ToText(vectors, "vc3"));
			lines.AddRange(ToText(rotations, "rot"));
			lines.AddRange(ToText(strings, "chr"));
			lines.AddRange(ToText(booleans, "bit"));
			lines.AddRange(ToText(prefabs, "prf"));
			lines.AddRange(ToText(images, "img"));

			lines.AddRange(ToText(integers_arr, "int"));
			lines.AddRange(ToText(short_integers_arr, "snt"));
			lines.AddRange(ToText(long_integers_arr, "lng"));
			lines.AddRange(ToText(floats32_arr, "f32"));
			lines.AddRange(ToText(floats64_arr, "f64"));
			lines.AddRange(ToText(vectors_arr, "vc3"));
			lines.AddRange(ToText(rotations_arr, "rot"));
			lines.AddRange(ToText(strings_arr, "chr"));
			lines.AddRange(ToText(booleans_arr, "bit"));
			lines.AddRange(ToText(prefabs_arr, "prf"));
			lines.AddRange(ToText(images_arr, "img"));

			foreach (DataStructure child in children.Values) {
				lines.Add(">" + child.Name.Replace(' ', '_'));
				lines.AddRange(child.ToText(false));
				lines.Add("<");
			}

			string[] res_arr = new string[lines.Count];
			for (int i = 0; i < lines.Count; i++) {
				res_arr [i] = (orig ? "" : "\t") + lines [i];
			}

			return res_arr;
		}

		private string ToDSString<T> (T value, bool ind = false) {
			switch (value.GetType().Name) {
			case "Vector3":
				Vector3 vec = (Vector3) (object) value;
				return string.Format("{0:0.0000}, {1:0.0000}, {2:0.0000}", vec.x, vec.y, vec.z);
			case "Quaternion":
				Vector3 rvec = ((Quaternion) (object) value).eulerAngles;
				return string.Format("{0:0.0000}, {1:0.0000}, {2:0.0000}", rvec.x, rvec.y, rvec.z);
			case "Boolean":
				return (bool) (object) value ? "1" : "0";
			case "String":
				return (value as string).Replace(@"\", @"\\").Replace("_", @"\_").Replace(' ', '_');
			default:
				return value.ToString().Replace(@"\", @"\\").Replace("_", @"\_").Replace(' ', '_');
			}
		}

		private List<string> ToText<T> (Dictionary<string, T> dict, string prefix) {
			List<string> lines = new List<string>();
			foreach (KeyValuePair<string, T> pair in dict) {
				string _name = ToDSString(pair.Key);
				try {
					lines.Add(string.Format("{0} {1} = {2}", prefix, _name, ToDSString(pair.Value)));
				} catch (NullReferenceException) {
					Throw(_name + " is not an instance", "", 0);
				}
			}
			return lines;
		}

		private List<string> ToText<T> (Dictionary<string, T []> dict, string suffix) {
			List<string> lines = new List<string>();
			foreach (KeyValuePair<string, T []> pair in dict) {
				string _name = ToDSString(pair.Key);
				lines.Add(string.Format("{0}*{1}-{2} = ", suffix, pair.Value.Length, _name));
				foreach (object single_item in pair.Value) {
					lines.Add(ToDSString(single_item, true));
				}
			}
			return lines;
		}

		public T Get<T> (string name, T def = default(T), bool test = false, bool quiet = false) {
			return (T) Get(name, typeof(T), def, test: test, quiet: quiet);
		}

		public object Get (string name, Type type, object def = null, bool test = false, bool quiet = false) {
			try {
				switch (type.Name) {
				case "Int32": return integers [name];
				case "UInt16": return short_integers [name];
				case "UInt64": return long_integers [name];
				case "Single": return floats32 [name];
				case "Double": return floats64 [name];
				case "Vector3": return vectors [name];
				case "Quaternion": return rotations [name];
				case "String": return strings [name];
				case "Boolean": return booleans [name];
				case "DSPrefab": return prefabs [name];
				case "GameObject": return prefabs [name].obj;
				case "Texture2D": return images [name].texture;
				case "DataStructure": return GetChild(name);
				case "Int32[]": return integers_arr [name];
				case "UInt16[]": return short_integers_arr [name];
				case "UInt64[]": return long_integers_arr [name];
				case "Single[]": return floats32_arr [name];
				case "Double[]": return floats64_arr [name];
				case "Vector3[]": return vectors_arr [name];
				case "Quaternion[]": return rotations_arr [name];
				case "String[]": return strings_arr [name];
				case "Boolean[]": return booleans_arr [name];
				case "DSPrefab[]": return prefabs_arr [name];
				case "GameObject[]": return Array.ConvertAll(prefabs_arr [name], x => x.obj);
				case "Texture2D[]": return Array.ConvertAll(images_arr [name], x => x.texture);
				}
			} catch (Exception e) {
				if (quiet) goto RETURNDEFAULT;
				if (e is KeyNotFoundException) {
					if (test) throw e;
					else { DeveloppmentTools.LogFormat("No {0} named {1} present", type.Name, name); }
					goto RETURNDEFAULT;
				} else if (e is NullReferenceException) {
					DeveloppmentTools.LogFormat("{0} is not an instance", name);
				} else DeveloppmentTools.LogFormat("Error occured: {0}", e.Message);
			}
			DeveloppmentTools.LogFormat("Type {0} is not valid", type.Name);
			RETURNDEFAULT:
			if (Equals(def, null))
				return Globals.defaults.Get("D", type, null);
			return def;
		}

		public void Set<T> (string name, T value) {
			try {
				switch (typeof(T).Name) {
				case "Int32":
					if (integers.ContainsKey(name)) integers [name] = (int) (object) value;
					else integers.Add(name, (int) (object) value);
					return;
				case "UInt16":
					if (short_integers.ContainsKey(name)) short_integers [name] = (ushort) (object) value;
					else short_integers.Add(name, (ushort) (object) value);
					return;
				case "UInt64":
					if (long_integers.ContainsKey(name)) long_integers [name] = (ulong) (object) value;
					else long_integers.Add(name, (ulong) (object) value);
					return;
				case "Single":
					if (floats32.ContainsKey(name)) floats32 [name] = (float) (object) value;
					else floats32.Add(name, (float) (object) value);
					return;
				case "Double":
					if (floats64.ContainsKey(name)) floats64 [name] = (double) (object) value;
					else floats64.Add(name, (double) (object) value);
					return;
				case "Vector3":
					if (vectors.ContainsKey(name)) vectors [name] = (Vector3) (object) value;
					else vectors.Add(name, (Vector3) (object) value);
					return;
				case "Quaternion":
					if (rotations.ContainsKey(name)) rotations [name] = (Quaternion) (object) value;
					else rotations.Add(name, (Quaternion) (object) value);
					return;
				case "String":
					if (strings.ContainsKey(name)) strings [name] = value as string;
					else strings.Add(name, value as string);
					return;
				case "Boolean":
					if (booleans.ContainsKey(name)) booleans [name] = (bool) (object) value;
					else booleans.Add(name, (bool) (object) value);
					return;
				case "DSPrefab":
					if (prefabs.ContainsKey(name)) prefabs [name] = (DSPrefab) (object) value;
					else prefabs.Add(name, (DSPrefab) (object) value);
					return;
				case "GameObject":
					if (prefabs.ContainsKey(name)) {
						DSPrefab prf = prefabs[name];
						prf.obj = (GameObject) (object) value;
						prefabs [name] = prf;
					} else {
						DeveloppmentTools.Log("Cannot assign new Prefab without path");
					}
					return;
				case "DataStructure":
					if (children.ContainsKey(name)) children [name] = (DataStructure) (object) value;
					else children.Add(name, (DataStructure) (object) value);
					return;
				case "Texture2D":
					if (images.ContainsKey(name)) {
						DSImage img = images[name];
						img.texture = (Texture2D) (object) value;
						images [name] = img;
					}
					return;
				case "Int32[]":
					if (integers_arr.ContainsKey(name)) integers_arr [name] = (int []) (object) value;
					else integers_arr.Add(name, (int []) (object) value);
					return;
				case "UInt16[]":
					if (short_integers_arr.ContainsKey(name)) short_integers_arr [name] = (ushort []) (object) value;
					else short_integers_arr.Add(name, (ushort []) (object) value);
					return;
				case "UInt64[]":
					if (long_integers_arr.ContainsKey(name)) long_integers_arr [name] = (ulong []) (object) value;
					else long_integers_arr.Add(name, (ulong []) (object) value);
					return;
				case "Single[]":
					if (floats32_arr.ContainsKey(name)) floats32_arr [name] = (float []) (object) value;
					else floats32_arr.Add(name, (float []) (object) value);
					return;
				case "Double[]":
					if (floats64_arr.ContainsKey(name)) floats64_arr [name] = (double []) (object) value;
					else floats64_arr.Add(name, (double []) (object) value);
					return;
				case "Vector3[]":
					if (vectors_arr.ContainsKey(name)) vectors_arr [name] = (Vector3 []) (object) value;
					else vectors_arr.Add(name, (Vector3 []) (object) value);
					return;
				case "Quaternion[]":
					if (rotations_arr.ContainsKey(name)) rotations_arr [name] = (Quaternion []) (object) value;
					else rotations_arr.Add(name, (Quaternion []) (object) value);
					return;
				case "String[]":
					if (strings_arr.ContainsKey(name)) strings_arr [name] = (string []) (object) value;
					else strings_arr.Add(name, (string []) (object) value);
					return;
				case "Boolean[]":
					if (booleans_arr.ContainsKey(name)) booleans_arr [name] = (bool []) (object) value;
					else booleans_arr.Add(name, (bool []) (object) value);
					return;
				case "DSPrefab[]":
					if (prefabs_arr.ContainsKey(name)) prefabs_arr [name] = (DSPrefab []) (object) value;
					else prefabs_arr.Add(name, (DSPrefab []) (object) value);
					return;
				case "GameObject[]":
					if (prefabs_arr.ContainsKey(name)) {
						DSPrefab[] prf_arr = prefabs_arr [name];
						GameObject[] obj_arr = (GameObject[]) (object) value;
						if (prf_arr.Length != obj_arr.Length)
							Throw("GameObject array has to be the same size as previous array", "", 0);
						for (int i = 0; i < prf_arr.Length; i++)
							prf_arr [i].obj = obj_arr [i];
						prefabs_arr [name] = prf_arr;
					} else new NotImplementedException("Cannot assign new prefabs without path");
					return;
				case "Texture2D[]":
					if (images_arr.ContainsKey(name)) {
						DSImage[] img_arr = images_arr [name];
						Texture2D[] obj_arr = (Texture2D[]) (object) value;
						if (img_arr.Length != obj_arr.Length)
							Throw("Texture array has to be the same size as previous array", "", 0);
						for (int i = 0; i < img_arr.Length; i++)
							img_arr [i].texture = obj_arr [i];
						images_arr [name] = img_arr;
					} else new NotImplementedException("Cannot assign new images without path");
					return;
				}
			} catch (Exception e) {
				if (e is KeyNotFoundException) {
					DeveloppmentTools.LogFormat("No {0} named {1} present", typeof(T).Name, name);
				} else if (e is NullReferenceException) {
					DeveloppmentTools.LogFormat("{0} is not an instance", name);
				} else {
					DeveloppmentTools.Log(e.Message);
				}
			}
			DeveloppmentTools.LogFormat("Type {0} is not valid", typeof(T).Name);
		}

		/// <summary>
		///		Returns true, if a certain item is in the Datastructure
		/// </summary>
		/// <typeparam name="T"> the Type of the searched item </typeparam>
		/// <param name="name"> The name of the item </param>
		public bool Contains<T> (string name) {
			T res;
			try {
				res = Get(name, default(T), true);
			} catch {
				return false;
			}
			if (res == null) return false;
			return true;
		}

		/// <summary>
		///		Returns true, if a certain item is in the Datastructure
		/// </summary>
		/// <param name="name"> The name of the item </param>
		/// <param name="type"> The Type class of the object </param>
		public bool Contains (string name, Type type) {
			object res;
			try {
				res = Get(name, type: type, test: true);
			} catch {
				return false;
			}
			if (res == null) return false;
			return true;
		}

		/// <summary>
		///		trys to find a child with a given name
		/// </summary>
		/// <param name="child_name"> name of the child to find </param>
		/// <returns> the child, or null if not present </returns>
		public DataStructure GetChild (string child_name) {
			foreach (DataStructure child in children.Values) {
				if (child.Name == child_name) {
					return child;
				}
			}
			return null;
		}

		/// <summary>
		///		Trys to find at least one child with a given name.
		///		Returns an array of all the children with the given name
		/// </summary>
		/// <param name="child_name"> array of names of the children to be found </param>
		/// <returns> An array with all the children; can be empty </returns>
		public DataStructure [] GetAllChildren (string [] child_names) {
			List<DataStructure> res = new List<DataStructure>();
			foreach (DataStructure child in children.Values) {
				if (Array.Exists(child_names, x => child.Name == x)) {
					res.Add(child);
				}
			}
			return res.ToArray();
		}

		/// <summary>
		///		Trys to find at least one child with a given name.
		///		Returns an array of all the children with the given name
		/// </summary>
		/// <param name="child_name"> name of the child to be found </param>
		/// <returns> An array with all the children; can be empty </returns>
		public DataStructure [] GetAllChildren (string child_name) {
			return GetAllChildren(new string [] { child_name });
		}

		/// <summary>
		///		Recursive function, which checks, if a item with a given name
		///		is contained somewhere in the DataStructure.
		///		Returns true, if it is, false if it isn't.
		/// </summary>
		/// <param name="item_name">the name of the item to search</param>
		/// <returns>boolean (is the dataq with the name present?)</returns>
		public bool ContainsChild (string item_name) {
			foreach (DataStructure child in children.Values) {
				if (child.Name == item_name) { return true; }
			}
			foreach (DataStructure chi in AllChildren) {
				if (chi.ContainsChild(item_name)) {
					return true;
				}
			}
			return false;
		}

		public static void Throw (string message, string file, int line) {
			DeveloppmentTools.LogFormat("Error at {0}, line {1}: {2}", file, line.ToString(), message);
		}

		public static Dictionary<Tkey, Tvalue> MergeDict<Tkey, Tvalue> (Dictionary<Tkey, Tvalue> dict1, Dictionary<Tkey, Tvalue> dict2) {
			Dictionary<Tkey, Tvalue> res = new Dictionary<Tkey, Tvalue>();
			foreach (KeyValuePair<Tkey, Tvalue> pair in dict1) {
				res.Add(pair.Key, pair.Value);
			}
			foreach (KeyValuePair<Tkey, Tvalue> pair in dict2) {
				if (!res.ContainsKey(pair.Key)) {
					res.Add(pair.Key, pair.Value);
				}
			}
			return res;
		}

		/// <summary>
		///		Nicely formats the content of the datastructure into a string
		/// </summary>
		/// <returns> a nice string </returns>
		public override string ToString () {
			string str = String.Empty;
			string pre_spaces = String.Empty;
			for (int i = 0; i < RecursionDepth - 1; i++) { pre_spaces += "  "; }
			str += string.Format("{1}{0}\n{1}---------------------\n", Name, pre_spaces);
			pre_spaces += "  ";
			foreach (KeyValuePair<string, int> item in integers) str += pre_spaces + item.Key + " = " + item.Value.ToString() + "\n";
			foreach (KeyValuePair<string, int []> item in integers_arr) str += pre_spaces + item.Key + " = (" + String.Join(", ", Array.ConvertAll(item.Value, i => i.ToString())) + ")\n";
			foreach (KeyValuePair<string, UInt16> item in short_integers) str += pre_spaces + item.Key + " = " + item.Value.ToString() + "\n";
			foreach (KeyValuePair<string, UInt16 []> item in short_integers_arr) str += pre_spaces + item.Key + " = (" + String.Join(", ", Array.ConvertAll(item.Value, i => i.ToString())) + ")\n";
			foreach (KeyValuePair<string, UInt64> item in long_integers) str += pre_spaces + item.Key + " = " + item.Value.ToString() + "\n";
			foreach (KeyValuePair<string, UInt64 []> item in long_integers_arr) str += pre_spaces + item.Key + " = (" + String.Join(", ", Array.ConvertAll(item.Value, i => i.ToString())) + ")\n";
			foreach (KeyValuePair<string, float> item in floats32) str += pre_spaces + item.Key + " = " + item.Value.ToString() + "\n";
			foreach (KeyValuePair<string, float []> item in floats32_arr) str += pre_spaces + item.Key + " = (" + String.Join(", ", Array.ConvertAll(item.Value, i => i.ToString())) + ")\n";
			foreach (KeyValuePair<string, double> item in floats64) str += pre_spaces + item.Key + " = " + item.Value.ToString() + "\n";
			foreach (KeyValuePair<string, double []> item in floats64_arr) str += pre_spaces + item.Key + " = (" + String.Join(", ", Array.ConvertAll(item.Value, i => i.ToString())) + ")\n";
			foreach (KeyValuePair<string, Vector3> item in vectors) str += pre_spaces + item.Key + " = " + item.Value.ToString() + "\n";
			foreach (KeyValuePair<string, Vector3 []> item in vectors_arr) str += pre_spaces + item.Key + " = (" + String.Join(", ", Array.ConvertAll(item.Value, i => i.ToString())) + ")\n";
			foreach (KeyValuePair<string, Quaternion> item in rotations) str += pre_spaces + item.Key + " = " + item.Value.eulerAngles.ToString() + "\n";
			foreach (KeyValuePair<string, Quaternion []> item in rotations_arr) str += pre_spaces + item.Key + " = (" + String.Join(", ", Array.ConvertAll(item.Value, i => i.eulerAngles.ToString())) + ")\n";
			foreach (KeyValuePair<string, string> item in strings) str += pre_spaces + item.Key + " = " + item.Value.ToString() + "\n";
			foreach (KeyValuePair<string, string []> item in strings_arr) str += pre_spaces + item.Key + " = (" + String.Join(", ", Array.ConvertAll(item.Value, i => i.ToString())) + ")\n";
			foreach (KeyValuePair<string, bool> item in booleans) str += pre_spaces + item.Key + " = " + item.Value.ToString() + "\n";
			foreach (KeyValuePair<string, bool []> item in booleans_arr) str += pre_spaces + item.Key + " = (" + String.Join(", ", Array.ConvertAll(item.Value, i => i.ToString())) + ")\n";
			foreach (KeyValuePair<string, DSPrefab> item in prefabs) str += pre_spaces + item.Key + " = " + item.Value.ToString() + "\n";
			foreach (KeyValuePair<string, DSPrefab []> item in prefabs_arr) str += pre_spaces + item.Key + " = (" + String.Join(", ", Array.ConvertAll(item.Value, i => i.ToString())) + ")\n";
			foreach (KeyValuePair<string, DSImage> item in images) str += pre_spaces + item.Key + " = " + item.Value.ToString() + "\n";
			foreach (KeyValuePair<string, DSImage []> item in images_arr) str += pre_spaces + item.Key + " = (" + String.Join(", ", Array.ConvertAll(item.Value, i => i.ToString())) + ")\n";
			foreach (KeyValuePair<string, DataStructure> item in children) str += "\n" + item.Value.ToString() + "\n";
			return str;
		}

		public static string Collectable2String<T> (ICollection<T> coll) {
			string res = "(";
			foreach (T item in coll) {
				res += item.ToString() + ", ";
			}
			res += ")";
			return res;
		}

		public static DataStructure operator + (DataStructure a, DataStructure b) {
			DataStructure res = new DataStructure(a._name, a.Parent){
				integers = MergeDict(a.integers, b.integers),
				short_integers = MergeDict(a.short_integers, b.short_integers),
				long_integers = MergeDict(a.long_integers, b.long_integers),
				floats32 = MergeDict(a.floats32, b.floats32),
				floats64 = MergeDict(a.floats64, b.floats64),
				vectors = MergeDict(a.vectors, b.vectors),
				rotations = MergeDict(a.rotations, b.rotations),
				strings = MergeDict(a.strings, b.strings),
				booleans = MergeDict(a.booleans, b.booleans),
				prefabs = MergeDict(a.prefabs, b.prefabs),
				images = MergeDict(a.images, b.images),
				children = MergeDict(a.children, b.children),

				integers_arr = MergeDict(a.integers_arr, b.integers_arr),
				short_integers_arr = MergeDict(a.short_integers_arr, b.short_integers_arr),
				long_integers_arr = MergeDict(a.long_integers_arr, b.long_integers_arr),
				floats32_arr = MergeDict(a.floats32_arr, b.floats32_arr),
				floats64_arr = MergeDict(a.floats64_arr, b.floats64_arr),
				vectors_arr = MergeDict(a.vectors_arr, b.vectors_arr),
				rotations_arr = MergeDict(a.rotations_arr, b.rotations_arr),
				strings_arr = MergeDict(a.strings_arr, b.strings_arr),
				booleans_arr = MergeDict(a.booleans_arr, b.booleans_arr),
				prefabs_arr = MergeDict(a.prefabs_arr, b.prefabs_arr),
				images_arr = MergeDict(a.images_arr, b.images_arr),
			};
			return res;
		}
	}

	public struct DSPrefab
	{
		public string path;
		public GameObject obj;

		public DSPrefab (string ppath) {
			path = ppath;
			obj = Resources.Load(path) as GameObject;
			if (obj == null) {
				DeveloppmentTools.Log(path + " not found");
			}
		}

		public override string ToString () {
			return path;
		}

		public static implicit operator GameObject (DSPrefab prf) {
			return prf.obj;
		}

		public static implicit operator DSPrefab (GameObject obj) {
			DeveloppmentTools.Log("Cannot directly cast to dsprefab");
			throw new NotImplementedException();
		}
	}

	public struct DSImage
	{
		public string path;
		public Texture2D texture;

		public DSImage (string ppath) {
			path = ppath;
			string act_path = DataStructure.GeneralPath + ppath + ".png";
			texture = new Texture2D(0, 0);
			if (ppath == String.Empty) {
				return;
			}
			if (File.Exists(act_path)) {
				byte[] binary_data = File.ReadAllBytes(act_path);
				texture.LoadImage(binary_data);
			} else {
				DeveloppmentTools.Log(act_path + " not found");
			}
		}

		public override string ToString () {
			return path;
		}

		public static implicit operator Texture2D (DSImage prf) {
			return prf.texture;
		}

		public static implicit operator DSImage (Texture obj) {
			DeveloppmentTools.Log("Cannot directly cast to dsimage");
			throw new NotImplementedException();
		}

		public static bool operator == (DSImage lhs, DSImage rhs) {
			return lhs.path == rhs.path;
		}

		public static bool operator != (DSImage lhs, DSImage rhs) {
			return lhs.path != rhs.path;
		}
	}
}