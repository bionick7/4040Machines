using UnityEngine;
using System.Collections.Generic;
using FileManagement;

public static class LoadSaving
{
	public static void Save () {
		GeneralExecution general = SceneGlobals.general;

		System.DateTime t0 = System.DateTime.Now;
		DataStructure save_datastructure = new DataStructure();

		DataStructure general_information = new DataStructure("GeneralInformation", save_datastructure);
		general_information.Set("original_path", general.battle_path);
		general_information.Set("in level progress", (int) general.mission_core.in_level_progress);
		general_information.Set("in stage progress", (int) general.mission_core.in_stage_progress);

		ReferenceSystem ref_sys = SceneGlobals.ReferenceSystem;
		if (ref_sys.HasParent) {
			general_information.Set("RS offset", ref_sys.Offset);
			general_information.Set("RS parent", ref_sys.ref_obj.ID);
		} else {
			general_information.Set("RS position", ref_sys.Position);
		}

		SceneObject[] scene_array = new SceneObject[SceneObject.TotObjectList.Count];
		Explosion[] explosion_array = new Explosion[SceneGlobals.explosion_collection.Count];
		Bullet[] bullet_array = new Bullet[SceneGlobals.bullet_collection.Count];
		DestroyableTarget[] target_array = new DestroyableTarget[SceneGlobals.destroyables.Count];

		SceneObject.TotObjectList.Values.CopyTo(scene_array, 0);
		SceneGlobals.bullet_collection.CopyTo(bullet_array);
		SceneGlobals.destroyables.CopyTo(target_array);

		scene_array = System.Array.FindAll(scene_array, 
			x => 
				!(x is Missile && !(x as Missile).Released) && 
				!(x is Network && (x as Network).Name == "\"friendly rogue\"-Network" | (x as Network).Name == "\"hostile rogue\"-Network") && 
				!(x is Target)
		);

		ISavable[] savable_objects = new ISavable[scene_array.Length +
												  explosion_array.Length +
												  bullet_array.Length +
												  target_array.Length];

		int indx = 0;
		System.Array.ConvertAll(scene_array,		x => x as ISavable).CopyTo(savable_objects, indx);
		indx += scene_array.Length;
		System.Array.ConvertAll(explosion_array,	x => x as ISavable).CopyTo(savable_objects, indx);
		indx += explosion_array.Length;
		System.Array.ConvertAll(bullet_array,		x => x as ISavable).CopyTo(savable_objects, indx);
		indx += bullet_array.Length;
		System.Array.ConvertAll(target_array,		x => x as ISavable).CopyTo(savable_objects, indx);

		DataStructure object_states = new DataStructure("ObjectStates", save_datastructure);

		foreach (ISavable obj in savable_objects) {
			if (obj != null) {
				DataStructure ds = new DataStructure(obj.Name, object_states);
				obj.Save(ds);
			}
		}

		//save_datastructure.Save("saved/Saves/" + System.DateTime.Now.ToString("dd_MM_yyyy_hh_mm_ss"));
		save_datastructure.Save("saved/Saves/def_save");
		DeveloppmentTools.LogFormat("Saved: {0} ms", (System.DateTime.Now - t0).Milliseconds.ToString());
	}

