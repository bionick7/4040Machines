using System.Collections.Generic;
using UnityEngine;

/*
 * This script basically creates and configures all the other components.
 * It is used one, at the very beginning of the scene for each object,
 * it is attached to
 */

public class ShipInitializer : MonoBehaviour {

	/// <summary>
	///		config_path is the path of the concerned file inside of the "configs" folder in the Assets
	///	</summary>
	public string config_path;

	/// <summary>
	///		Set to true, if the player is supposed to contorl this ship.
	///		This should be true for exactly one vessel in the scene.
	///	</summary>
	public bool player=false;

	public bool side;

	public void Initialize () {
		//Read the data
		string [] txt_lines = new string[0];
		DataStructure data = DataStructure.Load(config_path, "data", null);

		// Variables used to place weapons
		Dictionary<string, Turret[]> weapon_arrays = new Dictionary<string, Turret[]> ();

		//First set up some basic things
		Ship selfship = new Ship(gameObject){ side = side };

		ShipControl ship_control = Loader.EnsureComponent<ShipControl> (gameObject);
		RCSFiring rcs_comp= Loader.EnsureComponent<RCSFiring> (gameObject);
		Loader.EnsureComponent<AudioSource>(gameObject);


		//GameObject map_pointer = Instantiate(GameObject.Find("map_selecter"));
		//map_pointer.transform.SetParent(SceneData.map_canvas.transform);
		//MapDrawnObject map_image = SceneData.mapdrawer.AddSingleSprite(map_pointer, selfship);

		ship_control.myship = selfship;

		if (player  && GameObject.FindGameObjectWithTag("Player") == null) {
			goto PLAYER;
		}

		GameObject marker = Instantiate(GameObject.Find("tgt_pos_marker")) as GameObject;
		marker.transform.SetParent(GameObject.Find("Canvas").transform);
		marker.transform.SetSiblingIndex(1);
		marker.GetComponent<TgtMarker>().parent = selfship;
		marker.GetComponent<TgtMarker>().is_original = false;
		goto MAIN;

PLAYER:
		gameObject.tag = "Player";
		DataStructure player_data = data.GetChild("player");

		Camera cam = SceneData.ship_camera;
		GameObject main_camera = cam.gameObject;
		cam.farClipPlane = 100000f;
		CameraMovement camera_script = main_camera.AddComponent<CameraMovement>();
		try {
			camera_script.init_dpos = player_data.Get<Vector3 []>("cam pos") [0];
			camera_script.init_relrot = player_data.Get<Quaternion []>("cam rot") [0];
		} catch { }
		main_camera.transform.SetParent(transform);

		PlayerControl control = gameObject.AddComponent<PlayerControl> ();
		control.positions = player_data.Get<Vector3[]>("cam pos");
		control.rotations = player_data.Get<Quaternion[]>("cam rot");
		control.free_rotation = player_data.Get<bool[]>("free rotate");

		SceneData.Player = selfship;

MAIN:
		foreach (KeyValuePair<string, DataStructure> child_pair in data.children){
			// Do this for each "command" in the datafile
			DataStructure child = child_pair.Value;
			string comp_name = child.Name;
			
			DataStructure part_data;
			if (child.Contains<string>("part")) {
				string part_name = child.Get<string>("part");
				part_data = Data.parts.Get<DataStructure>(part_name);
			} else {
				part_data = child;
			}
			switch (comp_name) {
			case "rcs":
				ship_control.RCS_ISP = child.Get<float>("isp");

				rcs_comp.rcs_mesh = child.Get<GameObject>("mesh");
				rcs_comp.strength = child.Get<float>("thrust");
				rcs_comp.angular_limitation = child.Get<float>("angular limitation", 1);
				rcs_comp.positions = child.Get<Vector3[]>("positions");
				rcs_comp.directions = child.Get<Quaternion[]>("orientations");
				break;

			case "ship":
				if (rcs_comp != null) {
					rcs_comp.center_of_mass = child.Get<Vector3>("centerofmass");
				}
				selfship.offset = child.Get<Vector3>("centerofmass");
				break;

			case "engine":
				GameObject engine_obj = Instantiate(part_data.Get<GameObject>("source"));
				engine_obj.transform.position = transform.position + transform.rotation * child.Get<Vector3>("position");
				engine_obj.transform.rotation = transform.rotation * child.Get<Quaternion>("rotation");
				engine_obj.transform.SetParent(transform, true);

				float hp = (float) part_data.Get<ushort>("hp");

				Engine engine_instance = new Engine(hp, engine_obj, part_data.Get<float>("mass"), part_data.Get<float>("thrust")) {
					SpecificImpulse = part_data.Get<float>("isp"),
					Direction = child.Get<Quaternion>("rotation") * Vector3.back,
				};

				BulletCollisionDetection engine_behaviour = Loader.EnsureComponent<BulletCollisionDetection>(engine_obj);
				engine_behaviour.Part = engine_instance;
				break;

			case "tank":
				GameObject tank_obj = Instantiate(part_data.Get<GameObject>("source"));
				tank_obj.transform.position = transform.position + transform.rotation * child.Get<Vector3>("position");
				tank_obj.transform.rotation = transform.rotation * child.Get<Quaternion>("rotation");
				tank_obj.transform.SetParent(transform, true);

				FuelTank tank_instance = new FuelTank((float) part_data.Get<ushort>("hp"), tank_obj, part_data.Get<float>("mass")) {
					isrcs = part_data.Get<bool>("rcs"),
					ismain = part_data.Get<bool>("main"),
					Fuel = part_data.Get<float>("fuel"),
				};

				BulletCollisionDetection tank_behaviour = Loader.EnsureComponent<BulletCollisionDetection>(tank_obj);
				tank_behaviour.Part = tank_instance;

				break;

			case "fix weapon":
				GameObject weapon_obj = part_data.Get<GameObject>("source");
				Vector3 position = child.Get<Vector3>("position");
				Quaternion rotation = child.Get<Quaternion>("rotation");

				GameObject act_weapon_obj = Instantiate(weapon_obj);
				act_weapon_obj.transform.position = transform.position + transform.rotation * position;
				act_weapon_obj.transform.rotation = transform.rotation * rotation;
				act_weapon_obj.transform.SetParent(transform, true);

				Weapon weapon_instance = new Weapon((float) part_data.Get<ushort>("hp"), act_weapon_obj, part_data.Get<float>("mass")) {
					empty_hull = part_data.Get<GameObject>("hullpref"),
					BulletSpeed = part_data.Get<float>("bulletspeed"),
					HullSpeed = part_data.Get<float>("hullspeed"),
					ShootPos = part_data.Get<Vector3>("bulletpos"),
					EjectPos = part_data.Get<Vector3>("hullpos"),
					ReloadSpeed = part_data.Get<float>("reloadspeed"),
				};

				BulletCollisionDetection weapon_behaviour = Loader.EnsureComponent<BulletCollisionDetection>(act_weapon_obj);
				weapon_behaviour.Part = weapon_instance;
				break;

			case "ammobox":
				GameObject box_obj = Instantiate(part_data.Get<GameObject>("source"));
				box_obj.transform.position = transform.position + transform.rotation * child.Get<Vector3>("position");
				box_obj.transform.rotation = transform.rotation * child.Get<Quaternion>("rotation");
				box_obj.transform.SetParent(transform, true);

				if (!Data.ammunition_insts.ContainsKey(part_data.Get<string>("ammotype"))) throw new System.Exception(string.Format("No such ammo: {0}", part_data.Get<string>("ammotype")));

				AmmoBox box_instance = new AmmoBox((float) part_data.Get<ushort>("hp"), box_obj, part_data.Get<float>("mass")) {
					AmmoType = Data.ammunition_insts[part_data.Get<string>("ammotype")],
					Ammunition = part_data.Get<System.UInt16>("ammo")
				};

				BulletCollisionDetection box_behaviour = Loader.EnsureComponent<BulletCollisionDetection>(box_obj);
				box_behaviour.Part = box_instance;

				break;

			case "missiles":

				GameObject launcher_obj = Instantiate(part_data.Get<GameObject>("source"));
				launcher_obj.transform.position = transform.position + transform.rotation * child.Get<Vector3>("position");
				launcher_obj.transform.rotation = transform.rotation * child.Get<Quaternion>("rotation");
				launcher_obj.transform.SetParent(transform, true);

				MissileLauncher launcher_instance = new MissileLauncher((float) part_data.Get<ushort>("hp"), launcher_obj, part_data.Get<float>("mass")) {
					missile_source = part_data.Get<GameObject>("missile source"),
					Positions = part_data.Get<Vector3[]>("positions"),
					orientation = part_data.Get<Quaternion>("orientation"),
					acceleration = part_data.Get<float>("acceleration"),
					flight_duration = part_data.Get<float>("duration")
				};

				BulletCollisionDetection launcher_behaviour = Loader.EnsureComponent<BulletCollisionDetection>(launcher_obj);
				launcher_behaviour.Part = launcher_instance;

				launcher_instance.Spawn();
				break;

			default:
				if (comp_name.StartsWith("turr-")) {
					string name = comp_name;

					// Range
					float [] range = new float[4] {-1f, -1f, -1f, -1f};
					if (part_data.Contains<float[]>("horizontal range")) {
						range [0] = Mathf.Abs(Mathf.Min(part_data.Get<float[]>("horizontal range")));
						range [1] = Mathf.Abs(Mathf.Max(part_data.Get<float[]>("horizontal range")));
					}
					if (part_data.floats32_arr.ContainsKey("vertical range")) {
						range [2] = Mathf.Abs(Mathf.Min(part_data.Get<float[]>("vertical range")));
						range [3] = Mathf.Abs(Mathf.Max(part_data.Get<float[]>("vertical range")));
					}

					float horizontal_rate = part_data.Get<float>("horizontal rotating rate");
					float vertical_rate = part_data.Get<float>("vertical rotating rate");

					uint ammo = part_data.short_integers["ammunition"];
					float reload_speed = part_data.Get<float>("reload speed");
					float muzzle_velocity = part_data.Get<float>("muzzle velocity");
					Vector3 [] muzzle_positions = part_data.Get<Vector3[]>("barrels");

					int count = child.Get<Vector3[]>("positions").Length;
					Vector3 [] weapon_pos = child.Get<Vector3[]>("positions");
					Quaternion [] weapon_rot = child.Get<Quaternion[]>("rotations");
					GameObject pref_weapon = part_data.Get<GameObject>("source");

					Turret [] weapon_array = new Turret[count];

					//This is for each weapon in it
					for (int i = 0; i < count; i++) {
						Vector3 guns_p = transform.position + transform.rotation * weapon_pos[i];
						Quaternion guns_rot = transform.rotation * weapon_rot[i];
						GameObject turret_object = Instantiate(pref_weapon, guns_p, guns_rot);
						turret_object.transform.SetParent(this.transform);
						turret_object.name = string.Format("{0} ({1})", name, i.ToString());

						Turret turret_instance = new Turret(range, turret_object, new float[2] { horizontal_rate, vertical_rate}, part_data.Get<float>("mass"), part_data.Get<System.UInt16>("hp")){
							name = turret_object.name,
							ammo_count = ammo,
							full_ammunition = ammo,
							reload_speed = reload_speed,
							muzzle_velocity = muzzle_velocity,
							ammo_type = Data.ammunition_insts[part_data.Get<string>("ammotype")],
							muzzle_positions = muzzle_positions
						};

						weapon_array [i] = turret_instance;

						BulletCollisionDetection turret_behaviour = Loader.EnsureComponent<BulletCollisionDetection>(turret_object);
						turret_behaviour.Part = turret_instance;
					}

					weapon_arrays [name] = weapon_array;
					ship_control.turret_aims.Add(new TurretGroup(Target.None, weapon_array, name) { parentship = selfship });
				}
				break;
			}
		}
		ship_control.turrets = weapon_arrays;
	}
}