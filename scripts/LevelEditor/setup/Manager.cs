using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FileManagement;

/* ===================================================
 * The Manager in the upper-left.
 * Responsible for loading, saving and spawning things
 * =================================================== */

public class Manager : MonoBehaviour {

	public InputField path_field;
	public Dropdown spawn_selector;
	public Dropdown environnement;

	private DataStructure[] ships_data;

	private void Start () {
		path_field.text = "battles/editortest";
		ships_data = Globals.premade_ships.AllChildren;
		var option_data = new List<Dropdown.OptionData>(System.Array.ConvertAll(ships_data, x => new Dropdown.OptionData(x.Name)));
		spawn_selector.options = option_data;
	}

	/// <summary>
	///		Loads a battle.
	///		Should be called, when the load button is pressed
	/// </summary>
	public void LoadBattle () {
		EditorGeneral.active.Clear();
		string path = DataStructure.GeneralPath + path_field.text + ".cfgt";

		if (!File.Exists(path)) {
			FileReader.FileLog(string.Format("File \"{0}\" does not exist!", path), FileLogType.error);
		}
		DataStructure battle_data = DataStructure.Load(path, "battledata", is_general:true);
		FileReader.FileLog(string.Format("Loaded {0} into editor", path), FileLogType.editor);

		foreach(DataStructure child in battle_data.AllChildren) {
			switch (child.Name) {
			// Story
			case "story":
				var stages = child.AllChildren;
				StoryStage last = null;
				for (int i=0; i < stages.Length; i++) {
					DataStructure stage = stages[i];
					StoryStage current_stage = StoryManager.active.SpawnStoryStage();
					if (i == 0) {
						current_stage.IsFirst = true;
					} else {
						current_stage.SetParent(last);
					}
					foreach (DataStructure command in stage.AllChildren) {
						switch (command.Name) {
						case "spawn":
							current_stage.Spawn_SpawnNode(command.Get<string []>("types"), command.Get<string []>("names"));
							break;
						case "get conversation":
							current_stage.Spawn_ConversationNode(command.Get<ushort>("ID"));
							break;
						case "objective":
							current_stage.Spawn_ObjectiveNode(command.Get<string>("objective type"), command.Get<string>("target name"));
							break;
						}
					}
					last = current_stage;
				}
				break;
			
			// Implications
			case "player squadron":
			case "squadron":
				Squadron squad = new Squadron(child.Get<string>("name"), child.Get<bool>("friendly"));

				// Leader
				string leader_ship_name = child.Get<string>("leader", quiet: true);
				if (leader_ship_name != "NULL" & child.Contains<string>("leader")) {
					GameObject leader_prefab = Globals.premade_ships.GetChild(leader_ship_name).Get<GameObject>("chassis");
					GameObject leader = Instantiate(leader_prefab);
					EDShip leader_ship = new EDShip(leader_ship_name, child.Get<Vector3>("leader pos"), child.Get<Quaternion>("leader rot").eulerAngles) {
						Velocity = child.Get("leader vel", Vector3.zero, quiet: true),
						AngularVelocity = child.Get("leader angvel", Vector3.zero, quiet: true)
					};
					leader.AddComponent<Movable>().correspondence = leader_ship;
					squad.leader = leader_ship;
				} else
					squad.leader = null;

				// Squad
				string[] names = child.Get<string[]>("ships");
				GameObject[] ship_objs = System.Array.ConvertAll(names,
					x => Globals.premade_ships.GetChild(x).Get<GameObject>("chassis")
				);

				Quaternion[] def_rot = child.Get<Quaternion []>("orientations");
				Vector3[] positions = child.Get<Vector3 []>("positions");
				Vector3[] velocities = child.Get<Vector3 []>("velocities", quiet: true);
				Vector3[] angular_velocities = child.Get<Vector3 []>("angular velocities", quiet: true);

				bool has_velocities = velocities != default(Vector3[]);
				bool has_ang_velocities = angular_velocities != default(Vector3[]);

				List<EDShip> ship_collection = new List<EDShip>();
				for (ushort i=0; i < positions.Length; i++) {
					GameObject ship_obj = Instantiate(ship_objs[i]);
					EDShip ship = new EDShip(names[i], positions [i], def_rot [i].eulerAngles);
					if (has_velocities) ship.Velocity = velocities [i];
					if (has_ang_velocities) ship.AngularVelocity = angular_velocities [i];
					ship_obj.AddComponent<Movable>().correspondence = ship;
					ship_collection.Add(ship);
				}
				EditorGeneral.squadron_list.Add(squad);

				// Asssgn to squad
				if (squad.leader != null)
					squad.leader.AssignSilent(squad);
				ship_collection.ForEach(x => x.Squad = squad);

				break;
			case "target":
				DSPrefab ds_pref = child.Get<DSPrefab>("object");
				GameObject tgt_obj = Instantiate(ds_pref.obj);
				EDTarget tgt = new EDTarget(child.Get<string>("name"), child.Get<bool>("friendly"), ds_pref) {
					Position = child.Get<Vector3>("position"),
					Rotation = child.Get<Quaternion>("rotation").eulerAngles
				};
				tgt_obj.AddComponent<Movable>().correspondence = tgt;
				EditorGeneral.target_list.Add(tgt);
				break;
			default: break;
			}
		}

		EditorGeneral.active.Reload();
	}
	