	public static void Load (string path) {
		DataStructure saved = DataStructure.Load(path);
		DataStructure general_information = saved.GetChild("GeneralInformation");
		DataStructure original_file = DataStructure.Load(general_information.Get<string>("original_path"), is_general: true);

		FileReader.FileLog("Begin Loading", FileLogType.loader);
		GameObject placeholder = GameObject.Find("Placeholder");
		GeneralExecution general = placeholder.GetComponent<GeneralExecution>();
		general.battle_path = path;

		// Initiate Operating system
		general.os = new NMS.OS.OperatingSystem(Object.FindObjectOfType<ConsoleBehaviour>(), null);

		// Initiate mission core
		Loader partial_loader = new Loader(original_file);
		general.mission_core = new MissionCore(general.console, partial_loader);

		general.mission_core.in_level_progress = (short) general_information.Get<int>("in level progress");
		general.mission_core.in_stage_progress = (short) general_information.Get<int>("in stage progress");
		DeveloppmentTools.Log("start loading");
		partial_loader.LoadEssentials();
		
		DataStructure objects = saved.GetChild("ObjectStates");
		Debug.Log(objects);
		foreach (DataStructure child in objects.AllChildren) {
			int id = child.Get<ushort>("type", 1000, quiet:true);
			switch (id) {
			case 0:
				// Ship
				Dictionary<string, Turret[]> weapon_arrays = new Dictionary<string, Turret[]>();

				string config_path = child.Get<string>("config path");
				bool is_friendly = child.Get<bool>("friendly");
				bool is_player = child.Get<bool>("player");
				int given_id = child.Get<int>("id");
				GameObject ship_chassis = Loader.SpawnShip(config_path, is_friendly, is_player, false, pre_id: given_id);

				//LowLevelAI ai = Loader.EnsureComponent<LowLevelAI>(ship_chassis);
				//ai.HasHigherAI = !is_player;

				ShipControl ship_control = ship_chassis.GetComponent<ShipControl>();
				Ship ship = ship_control.myship;
				//ship.control_script.ai_low = ai;
				//ship.low_ai = ai;

				int netID = child.Get("parent network", is_friendly ? 1 : 2);
				if (SceneObject.TotObjectList.ContainsKey(netID) && SceneObject.TotObjectList [netID] is Network)
					ship.high_ai.Net = SceneObject.TotObjectList [netID] as Network;

				ship.Position = child.Get<Vector3>("position");
				ship.Orientation = child.Get<Quaternion>("orientation");
				ship.Velocity = child.Get<Vector3>("velocity");
				ship.AngularVelocity = child.Get<Vector3>("angular velocity");

				foreach (DataStructure child01 in child.AllChildren) 
					switch (child01.Get<ushort>("type", 9, quiet:true)) {
					case 1: // weapon
						Weapon.GetFromDS(child01.GetChild("description"), child01, ship.Transform);
						break;
					case 3: // fuel tank
						FuelTank.GetFromDS(child01.GetChild("description"), child01, ship.Transform);
						break;
					case 4: // engine
						Engine.GetFromDS(child01.GetChild("description"), child01, ship.Transform);
						break;
					case 10: // ammo box
						AmmoBox.GetFromDS(child01.GetChild("description"), child01, ship.Transform);
						break;
					case 11: // missile launcher
						MissileLauncher.GetFromDS(child01.GetChild("description"), child01, ship.Transform);
						break;
					case 12: // armor
						Armor.GetFromDS(child01, ship);
						break;
					default:
						if (child01.Name.StartsWith("turr-")) {
							var tg = TurretGroup.Load(child01, ship);
							weapon_arrays [child01.Name.Substring(5)] = tg.TurretArray;
							ship_control.turretgroup_list.Add(new TurretGroup(Target.None, tg.TurretArray, tg.name) { own_ship = ship });
						}
						break;
					}
				

				// Initializes parts
				foreach (BulletCollisionDetection part in ship_chassis.GetComponentsInChildren<BulletCollisionDetection>()) {
					part.Initialize();
				}
				ship_control.turrets = weapon_arrays;

				ship.os.cpu.Execute(child.Get<ulong []>("code"));

				if (is_player) {
					SceneGlobals.Player = ship;
					SceneGlobals.ui_script.Start_();
				}

				break;
			case 1: // Missile
				Missile.SpawnFlying(child);
				break;
			case 2: // Bullet
				Bullet.Spawn(
					Globals.ammunition_insts [child.Get<string>("ammunition")],
					child.Get<Vector3>("position"),
					Quaternion.FromToRotation(Vector3.forward, child.Get<Vector3>("velocity")),
					child.Get<Vector3>("velocity"),
					child.Get<bool>("is_friend")
				);
				break;
			case 3: // Destroyable target
				DestroyableTarget.Load(child);
				break;
			case 4: // Explosion

				break;
			}
		}

		general.os.Attached = SceneGlobals.Player;

		ReferenceSystem ref_sys;
		if (general_information.Contains<Vector3>("RS position")) {
			ref_sys = new ReferenceSystem(general_information.Get<Vector3>("RS position"));
		} else {
			int parent_id = general_information.Get<int>("RS parent");
			if (SceneObject.TotObjectList.ContainsKey(parent_id))
				ref_sys = new ReferenceSystem(SceneObject.TotObjectList [parent_id]);
			else ref_sys = new ReferenceSystem(Vector3.zero);
			ref_sys.Offset = general_information.Get<Vector3>("RS offset");
		}
		SceneGlobals.ReferenceSystem = ref_sys;
	}
}
