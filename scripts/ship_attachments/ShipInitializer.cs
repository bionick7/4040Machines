using System.Collections.Generic;
using UnityEngine;
using FileManagement;

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
	public bool include_parts = true;

	public DataStructure data_;

	public bool friendly;

	public void Initialize (int pregiven_id=-1) {
		//Read the data
		DataStructure data = data_ == null ? data_ = DataStructure.Load(config_path, "data", null) : data_;

		// Variables used to place weapons
		Dictionary<string, Turret[]> weapon_arrays = new Dictionary<string, Turret[]> ();

		//First set up some basic things
		Ship own_ship = new Ship(gameObject, friendly, data.Get<string>("name"), pregiven_id);
		
		ShipControl ship_control = Loader.EnsureComponent<ShipControl> (gameObject);
		RCSFiring rcs_comp = Loader.EnsureComponent<RCSFiring> (gameObject);

		own_ship.control_script = ship_control;
		own_ship.rcs_script = rcs_comp;
		own_ship.config_path = config_path;
		own_ship.TurretAim = own_ship.Target = Target.None;

		ship_control.myship = own_ship;

		// AI
		LowLevelAI ai = Loader.EnsureComponent<LowLevelAI>(gameObject);
		ai.Start_();
		ai.HasHigherAI = !player;

		HighLevelAI high_ai = own_ship.high_ai;
		//high_ai.Load(child.GetChild("ai data"));

		high_ai.low_ai = ai;

		// Instantiates normal UI marker
		TgtMarker.Instantiate(own_ship, 1);

		foreach (KeyValuePair<string, DataStructure> child_pair in data_.children){
			// Do this for each "command" in the datafile
			DataStructure child = child_pair.Value;
			string comp_name = child.Name;
			
			DataStructure part_data;
			if (child.Contains<string>("part")) {
				string part_name = child.Get<string>("part");
				part_data = Globals.parts.Get<DataStructure>(part_name);
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
				own_ship.offset = child.Get<Vector3>("centerofmass");
				break;

			case "AI":
				high_ai.Load(child.GetChild("ai data"));
				break;

			case "engine":
				if (!include_parts) break;
				Engine.GetFromDS(part_data, child, transform);
				break;

			case "tank":
				if (!include_parts) break;
				FuelTank.GetFromDS(part_data, child, transform);
				break;

			case "fix weapon":
				if (!include_parts) break;
				Weapon.GetFromDS(part_data, child, transform);
				break;

			case "ammobox":
				if (!include_parts) break;
				AmmoBox.GetFromDS(part_data, child, transform);
				break;

			case "missiles":
				if (!include_parts) break;
				MissileLauncher.GetFromDS(part_data, child, transform);
				break;

			case "armor":
				if (!include_parts) break;
				Armor.GetFromDS(child, own_ship);
				break;

			default:
				if (comp_name.StartsWith("turr-")) {
					if (!include_parts) break;
					var tg = TurretGroup.Load(child, own_ship);
					weapon_arrays [comp_name.Substring(5)] = tg.TurretArray;
					ship_control.turretgroup_list.Add(new TurretGroup(Target.None, tg.TurretArray, tg.name) { own_ship = own_ship });
				}
				break;
			}
		}

		// Initializes parts
		foreach (BulletCollisionDetection part in GetComponentsInChildren<BulletCollisionDetection>()) {
			part.Initialize();
		}
		ship_control.turrets = weapon_arrays;
	}
}