	/// <summary>
	///		Saves a battle.
	///		Should be called, when the save button is pressed
	/// </summary>
	public void SaveBattle () {
		string path = DataStructure.GeneralPath + path_field.text + ".cfgt";

		DataStructure res = new DataStructure();
		DataStructure general = new DataStructure("general", res);
		general.Set("planet", "mars");
		bool is_player_squad = false;
		ushort player_num = 0;

		//	Story
		// -------------
		StoryStage first = StoryStage.FirstStage;
		if (first != null) first.GetTotalDS(res);


		//	Implications
		// --------------
		for (int i=0; i < EditorGeneral.squadron_list.Count; i++) {
			Squadron squad = EditorGeneral.squadron_list[i];
			if (squad.name == "default") continue;

			is_player_squad = squad.ships.Exists(x => x.IsPlayer) | (squad.leader != null && squad.leader.IsPlayer);
			DataStructure squad_data = new DataStructure(is_player_squad ? "player squadron" : "squadron", res);
			squad_data.Set("name", squad.name);
			squad_data.Set("friendly", squad.friendly);
			if (squad.leader == null) {
				squad_data.Set("leader", "NULL");
			} else { 
				squad_data.Set("leader pos", squad.leader.Position);
				squad_data.Set("leader rot", Quaternion.Euler(squad.leader.Rotation));
				squad_data.Set("leader vel", squad.leader.Velocity);
				squad_data.Set("leader angvel", squad.leader.AngularVelocity);
				squad_data.Set("leader", squad.leader.name);
			}
			int ship_num = squad.ships.Count;
			string[] names = new string[ship_num];
			Vector3[] positions = new Vector3[ship_num];
			Quaternion[] rotations = new Quaternion[ship_num];
			Vector3[] velocities = new Vector3[ship_num];
			Vector3[] angular_velocities = new Vector3[ship_num];
			for (int j=0; j < ship_num; j++) {
				EDShip ship = squad.ships[j];
				names [j] = ship.name;
				positions [j] = ship.Position;
				rotations [j] = Quaternion.Euler(ship.Rotation);
				velocities [j] = ship.Velocity;
				angular_velocities [j] = ship.AngularVelocity;
				if (ship.IsPlayer) player_num = (ushort) (j + 1);
			}
			if (is_player_squad) squad_data.Set("player ship", player_num);
			squad_data.Set("ships", names);
			squad_data.Set("positions", positions);
			squad_data.Set("orientations", rotations);
			squad_data.Set("velocities", velocities);
			squad_data.Set("angular velocities", angular_velocities);
		}

		for (int i=0; i < EditorGeneral.target_list.Count; i++) {
			EDTarget tgt = EditorGeneral.target_list[i];
			DataStructure tgt_data = new DataStructure("target", res);
			tgt_data.Set("hp", tgt.hp);
			tgt_data.Set("mass", tgt.mass);
			tgt_data.Set("name", tgt.name);
			tgt_data.Set("friendly", tgt.friendly);
			tgt_data.Set("position", tgt.Position);
			tgt_data.Set("rotation", Quaternion.Euler(tgt.Rotation));
			tgt_data.Set("velocity", tgt.Velocity);
			tgt_data.Set("angular velocity", tgt.AngularVelocity);
			tgt_data.Set("object", tgt.pref);
		}

		// Debug.Log(string.Join("\n", res.ToText()));
		res.Save(path, true);

		FileReader.FileLog(string.Format("Saved to {0} sucessfully", path), FileLogType.editor);
	}

	public void UpdateEvironnement () {

	}

	/// <summary> Spawn selected ship </summary>
	public void Spawn () {
		DataStructure ship_data = ships_data[spawn_selector.value];
		GameObject instance = Instantiate(ship_data.Get<GameObject>("chassis"));
		Loader.EnsureComponent<Movable>(instance).correspondence = new EDShip(ship_data.Name, Vector3.zero, Vector3.zero) {
		};
	}

	/// <summary> Add a new squadron </summary>
	public void SpawnSquadron () {
		string sqname = "new squad";
		ushort i = 0;
		while (EditorGeneral.squadron_list.Exists(x => x.name == sqname))
			sqname = i++.ToString("new squad000");
		EditorGeneral.squadron_list.Add(new Squadron(sqname, true));
		ShipInspector.active.ReloadSquads();
	}

	/// <summary> Change to story view </summary>
	public void StoryView () {
		EditorGeneral.StoryView = true;
	}
}
