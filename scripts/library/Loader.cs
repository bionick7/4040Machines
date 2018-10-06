using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/* ===================================================================
 * Contains the Loader object to load a scene and spawn stuff from the 
 * Battle's datastructure
 * =================================================================== */

public class Loader
{
	private DataStructure BattleData { get; set; }

	public Loader (DataStructure data) {
		BattleData = data;
	}

	private GameObject placeholder;
	private GeneralExecution general;

	// There are more efficient data storing methods than DataStructures!
	private Dictionary<string, DataStructure> single_targets_dict = new Dictionary<string, DataStructure>();
	private Dictionary<string, DataStructure> squadrons_dict = new Dictionary<string, DataStructure>();

	public static readonly Dictionary<string, Sceneries> scenerie_dict = new Dictionary<string, Sceneries>() {
		{ "mars", Sceneries.mars },
		{ "earth", Sceneries.earth }
	};

	/// <summary> Loads a battle </summary>
	public void LoadBattle () {
		FileReader.FileLog("LOADER: GO");
		placeholder = GameObject.Find("Placeholder");
		general = placeholder.GetComponent<GeneralExecution>();
		general.loader = this;

		List<DataStructure> conversations = new List<DataStructure>();
		foreach (DataStructure child in BattleData.AllChildren) {
			string name = child.Name;
			switch (name) {
			case "squadron":
				squadrons_dict.Add(child.Get<string>("name"), child);
				break;

			case "player ship":
				SpawnPlayer(child);
				break;
				
			case "target":
				single_targets_dict.Add(child.Get<string>("name"), child);
				break;

			case "general":
				ChangeScenerie(scenerie_dict [child.Get<string>("planet")]);
				break;

			case "conversation":
				conversations.Add(child);
				break;

			case "story":
				FileReader.FileLog("LOADER: Load Story");
				Dictionary<ushort, DataStructure> story = new Dictionary<ushort, DataStructure>();
				foreach(DataStructure stage in child.AllChildren) {
					// Checks if string matches "StageX", where X is a number
					if (Regex.IsMatch(stage.Name, @"^stage\d{1,3}$")) {
						ushort number = System.UInt16.Parse(stage.Name.Substring(5));
						story [number] = stage;
					}
				}
				general.story = story;
				general.in_level_progress = (short) (child.Contains<ushort>("startstage") ? child.Get<ushort>("startstage") : 0);
				break;
			}
		}

		general.conversations = conversations.ToArray();

		foreach (Ship obj in SceneData.ship_list) {
			foreach (BulletCollisionDetection part in obj.Object.GetComponentsInChildren<BulletCollisionDetection>()) {
				part.Initialize();
			}
		}
	}

	public void Spawn(string type, string name) {
		switch (type) {
		case "squadron":
			if (!squadrons_dict.ContainsKey(name)) {
				Data.current_os.ThrowError("no such squadron: " + name);
				return;
			}
			SpawnSquadron(squadrons_dict [name]);
			break;
		case "single target":
			if (!single_targets_dict.ContainsKey(name)) {
				Data.current_os.ThrowError("no such single target: " + name);
				return;
			}
			SpawnSingleTarget(single_targets_dict [name]);
			break;
		}
	}

