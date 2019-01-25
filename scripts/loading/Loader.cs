using System.Collections.Generic;
using System.Text.RegularExpressions;
using FileManagement;
using UnityEngine;

/* ===================================================================
 * Contains the Loader object to load a scene and spawn stuff from the 
 * Battle's datastructure
 * =================================================================== */

public class Loader
{
	private DataStructure BattleData { get; set; }

	private GameObject placeholder;
	private GeneralExecution general;
	private MissionCore m_core;

	public string path;

	public AttackData attack_data;

	// There are more efficient data storing methods than DataStructures!
	public Dictionary<string, DataStructure> single_targets_dict = new Dictionary<string, DataStructure>();
	private Dictionary<string, DataStructure> squadrons_dict = new Dictionary<string, DataStructure>();

	public static readonly Dictionary<string, Sceneries> scenerie_dict = new Dictionary<string, Sceneries>() {
		{ "mars", Sceneries.mars },
		{ "earth", Sceneries.earth }
	};

	public Loader (DataStructure data) {
		BattleData = data;
	}

	public static void ThrowError(string message) {
		FileReader.FileLog(message, FileLogType.error);
		Debug.LogError(message);
	}

	/// <summary> Loads a battle </summary>
	public void LoadObjects () {
		FileReader.FileLog("Begin Loading Objects", FileLogType.loader);

		foreach (DataStructure child in BattleData.AllChildren) {
			string name = child.Name;
			switch (name) {
			case "squadron":
				string sqad_name = child.Get("name", "");
				if (squadrons_dict.ContainsKey(sqad_name)) {
					ThrowError(string.Format("Squad named \"{0}\" already exists", sqad_name));
				} else {
					squadrons_dict.Add(sqad_name, child);
				}
				break;

			case "player squadron":
				SpawnSquadron(child, true);
				break;
				
			case "target":
				string tgt_name = child.Get("name", "");
				if (single_targets_dict.ContainsKey(tgt_name)) {
					ThrowError(string.Format("Target named \"{0}\" already exists", tgt_name));
				} else {
					single_targets_dict.Add(tgt_name, child);
				}
				break;
			}
		}
	}

	/// <summary> Loads a battle </summary>
	public void LoadEssentials () {
		FileReader.FileLog("Begin Loading", FileLogType.loader);
		placeholder = GameObject.Find("Placeholder");
		general = placeholder.GetComponent<GeneralExecution>();
		general.loader = this;
		general.battle_path = path;

		m_core = general.mission_core;

		List<DataStructure> conversations = new List<DataStructure>();
		foreach (DataStructure child in BattleData.AllChildren) {
			string name = child.Name;
			switch (name) {
			case "general":
				ChangeScenerie(scenerie_dict [child.Get<string>("planet")]);
				SceneGlobals.battlefiled_size = child.Get("size", 1000, quiet: true);
				SceneGlobals.ship_camera.farClipPlane = SceneGlobals.battlefiled_size;
				SceneGlobals.map_camera.farClipPlane = SceneGlobals.battlefiled_size;
				break;

			case "conversation":
				conversations.Add(child);
				break;

			case "story":
				FileReader.FileLog("Load Story", FileLogType.loader);
				Dictionary<ushort, DataStructure> story = new Dictionary<ushort, DataStructure>();
				foreach(DataStructure stage in child.AllChildren) {
					// Checks if string matches "StageX", where X is an integer between 0 and 9999
					if (Regex.IsMatch(stage.Name, @"^[sS]tage\d{1,4}$")) {
						ushort number = System.UInt16.Parse(stage.Name.Substring(5));
						story [number] = stage;
					}
				}
				m_core.story = story;
				m_core.in_level_progress = (short) (child.Contains<ushort>("startstage") ? child.Get<ushort>("startstage") : 0);
				break;
			}
		}

		m_core.conversations = conversations.ToArray();
	}

	public void Spawn(string type, string name) {
		switch (type) {
		case "squadron":
			if (!squadrons_dict.ContainsKey(name)) {
				ThrowError("No such squadron: " + name);
				DeveloppmentTools.Log("no such squadron: " + name);
				return;
			}
			SpawnSquadron(squadrons_dict [name]);
			break;
		case "single target":
			if (!single_targets_dict.ContainsKey(name)) {
				ThrowError("No such single target: " + name);
				DeveloppmentTools.Log("no such single target: " + name);
				return;
			}
			SpawnSingleTarget(single_targets_dict [name]);
			break;
		}
	}

