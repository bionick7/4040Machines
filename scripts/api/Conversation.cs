using UnityEngine;
using System.Collections.Generic;
using FileManagement;

/* ================================================================
 * The conversations, that are shown during battles and in the menu
 * ================================================================ */

/// <summary> The conversation itself </summary>
public class Conversation
{
	public ushort CurrentState { get; set; }
	public ushort nextstate;
	public byte messgecycle = 0;
	public bool Running { get; set; }

	public ConsoleBehaviour console;
	//public NMS.OS.OperatingSystem operating_system;

	private Dictionary<ushort, Message[]> messages = new Dictionary<ushort, Message[]>();

	/// <param name="source"> 
	///		The dialog datastructure which should
	///		be the source of the actual dialog
	///	</param>
	///	<param name="os">
	///		The operating system on which this should be running.
	///		(Just used to get the console)
	///	</param>
	public Conversation (DataStructure source, ConsoleBehaviour p_console) {
		console = p_console;
		if (console != null)
			console.current_conversation = this;
		messages = InterpretDS(source);
		CurrentState = source.Get<ushort>("begin", 1, quiet: true);
	}

	/// <summary> Displays text </summary>
	public void DisplayText (string text) {
		string act_text = string.Format("|| {0}", text);
		console.WriteLine(act_text);
	}

	/// <summary> Displays text as an error </summary>
	public void LogError (string message) {
		string act_text = string.Format("**ERROR**: {0} ", message);
		DeveloppmentTools.Log(act_text);
		console.WriteLine(act_text);
	}

	/// <summary> Interprets the datastructure of the conversation </summary>
	/// <param name="structure"></param>
	/// <returns>
	///		A dictionnary unsigned short -> message array. 
	///		Every key-valuepair in the dictionnary represents one
	///		stage of the conversation.
	///	</returns>
	private Dictionary<ushort, Message[]> InterpretDS (DataStructure structure) {
		Dictionary<ushort, Message[]> res = new Dictionary<ushort, Message[]>();
		foreach (DataStructure child in structure.AllChildren) {
			Message[] msgs = new Message[child.children.Count];
			int i=0;
			foreach (DataStructure childchild in child.AllChildren) {
				if (Message.string2message_types.ContainsKey(childchild.Name)) {
					msgs [i++] = new Message(childchild, Message.string2message_types [childchild.Name]) {
						parent_conv = this,
					};
				} else {
					DeveloppmentTools.Log(childchild.Name + "Not a command");
				}
			}
			res.Add(child.Get<ushort>("num"), msgs);
		}
		return res;
	}

	/// <summary> 
	///		Should be called every frame.
	///		Continiues execution, depending on what the previous message returned
	///	</summary>
	public void Update () {
		if (!Running) return;
		if (!messages.ContainsKey(CurrentState)) {
			DeveloppmentTools.Log(string.Format("State {0} not an option", CurrentState));
			return;
		}
		if (messages[CurrentState].Length <= messgecycle) {
			DeveloppmentTools.Log(string.Format("MessageID {0} not an option", messgecycle));
			return;
		}
		Message curr_message = messages[CurrentState][messgecycle];
		Message.Result res = curr_message.Execute(console.HasInput ? console.ReadLine() : null);
		switch (res) {
		default:
		case Message.Result.error:
		case Message.Result.finished:
			if (++messgecycle >= messages [CurrentState].Length) {
				if (nextstate == ushort.MaxValue) LogError("State not set");
				CurrentState = nextstate;
				messgecycle = 0;
				// To be recognized as the error-source
				nextstate = ushort.MaxValue;
			}
			break;
		case Message.Result.running:
			break;
		case Message.Result.break_:
			CurrentState = nextstate;
			break;
		case Message.Result.notimplemented:
			LogError("Function not implemented yet");
			goto case Message.Result.error;
		case Message.Result.exit:
			Exit();
			break;
		}
		return;
	}

	/// <summary> Finishes a conversation </summary>
	private void Exit () {
		DeveloppmentTools.Log("Exit coversation");
		if (console != null) console.current_conversation = Null;
		if (SceneGlobals.general != null && SceneGlobals.general.gameObject)
			SceneGlobals.general.NextCommand();
		else if (CampagneManager.active != null && CampagneManager.active.gameObject)
			CampagneManager.active.ExitConversation();
	}

	/// <summary> Default conversation </summary>
	public static readonly Conversation Null = new Conversation(DataStructure.Empty, null){ Running = false };

	public override string ToString () {
		if (console == null) return "<Null Conversation>";
		return Running ? "Running Conversation" : "Inactive Conversation";
	}
}

/// <summary>
///		A message is one act of a conversation
/// </summary>
public class Message
{
	private DataStructure data;
	private Type type;

	public Conversation parent_conv;

	/// <summary> The delegate of the function </summary>
	private delegate Result Func ();

