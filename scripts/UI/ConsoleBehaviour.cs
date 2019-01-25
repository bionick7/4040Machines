using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConsoleBehaviour : MonoBehaviour {

	private InputField input;
	private Text text;
	private AudioManager audio_;

	private Stack<string> InputLines = new Stack<string>();
	private Stack<string> DirectCommands = new Stack<string>();
	private List<string> PreviousCommands = new List<string>();
	private int commands_indexer = 1;

	private Queue<char> character_queue = new Queue<char>();

	private bool initialized = false;

	private System.Exception not_init = new System.Exception("Console not initialized");

	public bool HasInput {
		get { return InputLines.Count > 0; }
	}

	private bool HasCommandInput {
		get { return DirectCommands.Count > 0; }
	}

	public bool typing = false;

	public const int full_lines = 22;
	public const int max_witdth = 47;
	public int lines_shown = full_lines;
	public int lines_written = 0;

	public Conversation current_conversation = Conversation.Null;

	private ConsolePosition _consolepos;
	public ConsolePosition ConsolePos {
		get { return _consolepos; }
		set {
			switch (value) {
			case ConsolePosition.hidden:
				transform.position = new Vector3(600, -1000);
				break;
			case ConsolePosition.shown:
				transform.position = new Vector3(Screen.width - 280, Screen.height / 2);
				lines_shown = full_lines;
				break;
			case ConsolePosition.lower:
				transform.position = new Vector3(Screen.width / 2, -200);
				lines_shown = 2;
				break;
			}
			_consolepos = value;
		}
	}

	private void Start () {
		if (!initialized) Start_();
	}

	public void Start_ () {
		audio_ = Globals.audio;
		input = GetComponentInChildren<InputField>();
		text = transform.GetChild(0).GetComponent<Text>();

		initialized = true;
		Clear();
	}

	public void WriteLine (string text) {
		string text2 = string.Empty;
		char [] text_arr = text.ToCharArray();
		for (int i = 0, count = 0; i < text.Length; i++, count++) {
			text2 += text_arr [i];
			if (text_arr [i] == '\n') {
				count = 0;
			}
			if (count % max_witdth == max_witdth - 1) {
				text2 += '\n';
			}
		}
		WriteSlowly(text2 + "\n");
	}

	public string ReadLine () {
		if (!initialized) throw not_init;
		if (InputLines.Count == 0) { return string.Empty; }
		string answer = InputLines.Pop();
		return answer;
	}

	public void Clear () {
		text.text = string.Empty;
	}

	public void ClearTopRow () {
		text.text = text.text.Substring(text.text.IndexOf('\n') + 1);
	}

	public void WriteFast (string ptext) {
		text.text += ptext;
	}

	public void WriteSlowly (string ptext) {
		foreach (char c in ptext.ToCharArray()) {
			character_queue.Enqueue(c);
		}
	}
	
	public void Enter () {
		if (!initialized) throw not_init;
		if (input.text.StartsWith("#"))
			DirectCommands.Push(input.text.Substring(1));
		else
			InputLines.Push(input.text);
		PreviousCommands.Add(input.text);
		commands_indexer = PreviousCommands.Count - 1;
		input.text = string.Empty;
	}

	public void Terminate () {
		audio_.computer_audio_source.Stop();
	}

	private void Update () {
		SceneGlobals.in_console = input.isFocused;
		if (SceneGlobals.in_console && Input.GetKeyDown(KeyCode.UpArrow)) {
			if (commands_indexer > 0) {
				input.text = PreviousCommands [--commands_indexer];
			}
		}
		if (SceneGlobals.in_console && Input.GetKeyDown(KeyCode.DownArrow)) {
			if (commands_indexer < PreviousCommands.Count - 1)
				input.text = PreviousCommands [++commands_indexer];
		}

		if (HasCommandInput) {
			string command = DirectCommands.Pop();
			switch (command.ToLower()) {
			case "cl_top":
				ClearTopRow();
				break;
			case "cls":
				Clear();
				break;
			case "godmode":
				break;
			default:
				if (command.ToLower().StartsWith("cpu")) {
					ulong number = 0;
					if (command.Length <= 5) break;
					if (System.UInt64.TryParse(command.Substring(4), out number)) {
						Globals.current_os.cpu.Execute(new ulong [] { number });
					}
					break;
				}
				if (command.ToLower().StartsWith("print")) {
					if (command.Length <= 7) break;
					WriteLine(command.Substring(6));
					break;
				}
				break;
			}
		}

		typing = character_queue.Count > 0;
		if (typing) {
			if (ConsolePos == ConsolePosition.shown && !audio_.computer_audio_source.isPlaying) {
				audio_.ComputerPlay("writing_ticks", ploop: true);
			} else if (ConsolePos != ConsolePosition.shown && audio_.computer_audio_source.isPlaying){
				audio_.StopComputerSoundLoop();
			}
			audio_.computer_audio_source.volume = Random.Range(.8f, 1);
			int lines = text.text.Split('\n').Length + 1;
			if (lines >= full_lines - 1) {
				ClearTopRow();
			}
			char character = character_queue.Dequeue();
			if (character != 0) {
				text.text += character;
			}
		} else {
			if (audio_.computer_audio_source.isPlaying) {
				audio_.StopComputerSoundLoop();
			}
		}

		if (current_conversation != Conversation.Null) {
			current_conversation.Update();
		}
	}
}
