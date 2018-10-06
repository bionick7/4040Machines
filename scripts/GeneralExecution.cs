using System.Collections.Generic;
using UnityEngine;
using NMS.OS;

/* ==========================================================
 * The general execution of a level, surveilling the
 * objectives and managing everything else, that is not bound
 * to a specific Object
 * ========================================================== */

public class GeneralExecution : MonoBehaviour {

	public bool ignore_player = true;

	private DataStructure eternal_data;
	private ConsoleBehaviour console;
	private OperatingSystem os;
	private ObjectiveTracker tracker;

	public Dictionary<ushort, DataStructure> story;
	public DataStructure [] conversations;
	public short in_level_progress;
	public short in_stage_progress = -1;
	public bool current_done = false;

	private GUIScript uiscript;
	public Loader loader;

	public Dictionary<string, DestroyableTarget> single_targets = new Dictionary<string, DestroyableTarget>();
	public Dictionary<string, Ship[]> squads = new Dictionary<string, Ship[]>();

	private Camera ship_camera;
	private Camera map_camera;

	private bool in_map;
	public bool InMap {
		get { return in_map; }
		set {
			if (value) MapView();
			else NormalView();
			in_map = value;
		}
	}

	private void Awake () {
		ship_camera = GameObject.Find("ShipCamera").GetComponent<Camera>();
		if (ship_camera == null) Debug.LogError("NO ShipCamera");
		map_camera = GameObject.Find("MapCamera").GetComponent<Camera>();

		SceneData.canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
		SceneData.map_canvas = GameObject.Find("MapCanvas").GetComponent<Canvas>();
		SceneData.console = console = SceneData.canvas.GetComponentInChildren<ConsoleBehaviour>();
		SceneData.ship_camera = ship_camera;
		SceneData.map_camera = map_camera;
		SceneData.mapdrawer = GameObject.Find("Placeholder").GetComponent<MapDrawer>();
		InMap = false;

		if (Data.persistend.loader != null) {
			Data.persistend.loader.LoadBattle();
		}
		SelfInit();
		uiscript = GetComponent<GUIScript>();
		SceneData.ui_script = uiscript;

		MapView();
		NextCommand();
	}

	private void SelfInit() {
		SceneData.general = this;
		os = new OperatingSystem(console);
		tracker = new ObjectiveTracker();
	}

	/// <summary> Switches to Map View </summary>
	private void MapView () {
		map_camera.enabled = true;
		map_camera.GetComponent<AudioListener>().enabled = true;

		ship_camera.enabled = false;
		ship_camera.GetComponent<AudioListener>().enabled = false;

		SceneData.canvas.enabled = false;
		SceneData.map_canvas.enabled = true;
	}

	/// <summary> Switches to Ship View </summary>
	private void NormalView () {
		ship_camera.enabled = true;
		ship_camera.GetComponent<AudioListener>().enabled = true;

		map_camera.enabled = false;
		map_camera.GetComponent<AudioListener>().enabled = false;
		
		SceneData.canvas.enabled = true;
		SceneData.map_canvas.enabled = false;
	}

	/// <summary>
	///		Goes to the next command of the in-level events and triggers it
	///		This should be called, if a specific event is accomplished
	/// </summary>
	public void NextCommand () {
		// Todo: Check if stage is a thing
		in_stage_progress = (short) ((in_stage_progress + 1) % story [(ushort) in_level_progress].children.Count);
		FileReader.FileLog(string.Format("STORY: Execute next command: {0} - {1}", in_level_progress, in_stage_progress));
		DataStructure command_data = new List<DataStructure>(story [(ushort) in_level_progress].AllChildren)[in_stage_progress];
		StoryCommand(command_data);
	}

	/// <summary>
	///		Executes an event in the level once
	/// </summary>
	/// <param name="data"> The Datastructure containing the information about the event </param>
	private void StoryCommand (DataStructure data) {
		switch (data.Name) {
		case "get conversation":
			FileReader.FileLog("STORY: Begin Conversation");
			ushort id = data.Get<ushort>("ID");
			os.current_conversation = new Conversation(conversations [id-1], os) { Running = true };
			os.ShowConsole();
			break;
		case "spawn":
			foreach (string name in data.Get<string[]>("names")) {
				loader.Spawn(data.Get<string>("type"), name);
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
				if (!squads.ContainsKey(obj_name)) os.ThrowError("no such sqadron " + obj_name); 
				obj_targets = System.Array.ConvertAll(squads [obj_name], s => s.associated_target);
				break;
			case "target":
				if (!single_targets.ContainsKey(obj_name)) os.ThrowError("no such target " + obj_name);
				Target selected_target = single_targets[obj_name].Target;
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
			in_level_progress = (short) (data.Get<ushort>("stage"));
			in_stage_progress = -1;
			NextCommand();
			return;
		case "finish mission":
			bool won = data.Get<bool>("won");
			Data.persistend.EndBattle(won, data.Get<bool>("progress") && won);
			return;
		}
	}

	private void Update () {
		os.Update();
		tracker.Check();

		if (!uiscript.Paused) {
			foreach (IPhysicsObject obj in SceneData.physics_objects) {
				//Debug.LogFormat(string.Format("{0} got {1} m/s", obj.ToString(), obj.Velocity));
				obj.PhysicsUpdate(Time.deltaTime);
			}
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
			SceneData.general.NextCommand();
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