	// Here, you can find different spawners
	# region Spawners
	/// <summary> Spawns a whole squadron </summary>
	private void SpawnSquadron(DataStructure data, bool is_player=false) {
		FileReader.FileLog("Load squadron", FileLogType.loader);
		// Create the host
		List<Ship> squad_list = new List<Ship>();
		NetworkHost host;
		Ship leader_ship = null;
		if (!data.Contains<string>("leader") || data.Get<string>("leader") == "NULL") {
			host = EnsureComponent<NetworkHost>(placeholder);
		} else {
			GameObject leader_obj = SpawnShipFromName(data.Get("leader", "Placeholder"),
													  data.Get("friendly", true),
													  data.Get("leader pos", Vector3.zero),
													  data.Get("leader rot", Quaternion.identity),
													  !is_player || data.Get<ushort>("player ship") != 0);
			host = EnsureComponent<NetworkHost>(leader_obj);
			leader_ship = leader_obj.GetComponent<ShipControl>().myship;

			//LowLevelAI ai = EnsureComponent<LowLevelAI>(leader_obj);
			//ai.HasHigherAI = !is_player || data.Get<ushort>("player ship") != 0;
			leader_ship.low_ai.net = host.Net;

			//leader_ship.control_script.ai_low = ai;
			//leader_ship.low_ai = ai;

			squad_list.Add(leader_ship);
			if (data.Contains<Vector3>("leader velocity")) {
				leader_ship.Velocity = data.Get<Vector3>("leader velocity");
			}
			if (data.Contains<Vector3>("leader angular velocity")) {
				leader_ship.AngularVelocity = data.Get<Vector3>("angular veloity");
			}
		}
		host.Net = new Network((uint) data.Get<Vector3 []>("positions").Length, data.Get<bool>("friendly"), data.Get<string>("name"));

		// Create the fleet
		bool friendly = data.Get("friendly", true);

		string [] ships = data.Get("ships", new string[0]);
		int gen_length = ships.Length;

		Quaternion [] rotations = data.Get<Quaternion []>("orientations");

		Vector3[] null_vecs = new Vector3[gen_length];
		Vector3[] angular_velocities = data.Get("angular velocities", null_vecs, quiet: true);

		null_vecs = new Vector3[gen_length];
		Vector3[] positions = data.Get<Vector3 []>("positions");
		Vector3[] velocities = data.Get("velocities", null_vecs, quiet: true);
		if (is_player) {
			for (int i=0; i < gen_length; i++) {
				positions [i] += SceneGlobals.battlefiled_size * attack_data.AttackPosition;
				velocities [i] = attack_data.AttackVelocity;
			}
		}

		//Debug.Log(DeveloppmentTools.LogIterable(angular_velocities));

		if (gen_length != positions.Length ||
			gen_length != rotations.Length ||
			gen_length != velocities.Length ||
			gen_length != angular_velocities.Length) {
			ThrowError("array lengths do not match");
		} else {
			for (int i = 0; i < positions.Length; i++) {
				GameObject ship_obj = SpawnShipFromName(ships[i], friendly, positions[i], rotations[i], !is_player || i + 1 != data.Get<ushort>("player ship"));
				Ship ship = ship_obj.GetComponent<ShipControl>().myship;
				
				//LowLevelAI ai = EnsureComponent<LowLevelAI>(ship_obj);
				//ai.HasHigherAI = !is_player || i + 1 != data.Get<ushort>("player ship");
				ship.low_ai.net = host.Net;

				//ship.control_script.ai_low = ai;
				//ship.low_ai = ai;
				squad_list.Add(ship);

				ship.Velocity = velocities [i];
				ship.AngularVelocity = angular_velocities [i];
			}
		}

		// Adds sqad to GE
		if (m_core.squads.ContainsKey(data.Get<string>("name"))) {
			ThrowError(string.Format("Squad {0} already exists", data.Get<string>("name")));
			DeveloppmentTools.LogIterable(m_core.squads.Keys);
		}
		m_core.squads.Add(data.Get<string>("name"), squad_list.ToArray());

		if (is_player) {
			FileReader.FileLog("Load Squadron Player", FileLogType.loader);
			SceneGlobals.ui_script.Start_();

			ushort player_ship_num = data.Get<ushort>("player ship");
			if (player_ship_num > squad_list.Count) {
				ThrowError(string.Format("Player ship number ({0}) out of range", player_ship_num));
			}
			if (player_ship_num == 0) {
				if (leader_ship == null)
					ThrowError("There is no leader ship in this squad");
				SceneGlobals.Player = leader_ship;
			} else
				SceneGlobals.Player = squad_list[player_ship_num - 1];
		}
	}

	/// <summary> Spawns a single target </summary>
	private void SpawnSingleTarget(DataStructure data) {
		FileReader.FileLog("Load single target", FileLogType.loader);
		string tgt_name = data.Get<string>("name");
		GameObject tgt_obj = Object.Instantiate(data.Get<GameObject>("object"),
												data.Get<Vector3>("position"),
												data.Get<Quaternion>("rotation"));
		Target tgt_instance = new Target(tgt_obj, data.Get<double>("mass"), true){ Friendly = data.Get<bool>("friend") };
		DestroyableTarget tgt_inst = new DestroyableTarget(data.Get<float>("hp"), tgt_instance, data.Get<DSPrefab>("object"));
		// Add or replace in general
		if (m_core.single_targets.ContainsKey(tgt_name)) m_core.single_targets [tgt_name] = tgt_inst;
		else m_core.single_targets.Add(tgt_name, tgt_inst);
		BulletCollisionDetection collision_det = EnsureComponent<BulletCollisionDetection>(tgt_obj);
		collision_det.DestObj = tgt_inst;
		collision_det.is_part = false;

		if (data.Contains<Vector3>("velocity")) {
			tgt_inst.Velocity = data.Get<Vector3>("velocity");
		}
		if (data.Contains<Vector3>("angular velocity")) {
			tgt_inst.AngularVelocity = data.Get<Vector3>("angular velocity");
		}
	}
	# endregion