	# region Spawners
	private void SpawnSquadron(DataStructure data) {
		Debug.Log("LOADER: Load squadron");
		// Create the host
		List<Ship> sqad_list = new List<Ship>();
		NetworkHost host;
		if (!data.Contains<string>("leader") || data.Get<string>("leader") == "NULL") {
			host = placeholder.AddComponent<NetworkHost>();
		} else {
			GameObject leader_obj = SpawnShip(data.Get<string>("leader"),
											  data.Get<bool>("side"),
											  data.Get<Vector3>("leader_pos"),
											  data.Get<Quaternion>("leader_rot"));
			Ship ship = leader_obj.GetComponent<ShipControl>().myship;
			sqad_list.Add(ship);
			host = EnsureComponent<NetworkHost>(leader_obj);
			if (data.Contains<Vector3>("leader velocity")) {
				ship.Velocity = data.Get<Vector3>("leader velocity");
			}
			if (data.Contains<Vector3>("leader angular velocity")) {
				ship.AngularVelocity = data.Get<Vector3>("angular veloity");
			}
		}
		host.Net = new Network((uint) data.Get<Vector3 []>("positions").Length, data.Get<bool>("side"));

		// Create the fleet
		string ship_name = data.Get<string>("ships");
		bool side = data.Get<bool>("side");
		Quaternion rotation = data.Get<Quaternion>("orientation");
		Vector3 velocity = data.Get("velocity", Vector3.zero);
		Vector3 angular_velocity = data.Get("angular velocity", Vector3.zero);
		foreach(Vector3 pos in data.Get<Vector3 []>("positions")) {
			GameObject ship_obj = SpawnShip(ship_name, side, pos, rotation);
			FighterAI ai = EnsureComponent<FighterAI>(ship_obj);
			ai.Net = host.Net;
			Ship ship = ship_obj.GetComponent<ShipControl>().myship;
			sqad_list.Add(ship);

			ship.Velocity = velocity;
			ship.AngularVelocity = angular_velocity;
		}

		// Adds sqad to GE
		general.squads.Add(data.Get<string>("name"), sqad_list.ToArray());
	}

	private void SpawnPlayer(DataStructure data) {
		FileReader.FileLog("LOADER: Load player ship");
		string ship_name = data.Get<string>("ship");
		bool side = data.Get<bool>("side");
		Vector3 pos = data.Get<Vector3>("position");
		Quaternion rot = data.Get<Quaternion>("rotation");
		GameObject ship_obj = SpawnShip(ship_name, side, pos, rot, true);

		Ship ship = ship_obj.GetComponent<ShipControl>().myship;

		if (data.Contains<Vector3>("velocity")) {
			ship.Velocity = data.Get<Vector3>("velocity");
		}
		if (data.Contains<Vector3>("angular velocity")) {
			ship.AngularVelocity = data.Get<Vector3>("angular veloity");
		}
	}

	private void SpawnSingleTarget(DataStructure data) {
		FileReader.FileLog("LOADER: Load single target");
		string tgt_name = data.Get<string>("name");
		GameObject tgt_obj = Object.Instantiate(data.Get<GameObject>("object"),
												data.Get<Vector3>("position"),
												data.Get<Quaternion>("rotation"));
		Target tgt_instance = new Target(tgt_obj, data.Get<double>("mass"), true){ side = data.Get<bool>("side") };
		DestroyableTarget tgt_inst = new DestroyableTarget(data.Get<float>("hp"), tgt_instance);
		// Add or replace in general
		if (general.single_targets.ContainsKey(tgt_name)) general.single_targets [tgt_name] = tgt_inst;
		else general.single_targets.Add(tgt_name, tgt_inst);
		BulletCollisionDetection collision_det = EnsureComponent<BulletCollisionDetection>(tgt_obj);
		collision_det.DestObj = tgt_inst;
		collision_det.is_part = false;

		if (data.Contains<Vector3>("velocity")) {
			tgt_inst.Velocity = data.Get<Vector3>("velocity");
		}
		if (data.Contains<Vector3>("angular velocity")) {
			tgt_inst.AngularVelocity = data.Get<Vector3>("angular veloity");
		}
		Debug.LogFormat("initial velocity: {0}", tgt_inst.Velocity);
	}
	# endregion

	public void ChangeScenerie (Sceneries present) {
		switch (present) {
		case Sceneries.mars:
			RenderSettings.skybox = Resources.Load<Material>("skyboxes/mars_skybox");
			Light sun = GameObject.FindGameObjectWithTag("Sun").GetComponent<Light>();
			sun.intensity = 1f;
			break;
		}
	}

