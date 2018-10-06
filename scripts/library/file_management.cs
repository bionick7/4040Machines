using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

// C:\Users\Nick\AppData\LocalLow\DefaultCompany\spacesim

/*
 * Here you will find anything, that helps you to read files and store data
 */

/// <summary>
///		Contains methods for Filereading/-writing
/// </summary>
public static class FileReader {
	public static string logfile;

    //Reads lines from a given path into a string array (and gives length into an int) returns true if it was succesfull
	public static bool ReadLines (string path, ref string [] in_txt) {
		if (File.Exists(path)){
			in_txt = File.ReadAllLines(path);
		}
		else {
			in_txt = new string[0];
			return false;
		}
		return true;
	}

    ///<summary> Trys to read vectors from a string array </summary>
	///<param name="txt_lines"> The lines of text, containing the vectors </param>
	///<param name="sep"> The sysmbol, with wich the numbers of the vector are seperated, default is ',' </param>
	///<returns> An array of te vectors </returns>
	public static Vector3 [] AnalyzeVector (string [] txt_lines, char[] sep = null){
		sep = sep ?? new char[1] { ',' };
		int length = txt_lines.Length;
		Vector3[] result = new Vector3[length];
		for (int i=0; i < length; i++){
			if (txt_lines [i] == "0") result [i] = Vector3.zero;
			else {
				string [] txt = txt_lines[i].Split(sep);
				if (txt.Length != 3) {
					//Debug.LogErrorFormat("Error at line {0}: {1} only 3 Values permitted", i, txt_lines[i]);
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

    /// <summary> Reads vectors straight from a file </summary>
	/// <param name="path"> The path of the file containing the Vectors </param>
	/// <returns> An array of the Vectors </returns>
	public static Vector3 [] ReadVectors (string path){
		string [] txt_lines = new string[0];
		if (!ReadLines(path, ref txt_lines)){
			return new Vector3 [0];
		}
		Vector3 [] result = AnalyzeVector(txt_lines);
		return result;
	}

	/// <param name="path"> The path of the directory (without "\") </param>
	/// <returns> All the files in a directory as an array </returns>
	public static string[][] AllFilesInDir(string path, out string[] fnames) {
		string[] file_paths = Directory.GetFiles(path + "/");
		List<string[]> end_arr = new List<string[]>();
		List<string> fnames_list = new List<string>();
		for (int i=0; i < file_paths.Length; i++) {
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

	public static string AnalyzeString (string input) {
		string output = input.Replace("\\n", "\n").Replace('_', ' ').Replace("\\_", "_").Replace("\\\\", "\\");
		return output;
	}

	/// <summary> Logs a line to the logfile </summary>
	/// <param name="log"> line to be logged </param>
	public static void FileLog (string log) {
		string [] curr_content = File.ReadAllLines(logfile);
		string [] new_cont = new string [curr_content.Length + 1];
		for (int i = 0; i < curr_content.Length; i++) new_cont [i] = curr_content [i];
		new_cont [curr_content.Length] = log;
		File.WriteAllLines(logfile, new_cont);
	}
}


[Serializable]
public class DSScriptException : Exception
{
	public DSScriptException (string message, string file, int line) : base(string.Format("Error at {0}, line {1}: {2}", file, line, message)) { }
}

/// <summary>
///		Means to store data
/// </summary>
public class DataStructure {

	//DataStructure is our system to store data.
	//It can be applicated recoursively

	public static string GeneralPath {
		get{
			string[] dir_split = Application.dataPath.Split('/');
			string last_path = dir_split[dir_split.Length - 1];
			return Application.dataPath.Substring(0, Application.dataPath.Length - last_path.Length) + "configs/";
		}
	}

	public static readonly DataStructure Empty = new DataStructure("empty");

	private string _name;
	/// <summary> The name of the DataStructure </summary>
	public string Name {
		get {
			if (_name[0] >= 48 && _name[0] <= 57) {
				return _name.Substring(4);
			}
			return _name;
		}
	}

	public DataStructure[] AllChildren {
		get {
			return new List<DataStructure>(children.Values).ToArray();
		}
	}

	/// <summary> The parent Datastructure </summary>
	public DataStructure Parent { get; private set; }

	public uint RecursionDepth {
		get {
			if (Parent == null) {
				return 0u;
			} else {
				return Parent.RecursionDepth + 1u;
			}
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
	public Dictionary<string, Single> floats32 = new Dictionary<string, Single>();
	public Dictionary<string, Double> floats64 = new Dictionary<string, Double>();
	public Dictionary<string, Vector3> vectors = new Dictionary<string, Vector3>();
	public Dictionary<string, Quaternion> rotations = new Dictionary<string, Quaternion> ();
	public Dictionary<string, String> strings = new Dictionary<string, String>();
	public Dictionary<string, Boolean> booleans = new Dictionary<string, Boolean>();
	public Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

    public Dictionary<string, Int32[]> integers_arr = new Dictionary<string, Int32[]>();
    public Dictionary<string, UInt16[]> short_integers_arr = new Dictionary<string, UInt16[]>();
    public Dictionary<string, Single[]> floats32_arr = new Dictionary<string, Single[]>();
    public Dictionary<string, Double[]> floats64_arr = new Dictionary<string, Double[]>();
    public Dictionary<string, Vector3[]> vectors_arr = new Dictionary<string, Vector3[]>();
	public Dictionary<string, Quaternion[]> rotations_arr = new Dictionary<string, Quaternion[]>();
    public Dictionary<string, String[]> strings_arr = new Dictionary<string, String[]>();
    public Dictionary<string, Boolean[]> booleans_arr = new Dictionary<string, Boolean[]>();
    public Dictionary<string, GameObject[]> prefabs_arr = new Dictionary<string, GameObject[]>();

    //New DataStructures can be saved in a DataStructure, allowing for a recursive parents-children systrem.
    public Dictionary<string, DataStructure> children = new Dictionary<string, DataStructure>();
    
	#endregion

	/// <param name="_name">The name of the DataStructure</param>
	/// <param name="depht">the recursion_deth of the Datastructure</param>
	public DataStructure (string name="", DataStructure parent=null) {
		_name = name;
		Parent = parent;
		if (parent != null) {
			if (!parent.children.ContainsKey(_name)) {
				parent.children.Add(_name, this);
			}
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
	///			rot -> rotation in 3D space (as euler rotations, comma seperated=
	///			chr -> string (spaces, will be removed)
	///			bit -> booleans (0 for false or 1 for true)
	///			prf -> gameobject (indicated by path in the resources folder)
	/// </remark>
	public static DataStructure AnalyzeText (string [] txt_lines, string file_name, string name="data", DataStructure parent=null) {
		int txt_size = txt_lines.Length;

        // Eliminate all comments, spaces and Tabs
		// Merges '§'-separated lines
        string[] txt_lines2 = new string[txt_size];
		// Set everything to empty
		for (int i=0; i < txt_size; i++) txt_lines2 [i] = String.Empty;
		string base_string = String.Empty;
		for (int i=0; i < txt_size; i++){
            string line = txt_lines[i]; 
            line = line.Replace('\t', ' ').Replace(" ", String.Empty);
            if (line.Contains("//")) {
                line = line.Substring(0, line.IndexOf("//"));
            }
			if (line.EndsWith("§")) {
				base_string += line.Substring(0, line.Length - 1);
				goto END;
			}
            txt_lines2[i] = base_string + line;
			base_string = String.Empty;
			END:;
        }

		//Identifiing empty lines and new commands
		foreach (string line in txt_lines2){
			if (line == String.Empty) txt_size--;
		}

		//Removing empty lines
		string [] act_lines = new string[txt_size];
		int index2 = 0;
		for (int i=0; i < txt_lines2.Length; i++){
			if (txt_lines2[i].Length != 0 || txt_lines2[i].StartsWith(">")){
				act_lines[index2] = txt_lines2[i].Replace('\t', ' ').Replace(" ", "");
				index2++;
			}
		}

		DataStructure res = AnalyzeStructure(act_lines, file_name, 0, name, parent);
		return res;
	}

	private static DataStructure AnalyzeStructure (string [] txt_lines, string file_name, int beginning_line, string name="data", DataStructure parent = null) {
		//Debug.LogFormat("beginning of {1}: {0}", beginning_line, name);

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
				string ch_name = String.Format("{0:0000}{1}", counter++, FileReader.AnalyzeString(line.Substring(1)));
				byte recursion = 0x01;
				List<string> _lines_ = new List<string>();
				int child_beginning_line = current_line + 1;
				for (current_line++; recursion > 0; current_line++) {
					// Check the size of the DataStructure
					if (current_line >= txt_lines.Length) {
						throw new DSScriptException("Not matching \">\" with \"<\" ", file_name, current_line + beginning_line);
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
			if (line [3] == '*') {
				isarray = true;
				string written_count;
				if (!line.Contains("-")) throw new DSScriptException("must contain '-'", file_name, current_line + beginning_line);
				try {
					written_count = line.Substring(4, line.IndexOf('-') - 4);
				} catch (ArgumentOutOfRangeException e) {
					throw new DSScriptException(line + " is not valid", file_name, current_line + beginning_line);
				}
				try {
					count = Int16.Parse(written_count);
				} catch (FormatException) {
					throw new DSScriptException(written_count + " cannot be interpreted as an integer", file_name, current_line + beginning_line);
				}
			}

			//If there is an array
			if (isarray) {
				string item_name = FileReader.AnalyzeString(String.Format("{0}", line.Substring(line.IndexOf("-") + 1, line.IndexOf("=") - line.IndexOf("-") - 1)));
				string[] content = new string[count];
				for (int j = 0; j < count; j++) {
					content [j] = txt_lines [j + current_line + 1];
				}
				switch (indicator_type) {
					case "int":
						int [] temp_arr_int = new int[count];
						for (int j = 0; j < count; j++) {
							temp_arr_int [j] = Int32.Parse(content [j]);
						}
						res.integers_arr [item_name] = temp_arr_int;
						break;

					case "snt":
						UInt16 [] temp_arr_uint = new UInt16[count];
						for (int j = 0; j < count; j++) {
							temp_arr_uint [j] = UInt16.Parse(content [j]);
						}
						res.short_integers_arr [item_name] = temp_arr_uint;
						break;

					case "f32":
						float [] temp_arr_32 = new float[count];
						for (int j = 0; j < count; j++) {
							temp_arr_32 [j] = Single.Parse(content [j], System.Globalization.CultureInfo.InvariantCulture);
						}
						res.floats32_arr [item_name] = temp_arr_32;
						break;

					case "f64":
						double [] temp_arr_64 = new double[count];
						for (int j = 0; j < count; j++) {
							temp_arr_64 [j] = Double.Parse(content [j], System.Globalization.CultureInfo.InvariantCulture);
						}
						res.floats64_arr [item_name] = temp_arr_64;
						break;

					case "vc3":
						res.vectors_arr [item_name] = FileReader.AnalyzeVector(content);
						break;

					case "rot":
						Vector3 [] rotation_vecs = FileReader.AnalyzeVector(content);
						Quaternion [] temp_rotations = new Quaternion[rotation_vecs.Length];
						for (int j = 0; j < rotation_vecs.Length; j++) {
							temp_rotations [j] = Quaternion.Euler(rotation_vecs [j]);
						}
						res.rotations_arr [item_name] = temp_rotations;
						break;

					case "chr":
						res.strings_arr [item_name] = Array.ConvertAll(content, l => FileReader.AnalyzeString(l));
						break;

					case "bit":
						bool [] temp_arr_bit = new bool[count];
						for (int j = 0; j < count; j++) {
							temp_arr_bit [j] = content [j] == "1";
						}
						res.booleans_arr [item_name] = temp_arr_bit;
						break;

					case "prf":
						GameObject [] temp_arr_pref = new GameObject[count];
						for (int j = 0; j < count; j++) {
							temp_arr_pref [j] = Resources.Load(content [j], typeof(UnityEngine.Object)) as GameObject;
							if (temp_arr_pref [j] == null) {
								throw new FileNotFoundException(content[j] + " not found");
							}
						}
						res.prefabs_arr [item_name] = temp_arr_pref;
						break;

					default:
						throw new DSScriptException(string.Format("There is no prefix {0}", indicator_type), file_name, current_line + beginning_line);
				}
				current_line += count + 1;
			}
			//If there is a single item
			else {
				string [] item_split = line.Substring(3).Split('=');
				string item_name = FileReader.AnalyzeString(item_split[0]);
				string content;
				if (item_split.Length < 2) {
					indicator_type = "";
					content = "";
				} else {
					content = item_split [1];
				}
				switch (indicator_type) {
					case "int":
						int intitem = Int32.Parse(content);
						res.integers [item_name] = intitem;
						break;

					case "snt":
						UInt16 uintitem = UInt16.Parse(content);
						res.short_integers [item_name] = uintitem;
						break;

					case "f32":
						float f32item = 0;
						try {
							f32item = Single.Parse(content, System.Globalization.CultureInfo.InvariantCulture);
						} catch (FormatException) {
							Debug.LogWarning("\"" + content + "\" is not a valid scalar expression");
						}
						res.floats32 [item_name] = f32item;
						break;

					case "f64":
						double f64item = Double.Parse(content, System.Globalization.CultureInfo.InvariantCulture);
						res.floats64 [item_name] = f64item;
						break;

					case "vc3":
						string [] str_arr = new string [1] {content};
						Vector3[] vec_arr = FileReader.AnalyzeVector(str_arr);
						res.vectors [item_name] = vec_arr [0];
						break;

					case "rot":
						string [] str_arr_ = new string [1] {content};
						Vector3[] vec_arr_ = FileReader.AnalyzeVector(str_arr_);
						res.rotations [item_name] = Quaternion.Euler(vec_arr_ [0]);
						break;

					case "chr":
						res.strings [item_name] = FileReader.AnalyzeString(content);
						break;

					case "bit":
						res.booleans [item_name] = content == "1";
						break;

					case "prf":
						GameObject obj = Resources.Load(content, typeof(UnityEngine.Object)) as GameObject;
						if (obj == null) {
							throw new DSScriptException(string.Format("no such file: \"{0}\"", content), file_name, current_line + beginning_line);
						}
						res.prefabs [item_name] = obj;
						break;

					default:
						throw new DSScriptException(string.Format("There is no prefix \"{0}\"", indicator_type), file_name, current_line + beginning_line);
				}
				current_line++;
			}

			NOVALUE:
			// Safty stuff
			if (++safty_counter >= 100000) {
				throw new DSScriptException("Infinite loop", name, current_line);
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
	public static DataStructure Load (string path, string name="data", DataStructure parent=null, bool is_general=false) {
		string [] lines = new string [0];
		if (FileReader.ReadLines((is_general ? string.Empty : GeneralPath) + path, ref lines)) {
			return AnalyzeText(lines, path, name, parent);
		}
		return null;
	}

	public static DataStructure LoadFromDir (string dir_path, string name="data", DataStructure parent=null) {
		DataStructure res = new DataStructure(name, parent);
		string[] fnames = new string[0];
		string [][] docs = FileReader.AllFilesInDir(GeneralPath + dir_path, out fnames);
		for (int i=0; i < fnames.Length; i++) {
			res += AnalyzeText(docs[i], fnames[i], name, parent);
		}
		return res;
	}

	/// <summary>
	///		Saves a datastructure as a file
	/// </summary>
	/// <param name="path"> The file name and directory </param>
	/// <param name="is_general"> If the filepath is a full filepath (true), or just the path inside the configs </param>
	/// <returns> True if saved sucessfully </returns>
	public bool Save (string path, bool is_general=false) {
		File.WriteAllLines((is_general ? string.Empty : GeneralPath) + path, ToText());
		return true;
	}

	public string [] ToText (bool orig=true) {
		List<string> lines = new List<string>();

		lines.AddRange(ToText(integers, "int"));
		lines.AddRange(ToText(short_integers, "snt"));
		lines.AddRange(ToText(floats32, "f32"));
		lines.AddRange(ToText(floats64, "f64"));
		lines.AddRange(ToText(vectors, "vc3"));
		lines.AddRange(ToText(rotations, "rot"));
		lines.AddRange(ToText(strings, "chr"));
		lines.AddRange(ToText(booleans, "bit"));
		lines.AddRange(ToText(prefabs, "prf"));

		lines.AddRange(ToText(integers_arr, "int"));
		lines.AddRange(ToText(short_integers_arr, "snt"));
		lines.AddRange(ToText(floats32_arr, "f32"));
		lines.AddRange(ToText(floats64_arr, "f64"));
		lines.AddRange(ToText(vectors_arr, "vc3"));
		lines.AddRange(ToText(rotations_arr, "rot"));
		lines.AddRange(ToText(strings_arr, "chr"));
		lines.AddRange(ToText(booleans_arr, "bit"));
		lines.AddRange(ToText(prefabs_arr, "prf"));

		foreach (DataStructure child in children.Values) {
			lines.Add(">" + child.Name.Replace(' ', '_'));
			lines.AddRange(child.ToText(false));
			lines.Add("<");
		}

		string[] res_arr = new string[lines.Count];
		for (int i=0; i < lines.Count; i++) {
			res_arr [i] = (orig ? "" : "\t") + lines [i];
		}

		return res_arr;
	}

	private List<string> ToText <T> (Dictionary<string, T> dict, string prefix) {
		List<string> lines = new List<string>();
		foreach (KeyValuePair<string, T> pair in dict) {
			string _name = pair.Key.Replace(' ', '_');
			lines.Add(string.Format("{0} {1} = {2}", prefix, _name, pair.Value));
		}
		return lines;
	}

	private List<string> ToText <T> (Dictionary<string, T[]> dict, string suffix) {
		List<string> lines = new List<string>();
		foreach (KeyValuePair<string, T[]> pair in dict) {
			string _name = pair.Key.Replace(' ', '_');
			lines.Add(string.Format("{0}*{1}-{2}", suffix, pair.Value.Length, _name));
			foreach (object single_item in pair.Value) {
				lines.Add(single_item.ToString());
			}
		}
		return lines;
	}

	public T Get<T> (string name, T def=default(T), bool test=false) {
		return (T) Get(name, typeof(T), def, test);
	}

	public object Get(string name, Type type, object def=null, bool test=false) {
		try {
			switch (type.Name) {
				case "Int32": return integers [name];
				case "UInt16": return short_integers [name];
				case "Single": return floats32 [name];
				case "Double": return floats64 [name];
				case "Vector3": return vectors [name];
				case "Quaternion": return rotations [name];
				case "String": return strings [name];
				case "Boolean": return booleans [name];
				case "GameObject": return prefabs [name];
				case "DataStructure": return GetChild(name);
				case "Int32[]": return integers_arr [name];
				case "UInt16[]": return short_integers_arr [name];
				case "Single[]": return floats32_arr [name];
				case "Double[]": return floats64_arr [name];
				case "Vector3[]": return vectors_arr [name];
				case "Quaternion[]": return rotations_arr [name];
				case "String[]": return strings_arr [name];
				case "Boolean[]": return booleans_arr [name];
				case "GameObject[]": return prefabs_arr [name];
			}
		} catch (Exception e) {
			if (e is KeyNotFoundException) {
				if (test) throw e;
				else {
					Debug.LogFormat("No {0} named {1} present", type.Name, name);
					Data.current_os.ThrowError(string.Format("No {0} named {1} present", type.Name, name), file: "DataStructure: " + Name);
				}
			} else if (e is NullReferenceException) {
				throw new NullReferenceException(string.Format("{0} is not an instance", name));
			} else throw e;
		}
		Debug.LogWarningFormat("Type {0} is not valid", type.Name);
		return def;
	}

	public void Set<T> (string name, T value) {
		try {
			switch (typeof(T).Name) {
				case "Int32":
					integers [name] = (int) (object) value;
					return;
				case "UInt16":
					short_integers [name] = (ushort) (object) value;
					return;
				case "Single":
					floats32 [name] = (float) (object) value;
					return;
				case "Double":
					floats64 [name] = (double) (object) value;
					return;
				case "Vector3":
					vectors [name] = (Vector3) (object) value;
					return;
				case "Quaternion":
					rotations [name] = (Quaternion) (object) value;
					return;
				case "String":
					strings [name] = (string) (object) value;
					return;
				case "Boolean":
					booleans [name] = (bool) (object) value;
					return;
				case "GameObject":
					prefabs [name] = (GameObject) (object) value;
					return;
				case "DataStructure":
					children [name] = (DataStructure) (object) value;
					return;
				case "Int32[]":
					integers_arr [name] = (int[]) (object) value;
					return;
				case "UInt16[]":
					short_integers_arr [name] = (ushort[]) (object) value;
					return;
				case "Single[]":
					floats32_arr [name] = (float[]) (object) value;
					return;
				case "Double[]":
					floats64_arr [name] = (double[]) (object) value;
					return;
				case "Vector3[]":
					vectors_arr [name] = (Vector3[]) (object) value;
					return;
				case "Quaternion[]":
					rotations_arr [name] = (Quaternion[]) (object) value;
					return;
				case "String[]":
					strings_arr [name] = (string[]) (object) value;
					return;
				case "Boolean[]":
					booleans_arr [name] = (bool[]) (object) value;
					return;
				case "GameObject[]":
					prefabs_arr [name] = (GameObject[]) (object) value;
					return;
			}
		} catch (Exception e) {
			if (e is KeyNotFoundException) {
				Data.current_os.ThrowError(string.Format("No {0} named {1} present", typeof(T).Name, name));
			} else if (e is NullReferenceException) {
				throw new NullReferenceException(string.Format("{0} is not an instance", name));
			} else {
				throw e;
			}
		}
		Debug.LogWarningFormat("Type {0} is not valid", typeof(T).Name);
	}

	/// <summary>
	///		Returns true, if a certain item is in the Datastructure
	/// </summary>
	/// <typeparam name="T"> the Type of the searched item </typeparam>
	/// <param name="name"> The name of the item </param>
	public bool Contains<T>(string name) {
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
	public bool Contains(string name, Type type) {
		object res;
		try {
			res = Get(name, type: type, test:true);
		} catch {
			return false;
		}
		if (res == null) return false;
		return true;
	}

	/// <summary>
	///		trys to find a child with a given name
	/// </summary>
	/// <param name="child_name">name of the child to find</param>
	/// <returns>the child, or null if not present</returns>
	public DataStructure GetChild (string child_name) {
		foreach (DataStructure child in children.Values) {
			if (child.Name == child_name) {
				return child;
			}
		}
		return null;
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

	public void Throw(string message) {
		throw new DSScriptException(message, string.Empty, 0);
	}

	public static Dictionary<Tkey, Tvalue> MergeDict<Tkey, Tvalue>(Dictionary<Tkey, Tvalue>  dict1, Dictionary<Tkey, Tvalue> dict2){
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
	/// nicely formats the content of the datastructure into a string
	/// </summary>
	/// <returns>a nice string</returns>
	public override string ToString () {
		string str = String.Empty;
		string pre_spaces = String.Empty;
		for(uint i=0u; i < RecursionDepth - 1; i++){pre_spaces+="  ";}
		str += string.Format("{1}{0}\n{1}---------------------\n", Name, pre_spaces);
		pre_spaces += "  ";
		foreach (KeyValuePair<string, int> item in integers) str += pre_spaces + item.Key + " = " + item.Value.ToString() + "\n";
		foreach (KeyValuePair<string, int[]> item in integers_arr) str += pre_spaces + item.Key + " = (" + String.Join(", ", Array.ConvertAll(item.Value, i => i.ToString())) + ")\n";
		foreach (KeyValuePair<string, UInt16> item in short_integers) str += pre_spaces + item.Key + " = " + item.Value.ToString() + "\n";
		foreach (KeyValuePair<string, UInt16[]> item in short_integers_arr) str += pre_spaces + item.Key + " = (" + String.Join(", ", Array.ConvertAll(item.Value, i => i.ToString())) + ")\n";
		foreach (KeyValuePair<string, float> item in floats32) str += pre_spaces + item.Key + " = " + item.Value.ToString() + "\n";
		foreach (KeyValuePair<string, float[]> item in floats32_arr) str += pre_spaces + item.Key + " = (" + String.Join(", ", Array.ConvertAll(item.Value, i => i.ToString())) + ")\n";
		foreach (KeyValuePair<string, double> item in floats64) str += pre_spaces + item.Key + " = " + item.Value.ToString() + "\n";
		foreach (KeyValuePair<string, double[]> item in floats64_arr) str += pre_spaces + item.Key + " = (" + String.Join(", ", Array.ConvertAll(item.Value, i => i.ToString())) + ")\n";
		foreach (KeyValuePair<string, Vector3> item in vectors) str += pre_spaces + item.Key + " = " + item.Value.ToString() + "\n";
		foreach (KeyValuePair<string, Vector3 []> item in vectors_arr) str += pre_spaces + item.Key + " = (" + String.Join(", ", Array.ConvertAll(item.Value, i => i.ToString())) + ")\n";
		foreach (KeyValuePair<string, Quaternion> item in rotations) str += pre_spaces + item.Key + " = " + item.Value.eulerAngles.ToString() + "\n";
		foreach (KeyValuePair<string, Quaternion []> item in rotations_arr) str += pre_spaces + item.Key + " = (" + String.Join(", ", Array.ConvertAll(item.Value, i => i.eulerAngles.ToString())) + ")\n";
		foreach (KeyValuePair<string, string> item in strings) str += pre_spaces + item.Key + " = " + item.Value.ToString() + "\n";
		foreach (KeyValuePair<string, string []> item in strings_arr) str += pre_spaces + item.Key + " = (" + String.Join(", ", Array.ConvertAll(item.Value, i => i.ToString())) + ")\n";
		foreach (KeyValuePair<string, bool> item in booleans) str += pre_spaces + item.Key + " = " + item.Value.ToString() + "\n";
		foreach (KeyValuePair<string, bool []> item in booleans_arr) str += pre_spaces + item.Key + " = (" + String.Join(", ", Array.ConvertAll(item.Value, i => i.ToString())) + ")\n";
		foreach (KeyValuePair<string, GameObject> item in prefabs) str += pre_spaces + item.Key + " = " + item.Value.ToString() + "\n";
		foreach (KeyValuePair<string, GameObject []> item in prefabs_arr) str += pre_spaces + item.Key + " = (" + String.Join(", ", Array.ConvertAll(item.Value, i => i.ToString())) + ")\n";
		foreach (KeyValuePair<string, DataStructure> item in children) str += "\n" + item.Value.ToString() + "\n";
		return str;
	}

	public static string Collectable2String<T> (ICollection<T> coll){
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
			floats32 = MergeDict(a.floats32, b.floats32),
			floats64 = MergeDict(a.floats64, b.floats64),
			vectors = MergeDict(a.vectors, b.vectors),
			rotations = MergeDict(a.rotations, b.rotations),
			strings = MergeDict(a.strings, b.strings),
			booleans = MergeDict(a.booleans, b.booleans),
			prefabs = MergeDict(a.prefabs, b.prefabs),
			children = MergeDict(a.children, b.children),

			integers_arr = MergeDict(a.integers_arr, b.integers_arr),
			short_integers_arr = MergeDict(a.short_integers_arr, b.short_integers_arr),
			floats32_arr = MergeDict(a.floats32_arr, b.floats32_arr),
			floats64_arr = MergeDict(a.floats64_arr, b.floats64_arr),
			vectors_arr = MergeDict(a.vectors_arr, b.vectors_arr),
			rotations_arr = MergeDict(a.rotations_arr, b.rotations_arr),
			strings_arr = MergeDict(a.strings_arr, b.strings_arr),
			booleans_arr = MergeDict(a.booleans_arr, b.booleans_arr),
			prefabs_arr = MergeDict(a.prefabs_arr, b.prefabs_arr),
		};
		return res;
	}
}