	/// <summary> The actual function executed by the message </summary>
	private Func actual_func;

	private string input;
	/// <summary> True, if there is input </summary>
	private bool IsInput {
		get { return input != null; }
		set { if (!value) input = null; }
	}

	// For misc needs
	private double d01 = 0d;
	private double d02 = 0d;
	private string answer_string;

	/// <param name="message_data"> All the data, that is precised in the message, as Datastructure </param>
	/// <param name="messagetype"> The type of the message </param>
	public Message (DataStructure message_data, Type messagetype) {
		data = message_data;
		type = messagetype;
		// Generates function depending on the type
		switch (messagetype) {
		case Type.text:
			actual_func = new Func(Text);
			break;
		case Type.inputwait:
			actual_func = new Func(Inputwait);
			break;
		case Type.wait:
			actual_func = new Func(Wait);
			break;
		case Type.route:
			actual_func = new Func(Route);
			break;
		case Type.dialog:
			actual_func = new Func(Dialog);
			break;
		case Type.textinput:
			actual_func = new Func(Textinput);
			break;
		case Type.polit_change:
			actual_func = new Func(PolitChange);
			break;
		case Type.leave:
			actual_func = new Func(Leave);
			break;
		case Type.key_up:
			actual_func = new Func(KeyUp);
			break;
		case Type.key_down:
			actual_func = new Func(KeyDown);
			break;
		case Type.mark:
			actual_func = new Func(Mark);
			break;
		case Type.clear:
			actual_func = new Func(Clear);
			break;
		}
	}

	/// <summary> The type of the message </summary>
	public enum Type
	{
		text,
		inputwait,
		wait,
		route,
		dialog,
		textinput,
		polit_change,
		leave,
		key_down,
		key_up,
		mark,
		clear,
	}

	/// <summary> What the message returns </summary>
	public enum Result
	{
		finished,
		running,
		notimplemented,
		break_,
		error,
		exit,
		none,
	}

	/// <summary> Executes itself </summary>
	/// <param name="inp"> The input, that was typed in; "null", if none </param>
	public Result Execute (string inp) {
		if (inp != null) input = inp;
		Result res = Result.none;
		ushort saftycounter = 0;
		while (res == Result.none) {
			res = actual_func();
			if (++saftycounter > 1000) {
				parent_conv.LogError("function does not return anything");
			}
		}
		IsInput = false;
		return res;
	}

	// Here are all the Functions
	#region executable methods
	private Result Text () {
		if (d01 == 0) {
			if (!data.Contains<string>("text")) {
				parent_conv.LogError("Needs component \"text\"(chr)");
				return Result.error;
			}
			string text = data.Get<string>("text");
			parent_conv.DisplayText(text);
			d01 = 1;
		}
		return parent_conv.console.typing ? Result.running : Result.finished;
	}

	private Result Inputwait () {
		return IsInput ? Result.finished : Result.running;
	}

	private Result Wait () {
		if (!data.Contains<double>("time")) {
			parent_conv.LogError("Needs Component \"time\"(f64)");
			return Result.error;
		}
		d01 += Time.deltaTime;
		if (d01 >= data.Get<double>("time")) {
			d01 = 0d;
			return Result.finished;
		}
		return Result.running;
	}

	private Result Route () {
		if (!data.Contains<ushort>("num")) {
			parent_conv.LogError("Needs Component \"num\"(snt)");
			return Result.error;
		}
		parent_conv.nextstate = data.Get<ushort>("num");
		return Result.finished;
	}

	private Result Dialog () {
		//Checkin'
		if (!Check4Items(
			new System.Type [] {
				typeof(string),
				typeof(ushort[]),
				typeof(string[])
			},
			new string [] {
				"text",
				"routes",
				"answers"
			})) return Result.error;

		switch ((int) d01) {
		case 0:
			// Display text
			parent_conv.DisplayText(data.Get<string>("text"));
			string [] answers = data.Get<string []>("answers");
			for (int i=0; i < answers.Length; i++) {
				parent_conv.DisplayText(string.Format("{0} - {1}", i + 1, answers[i]));
			}
			d01 = 1d;
			return Result.running;
		case 1:
			// Waiting for answer
			if (IsInput && !parent_conv.console.typing) {
				answer_string = input;
				d01 = 2d;
			}
			return Result.running;
		case 2:
			// Responding
			if (!(answer_string.Length == 1 && answer_string[0] > 0x30 && answer_string[0] <= 0x30 + data.Get<string[]>("answers").Length)) {
				parent_conv.LogError(string.Format("answer must be between '1' and '{0}' included", data.Get<string[]>("answers").Length));
				d01 = 1d;
				return Result.running;
			}
			int ans = answer_string[0] - 0x31;
			parent_conv.DisplayText(data.Get<string[]>("answers")[ans]);
			parent_conv.nextstate = data.Get<ushort[]>("routes")[ans];
			d01 = 0d;
			parent_conv.DisplayText("-------------------------------");
			return Result.finished;
		default:
			return Result.none;
		}
	}

