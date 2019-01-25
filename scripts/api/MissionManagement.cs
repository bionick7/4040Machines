using System.Collections.Generic;
using FileManagement;

/// <summary> The core, who manages the mission </summary>
public class MissionCore
{
	private ConsoleBehaviour console;
	private ObjectiveTracker tracker;

	public Dictionary<ushort, DataStructure> story;
	public DataStructure [] conversations;
	public short in_level_progress;
	public short in_stage_progress = -1;
	public bool current_done = false;

	public Loader loader;

	/// <summary> Single targets already in the scene </summary>
	public Dictionary<string, DestroyableTarget> single_targets = new Dictionary<string, DestroyableTarget>();
	/// <summary> Squads already in the scene </summary>
	public Dictionary<string, Ship[]> squads = new Dictionary<string, Ship[]>();

	public MissionCore (ConsoleBehaviour p_console, Loader p_loader) {
		console = p_console;
		loader = p_loader;

		tracker = new ObjectiveTracker();
	}

	/// <summary>
	///		Goes to the next command of the in-level events and triggers it
	///		This should be called, if a specific event is accomplished
	/// </summary>
	public void NextCommand () {
		// Todo: Check if stage is a thing
		if (!story.ContainsKey((ushort) in_level_progress)) {
			FileReader.FileLog(in_level_progress.ToString() + " not in story", FileLogType.error);
			return;
		}
		in_stage_progress = (short) ((in_stage_progress + 1) % story [(ushort) in_level_progress].children.Count);
		FileReader.FileLog(string.Format("Execute next command: {0} - {1}", in_level_progress, in_stage_progress), FileLogType.story);
		DataStructure command_data = new List<DataStructure>(story [(ushort) in_level_progress].AllChildren)[in_stage_progress];
		StoryCommand(command_data);
	}

	public void Update () {
		tracker.Check();
	}

	/// <summary>
	///		Executes an event in the level once
	/// </summary>
	/// <param name="data"> The Datastructure containing the information about the event </param>
	private void StoryCommand (DataStructure data) {
		switch (data.Name) {
		case "get conversation":
			FileReader.FileLog("Begin Conversation", FileLogType.story);
			ushort id = data.Get<ushort>("ID");
			new Conversation(conversations [id-1], console) { Running = true };
			console.ConsolePos = ConsolePosition.shown;
			break;
		case "spawn":
			string[] types = data.Get<string[]>("types");
			string[] names = data.Get<string[]>("names");
			if (types.Length != names.Length) {
				DeveloppmentTools.Log("length of \"types\" array must be the same as length of \"names\" array");
			}
			for (int i=0; i < types.Length; i++) {
				loader.Spawn(types[i], names[i]);
			}
			NextCommand();
			break;
		case "objective":
			// Todo: Check compleateness
			Objectives obj_type; Target [] obj_targets;
			string [] objective_specs = data.Get<string>("objective type").Split(' ');
			// What to do with the target?
			switch (objective_specs [0]) {
			case "kill":
				obj_type = Objectives.destroy;
				break;
			case "escort":
				obj_type = Objectives.escort;
				break;
			case "hack":
				obj_type = Objectives.hack;
				break;
			default:
				obj_type = Objectives.none;
				break;
			}
			string obj_name = data.Get<string>("target name");
			// Which target is it?
			switch (objective_specs [1]) {
			case "squadron":
				if (!squads.ContainsKey(obj_name)) DeveloppmentTools.Log("LOADER - no such sqadron in the scene" + obj_name); 
				obj_targets = System.Array.ConvertAll(squads [obj_name], s => s.Associated);
				break;
			case "target":
				if (!single_targets.ContainsKey(obj_name)) DeveloppmentTools.Log("LOADER - no such target in the scene: " + obj_name);
				Target selected_target = single_targets[obj_name].Associated;
				obj_targets = new Target [1] { selected_target };
				break;
			default:
				obj_targets = new Target[0];
				break;
			}
			Objective objective = new Objective() { objective_type = obj_type, targets = obj_targets };
			tracker.NewObjective(objective);
			return;
		case "goto":
			FileReader.FileLog("Go to story num " + data.Get<ushort>("stage").ToString(), FileLogType.story);
			in_level_progress = (short) (data.Get<ushort>("stage"));
			in_stage_progress = -1;
			NextCommand();
			return;
		case "finish mission":
			bool won = data.Get<bool>("won");
			SceneGlobals.general.EndBattle(won);
			return;
		}
	}
}

public class ObjectiveTracker
{
	public Objective current_objective = Objective.none;
	public bool [] done;

	public void NewObjective (Objective obj) {
		current_objective = obj;
		done = new bool[obj.targets.Length];
		for (int i = 0; i < done.Length; i++) done [i] = false;
	}

	public void Check () {
		if (current_objective == Objective.none) return;
		for (int i=0; i < done.Length; i++) {
			if (!done [i]) {
				Target t = current_objective.targets[i];
				switch (current_objective.objective_type) {
				case Objectives.destroy:
					if (!t.Exists) done [i] = true;
					break;
				case Objectives.escort:
					break;
				case Objectives.hack:
					break;
				}
			}
		}
		if (System.Array.TrueForAll(done, x => x)) {
			current_objective = Objective.none;
			SceneGlobals.general.NextCommand();
		}
	}
}

public struct Objective
{
	public Objectives objective_type;
	public Target [] targets;

	public static readonly Objective none = new Objective () { objective_type = Objectives.none, targets = new Target [0] };

	public static bool operator == (Objective left, Objective right) {
		return left.objective_type == right.objective_type && left.targets == right.targets;
	}

	public static bool operator != (Objective left, Objective right) {
		return !(left == right);
	}

	public override string ToString () {
		return string.Format("<Objective: {0}>", objective_type);
	}
}