	/// <summary>
	///		Creates a new ship
	/// </summary>
	/// <param name="ship_name"> How the ship is called in the configs </param>
	/// <param name="side"> The battleside of the ship </param>
	/// <param name="position"> The position of the ship in worldspace </param>
	/// <param name="rotation"> The rotation of the ship as a Quaternion </param>
	/// <param name="is_player"> True, if the ship is the player </param>
	/// <returns> The ship as a GameObject </returns>
	public static GameObject SpawnShip(string ship_name, bool side, Vector3 position, Quaternion rotation, bool is_player=false) {
		if (!Data.premade_ships.ContainsChild(ship_name)) {
			throw new System.Exception("No such ship: " + ship_name);
		}
		DataStructure ship_description = Data.premade_ships.GetChild(ship_name);
		GameObject ship_obj = Object.Instantiate(ship_description.Get<GameObject>("chassis"));
		ship_obj.transform.position = position;
		ship_obj.transform.rotation = rotation;
		ShipInitializer init = EnsureComponent<ShipInitializer>(ship_obj);
		init.player = is_player;
		init.side = side;
		init.config_path = string.Format("ships/{0}/{1}.txt", ship_description.Get<bool>("premade") ? "premade" : "user_made", ship_description.Get<string>("config"));
		init.Initialize();
		return ship_obj;
	}

	/// <summary>
	///		Returns a component of type T, if it is already there and adds it otherwise
	/// </summary>
	/// <typeparam name="T"> The type of the component </typeparam>
	/// <param name="obj"> the object of the component </param>
	/// <returns> The component </returns>
	/// <example>
	///		<code>
	///			Rigidbody rb = EnsureComponent<Rigidbody> (gameObject);
	///		</code>
	/// </example>
	public static T EnsureComponent<T> (GameObject obj) where T : Component {
		T comp = obj.GetComponent<T> ();
		if (comp == null) {
			comp = obj.AddComponent<T> ();
		}
		return comp;
	}

	/// <summary>
	///		Loads all the ammunition in a DataStructure
	/// </summary>
	/// <param name="data_base"> The datastructure to read from </param>
	/// <returns> An array with the ammunition </returns>
	public static Ammunition[] LoadAmmo (DataStructure data_base) {
		Ammunition[] res = new Ammunition[data_base.children.Count];
		for (int i=0; i < data_base.children.Count; i++) {
			DataStructure child = data_base.AllChildren[i];
			if (!Ammunition.caliber_names.ContainsKey(child.Get<string>("caliber"))) throw new System.Exception(string.Format("No such caliber {0}", child.Get<string>("caliber")));
			if (Ammunition.caliber_names.ContainsKey(child.Get<string>("caliber"))) {
				res [i] = new Ammunition(
					child.Name,
					child.Get<bool>("explosive"),
					child.Get<bool>("kinetic"),
					child.Get<bool>("timed"),
					child.Get<float>("mass"),
					Ammunition.caliber_names [child.Get<string>("caliber")],
					child.Get<GameObject>("source"),
					new Explosion(child.Get<float>("explosion force"))
				);
			}
			else {
				Data.current_os.ThrowError(string.Format("caliber {0} does not exist", child.Get<string>("caliber")));
				res [i] = Ammunition.None;
			}
		}

		return res;
	}

	/// <summary>
	///		Loads all the characters in a directory
	/// </summary>
	/// <param name="path"> The path of the directory </param>
	/// <returns> A dictionary of all the characters and their names </returns>
	public static List<Character> Load_Characters (string path) {
		List<Character> res = new List<Character>();
		string [] fnames = System.IO.Directory.GetFiles(path + "/");
		for(int i=0; i < fnames.Length; i++) {
			Character character = new Character(DataStructure.Load(fnames[i], fnames[i], Data.persistend_data, is_general: true), fnames[i]);
			res.Add(character);
		}
		return res;
	}

	public static DataStructure[] Load_Campagnes (string path) {
		string [] fnames = System.IO.Directory.GetFiles(path + "/");
		DataStructure [] res = new DataStructure [fnames.Length];
		for(int i=0; i < fnames.Length; i++) {
			res[i] = DataStructure.Load(fnames[i], fnames[i], Data.persistend_data, is_general: true);
		}
		return res;
	}
}