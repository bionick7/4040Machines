using UnityEngine;
using System.Collections.Generic;

public class Conversation
{
	public ushort CurrentState { get; set; }
	public ushort nextstate;
	public byte messgecycle = 0;
	public bool Running { get; set; }

	public ConsoleBehaviour console;
	public NMS.OS.OperatingSystem operating_system;

	private Dictionary<ushort, Message[]> messages = new Dictionary<ushort, Message[]>();

	/// <param name="source"> 
	///		The dialog datastructure which should
	///		be the source of the actual dialog
	///	</param>
	///	<param name="os">
	///		The operating system on which this should be running.
	///		(Just used to get the console)
	///	</param>
	public Conversation (DataStructure source, NMS.OS.OperatingSystem os) {
		console = os == null ? null : os.console;
		operating_system = os;
		messages = InterpretDS(source);
		CurrentState = source.Contains<ushort>("begin") ? source.Get<ushort>("begin") : (ushort) 1;
	}

	public void DisplayText (string text) {
		string act_text = string.Format("|| {0}", text);
		console.WriteLine(act_text);
	}

	public void LogError (string message) {
		string act_text = string.Format("-- Error {0} --", message);
		console.WriteLine(act_text);
	}

	private Dictionary<ushort, Message[]> InterpretDS (DataStructure structure) {
		Dictionary<ushort, Message[]> res = new Dictionary<ushort, Message[]>();
		foreach (DataStructure child in structure.AllChildren) {
			Message[] msgs = new Message[child.children.Count];
			int i=0;
			foreach (DataStructure childchild in child.AllChildren) {
				msgs [i++] = new Message(childchild, Message.string2message_types [childchild.Name]) {
					parent_conv = this,
				};
			}
			res.Add(child.Get<ushort>("num"), msgs);
		}
		return res;
	}

	public void Update () {
		if (!Running) return;
		if (!messages.ContainsKey(CurrentState)) {
			operating_system.ThrowError(string.Format("{0} is not a state", CurrentState));
		}
		Message curr_message = messages[CurrentState][messgecycle];
		Message.Result res = curr_message.Execute(console.HasInput ? console.ReadLine() : null);
		switch (res) {
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
		case Message.Result.notimplemented:
			LogError("Function not implemented yet");
			goto case Message.Result.error;
		case Message.Result.break_:
			CurrentState = nextstate;
			break;
		case Message.Result.error:
			goto case Message.Result.exit;
		case Message.Result.exit:
			Exit();
			break;
		default:
			goto case Message.Result.finished;
		}
		return;
	}

	private void Exit () {
		Debug.LogWarning("Exit coversation");
		if (operating_system != null) operating_system.current_conversation = Null;
		SceneData.general.NextCommand();
	}

	public static readonly Conversation Null = new Conversation(DataStructure.Empty, null){ Running = false };

	public override string ToString () {
		if (operating_system == null) return "<Null Conversation>";
		return Running ? "Running Conversation" : "Inactive Conversation";
	}
}

public class Message
{
	private DataStructure data;
	private Type type;

	public Conversation parent_conv;
	private delegate Result Func ();

	private Func actual_func;

	private string input;
	private bool IsInput {
		get {
			return input != null;
		}
		set {
			if (!value) input = null;
		}
	}

	// For misc needs
	private double d01 = 0d;
	private double d02 = 0d;
	private string answer_string;

	public Message (DataStructure message_data, Type messagetype) {
		data = message_data;
		type = messagetype;
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
		}
	}

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
	}

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

	#region executable methods
	private Result Text () {
		if (!data.Contains<string>("text")) {
			parent_conv.LogError("Needs component \"text\"(chr)");
			return Result.error;
		}
		string text = data.Get<string>("text");
		parent_conv.DisplayText(text);
		return Result.finished;
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
		string missing = string.Empty;
		if (!Check4Items(new Dictionary<System.Type, string>() {
			{ typeof(string), "text" },
			{ typeof(ushort[]), "routes" },
			{ typeof(string[]), "answers" }
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
			if (IsInput) {
				answer_string = input;
				d01 = 2d;
			}
			return Result.running;
		case 2:
			// Responding
			if (!(answer_string.Length == 1 && answer_string[0] > 0x30 && answer_string[0] <= 0x30 + data.Get<string[]>("answers").Length)) {
				parent_conv.LogError(string.Format("answer must be between '1' and '{0}' included", data.Get<string[]>("answers").Length));
				d01 = 0d;
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
		Character curr_char = Data.persistend.current_character;

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
		return Result.exit;
	}
	#endregion

	/// <summary> Checks if certain items of certain types are in the data </summary>
	/// <param name="item_array"></param>
	/// <returns> True if all items are contained </returns>
	public bool Check4Items(Dictionary<System.Type, string> item_array) {
		bool present = true;
		foreach (KeyValuePair<System.Type, string> pair in item_array) {
			if (!data.Contains(pair.Value, pair.Key)) {
				parent_conv.operating_system.ThrowError(string.Format("{0} not found", pair.Value), 0, NMS.ErrorType.default_);
				present = false;
			}
		}
		return present;
	}

	public override string ToString () {
		return string.Format("<Message: {0}>", type);
	}

	public static readonly Dictionary<string, Type> string2message_types = new Dictionary<string, Type>{
		{ "text", Type.text },
		{ "inputwait", Type.inputwait },
		{ "wait", Type.wait },
		{ "goto", Type.route },
		{ "dialog", Type.dialog },
		{ "textinput", Type.textinput },
		{ "polit_change", Type.polit_change },
		{ "leave", Type.leave }
	};
}