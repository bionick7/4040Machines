using System.Collections.Generic;

namespace NMS.OS
{
	public class OperatingSystem
	{
		public Folder MainFolder { get; private set; }
		public ConsoleBehaviour console;
		public Interpreter MainInterpreter { get; private set; }
		public static Queue<Error> errors = new Queue<Error>();
		public uint folder_counter = 0u;

		public Conversation current_conversation = Conversation.Null;

		public OperatingSystem (ConsoleBehaviour cons) {
			console = cons;
			MainInterpreter = new Interpreter();
		}

		public void ShowConsole () {
			SceneData.ui_script.Paused = true;
			SceneData.ui_script.ConsolePos = ConsolePosition.shown;
		}

		public void Update () {
			if (console.HasInput && current_conversation == Conversation.Null) {
				string input = console.ReadLine();

				string prefix = input.Split(' ')[0];
				switch (prefix) {
				case "nms":
					byte [] res;
					string output = "";
					output = MainInterpreter.Process(input.Substring(4), out res);
					console.WriteLine(output);
					break;
				case "exit":
					SceneData.ui_script.ConsolePos = ConsolePosition.hidden;
					break;
				case "cls":
					console.Clear();
					break;
				default:
					console.WriteLine(string.Format("Command {0} not known", prefix));
					break;
				}
			}
			if (errors.Count > 0) {
				console.WriteLine(errors.Dequeue().Display());
			}
			if (current_conversation != Conversation.Null) {
				current_conversation.Update();
			}
		}

		public void ThrowError (string message, uint line = 0u, ErrorType type = ErrorType.default_, string file = "") {
			errors.Enqueue(new Error() { message = message, line = line, file = file, type = type });
		}
	}

	public struct Folder
	{
		public string name;
		public File [] content;
		public Folder [] subfolders;

		public uint FolderID;

		public OperatingSystem parent_system;

		private bool is_root;
		private uint _parent;
		public uint Parent {
			get {
				if (is_root) return 0u;
				return _parent;
			}
			private set {
				_parent = value;
				is_root = value == 0u;
			}
		}

		public Folder (string _name, File [] cont, Folder [] subfolds, OperatingSystem parent) {
			name = _name;
			content = cont;
			subfolders = subfolds;
			parent_system = parent;
			_parent = 0u;
			is_root = true;
			FolderID = parent_system.folder_counter++;
		}

		public Folder (string _name, File [] cont, Folder [] subfolds, Folder parent) {
			name = _name;
			content = cont;
			subfolders = subfolds;
			parent_system = parent.parent_system;
			_parent = parent.FolderID;
			is_root = false;
			FolderID = parent_system.folder_counter++;
		}

		public static bool operator == (Folder left, Folder right) {
			return left.FolderID == right.FolderID;
		}

		public static bool operator != (Folder left, Folder right) {
			return !(left == right);
		}
	}

	public struct File
	{
		public string name;
		public byte [] content;

		public const string seperator = "_";

		/// <summary>
		///		The type of file, determined by the prefix
		/// </summary>
		public FileExtension FileType {
			get {
				if (!name.Contains(seperator)) return FileExtension.none;
				switch (name.Substring(0, name.IndexOf(seperator))) {
				case "txt":	return FileExtension.text;
				case "bin": return FileExtension.binary;
				default: return FileExtension.none;
				}
			}
			set {
				string raw_name;
				if (!name.Contains(seperator)) raw_name = name;
				else raw_name = name.Substring(name.IndexOf(seperator));
				switch (value) {
				case FileExtension.text:
					name = "txt" + raw_name;
					break;
				case FileExtension.binary:
					name = "bin" + raw_name;
					break;
				case FileExtension.none:
					name = raw_name;
					break;
				default:
					goto case FileExtension.none;
				}
			}
		}

		/// <param name="_name"> The name of the file </param>
		/// <param name="cont"> The content of the file in binary </param>
		public File (string _name, byte[] cont) {
			name = _name;
			content = cont;
		}

		/// <param name="_name"> The name of the file </param>
		/// <param name="cont"> The content of the file as text </param>
		public File (string _name, string[] lines) {
			name = _name;
			content = System.Array.ConvertAll(string.Join("\n", lines).ToCharArray(), x => (byte) x);
			FileType = FileExtension.text;
		}
	}

	public enum FileExtension
	{
		text,
		binary,
		none
	}
}