	/// <summary> Changes the scenerie </summary>
	/// <param name="present"> Scenerie; can be anything </param>
	public static void ChangeScenerie (Sceneries present) {
		switch (present) {
		case Sceneries.mars:
			RenderSettings.skybox = Resources.Load<Material>("skyboxes/mars_skybox");
			Light sun = GameObject.FindGameObjectWithTag("Sun").GetComponent<Light>();
			RenderSettings.sun = sun;
			sun.intensity = 1f;
			break;
		}
	}

	/// <summary>
	///		Creates a new ship
	/// </summary>
	/// <param name="ship_path"> How the ship is called in the configs </param>
	/// <param name="friendly"> If the ship is on the same side as the player </param>
	/// <param name="is_player"> True, if the ship is the player </param>
	/// <returns> The ship as a GameObject </returns>
	public static GameObject SpawnShip(string ship_path, bool friendly, bool is_player=false, bool include_parts=true, int pre_id=-1) {
		DataStructure ship_ds = DataStructure.Load(ship_path);

		GameObject ship_obj = Object.Instantiate(ship_ds.GetChild("ship").Get<GameObject>("chassis"));

		ShipInitializer init = EnsureComponent<ShipInitializer>(ship_obj);
		init.data_ = ship_ds;
		init.player = is_player;
		init.friendly = friendly;
		init.config_path = ship_path;
		init.include_parts = include_parts;
		init.Initialize(pre_id);
		FileReader.FileLog("Initialization compleate: " + ship_obj.name, FileLogType.loader);
		return ship_obj;
	}

	/// <summary>
	///		Creates a new ship
	/// </summary>
	/// <param name="ship_name"> How the ship is called in the configs </param>
	/// <param name="friendly"> If the ship is on the same side as the player </param>
	/// <param name="position"> The position of the ship in worldspace </param>
	/// <param name="rotation"> The rotation of the ship as a Quaternion </param>
	/// <param name="is_player"> True, if the ship is the player </param>
	/// <returns> The ship as a GameObject </returns>
	public static GameObject SpawnShipFromName (string ship_name, bool friendly, Vector3 position, Quaternion rotation, bool is_player=false) {
		if (!Globals.premade_ships.ContainsChild(ship_name)) {
			ThrowError("No such ship: " + ship_name);
			return GameObject.CreatePrimitive(PrimitiveType.Cube);
		}
		DataStructure ship_description = Globals.premade_ships.GetChild(ship_name);
		string config_path = string.Format("ships/{0}/{1}", ship_description.Get<bool>("premade") ? "premade" : "user_made", ship_description.Get<string>("config"));
		GameObject ship_obj = SpawnShip(config_path, friendly, is_player);
		ship_obj.transform.position = position;
		ship_obj.transform.rotation = rotation;
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
			if (Ammunition.caliber_names.ContainsKey(child.Get<string>("caliber"))) {
				if (Ammunition.caliber_names.ContainsKey(child.Get<string>("caliber"))) {
					res [i] = new Ammunition(
						child.Name,
						child.Get<bool>("explosive"),
						child.Get<bool>("kinetic"),
						child.Get<bool>("timed"),
						child.Get<float>("mass"),
						Ammunition.caliber_names [child.Get<string>("caliber")],
						child.Get<GameObject>("source"),
						child.Get<float>("explosion force")
					);
				}
				else {
					string error_mes = string.Format("caliber {0} does not exist", child.Get<string>("caliber"));
					DeveloppmentTools.Log(error_mes);
					ThrowError(error_mes);
					res [i] = Ammunition.None;
				}
			} else {
				DeveloppmentTools.LogFormat("No such caliber {0}", child.Get<string>("caliber"));
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
			Character character = new Character(DataStructure.Load(fnames[i], fnames[i], Globals.persistend_data, is_general: true), fnames[i]);
			res.Add(character);
		}
		return res;
	}

	/// <summary> Loads a bunch of campagnes from a path </summary>
	/// <param name="path"> The source path of the campagnes </param>
	/// <returns> The campagnes as datastructures </returns>
	public static DataStructure[] Load_Campagnes (string path) {
		string [] fnames = System.IO.Directory.GetFiles(path + "/");
		DataStructure [] res = new DataStructure [fnames.Length];
		for(int i=0; i < fnames.Length; i++) {
			res[i] = DataStructure.Load(fnames[i], fnames[i], Globals.persistend_data, is_general: true);
		}
		return res;
	}
}

public struct AttackData
{
	/// <summary> Normalized vector, that points to position on rim </summary>
	public Vector3 AttackPosition { get; private set; }
	/// <summary> Velociity of the ships </summary>
	public Vector3 AttackVelocity { get; private set; }

	public AttackData (Vector3 p_pos, Vector3 p_vel) {
		AttackPosition = p_pos;
		AttackVelocity = p_vel;
	}
}