	private Result Textinput () {
		return Result.notimplemented;
	}

	private Result PolitChange () {
		Character curr_char = Globals.current_character;

		if (data.Contains<double>("cap")) {
			curr_char.politics [0] += data.Get<double>("cap");
		}
		if (data.Contains<double>("auth")) {
			curr_char.politics [1] += data.Get<double>("auth");
		}
		if (data.Contains<double>("nat")) {
			curr_char.politics [0] += data.Get<double>("nat");
		}
		if (data.Contains<double>("trad")) {
			curr_char.politics [1] += data.Get<double>("trad");
		}

		curr_char.Save();

		return Result.finished;
	}

	private Result Leave () {
		parent_conv.DisplayText("**TRANSMISSION ENDS**");
		return Result.exit;
	}

	private Result KeyUp () {
		if (data.Contains<string []>("keys")) {
			var bindings = System.Array.ConvertAll(data.Get<string[]>("keys"), x => Globals.bindings.GetByFunction(x));
			if (System.Array.Exists(bindings, x => x.ISUnPressed())) {
				FileReader.FileLog("Recieved keypress: " + string.Join(" : ", data.Get<string[]>("keys")), FileLogType.story);
				return Result.finished;
			}
			return Result.running;
		}
		if (data.Contains<ushort []>("keynums")) {
			if (System.Array.Exists(data.Get<ushort []>("keynum"), x => Input.GetKeyUp((KeyCode) x))) {
				FileReader.FileLog("Recieved keypress: " + string.Join(" : ", System.Array.ConvertAll(data.Get<ushort[]>("keynums"), x => ((KeyCode) x).ToString())), FileLogType.story);
				return Result.finished;
			}
			return Result.running;
		}
		return Result.error;
	}

	private Result KeyDown () {
		if (data.Contains<string []>("keys")) {
			var bindings = System.Array.ConvertAll(data.Get<string[]>("keys"), x => Globals.bindings.GetByFunction(x));
			if (System.Array.Exists(bindings, x => x.ISPressedDown())) {
				FileReader.FileLog("Recieved keypress: " + string.Join(" : ", data.Get<string[]>("keys")), FileLogType.story);
				return Result.finished;
			}
			return Result.running;
		}
		if (data.Contains<ushort []>("keynums")) {
			if (System.Array.Exists(data.Get<ushort []>("keynum"), x => Input.GetKeyDown((KeyCode) x))) {
				FileReader.FileLog("Recieved keypress: " + string.Join(" : ", System.Array.ConvertAll(data.Get<ushort[]>("keynums"), x => ((KeyCode) x).ToString())), FileLogType.story);
				return Result.finished;
			}
			return Result.running;
		}
		return Result.error;
	}

	private Result Mark () {
		//Checkin'
		if (!Check4Items(
			new System.Type [] {
				typeof(int),
				typeof(int),
				typeof(int),
				typeof(int),
				typeof(int)
			},
			new string [] {
				"pos x",
				"pos y",
				"size x",
				"size y",
				"attachment"
			})) return Result.error;
		
		DialogIndicator.Active.Set(
			data.Get<int>("pos x"),
			data.Get<int>("pos y"),
			data.Get<int>("size x"),
			data.Get<int>("size y"),
			(DialogIndicator.Anchor) data.Get<int>("attachment")
		);
		return Result.finished;
	}

	private Result Clear () {
		parent_conv.console.Clear();
		return Result.finished;
	}
	#endregion

	/// <summary> Checks if certain items of certain types are in the data </summary>
	/// <param name="item_array"></param>
	/// <returns> True if all items are contained </returns>
	public bool Check4Items(System.Type[] types, string[] names) {
		bool present = true;
		for (int i=0; i < Mathf.Min(types.Length, names.Length); i++) {
			if (!data.Contains(names[i], types[i])) {
				DeveloppmentTools.LogFormat("\"{0}\" not found", names [i]);
				present = false;
			}
		}
		return present;
	}

	public override string ToString () {
		return string.Format("<Message: {0}>", type);
	}

	/// <summary> Dictionnary, that links the messagetypes to strings </summary>
	public static readonly Dictionary<string, Type> string2message_types = new Dictionary<string, Type>{
		{ "text", Type.text },
		{ "inputwait", Type.inputwait },
		{ "wait", Type.wait },
		{ "goto", Type.route },
		{ "dialog", Type.dialog },
		{ "textinput", Type.textinput },
		{ "polit change", Type.polit_change },
		{ "leave", Type.leave },
		{ "await keydown", Type.key_down },
		{ "await keyup", Type.key_up },
		{ "mark", Type.mark },
		{ "cls", Type.clear },
	};
}