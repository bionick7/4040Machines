using System.Collections.Generic;
using UnityEngine;
using FileManagement;

public class HighLevelAI : MonoBehaviour {
	/// <summary> The network, this ship is part of </summary>
	public Network Net { get; set; }

	public LowLevelAI low_ai;
	public Target target;
	public IAimable turret_aim;
	private uint net_id;
	private Ship own_ship;

	private Dictionary<string, ConsiderationAction> main_dict = new Dictionary<string, ConsiderationAction>();

	public void Start_ (LowLevelAI p_low_level) {
		target = Target.None;
		low_ai = p_low_level;
		own_ship = low_ai.own_ship;
		Net = low_ai.net;

		if (Net == null) {
			GameObject[] net_holders = GameObject.FindGameObjectsWithTag("Network");
			foreach (GameObject net_holder in net_holders) {
				Network net = net_holder.GetComponent<NetworkHost>().Net;
				if (net != null) {
					if (!net.Full && net.Friendly == own_ship.Friendly) {
						Net = net;
					}
				}
			}
		}
		if (Net == null) {
			Net = own_ship.Friendly ? Network.Rogue_0 : Network.Rogue_1;
		}

		net_id = Net.AddAgent(own_ship, target);
	}

	public void Load (DataStructure ds) {
		foreach (DataStructure child in ds.AllChildren) {
			switch (child.Name) {
			case "Consider&Do":
				ConsiderationAction cons = new ConsiderationAction(own_ship);
				foreach (DataStructure child01 in child.AllChildren) {
					switch (child01.Name) {
					case "Consideration":
						cons.AddConsideration(new DataStructure[] { child01 });
						break;
					case "Any":
						DataStructure [] any_data = System.Array.FindAll(child01.AllChildren, x => x.Name == "Consideration");
						cons.AddConsideration(any_data);
						break;
					case "Action":
						cons.AddAction(child01);
						break;
					default:
						break;
					}
				}
				main_dict.Add(child.Get<string>("triggername"), cons);
				break;
			default:
				break;
			}
		}
	}

	struct Consideration {
		private ulong[] compiled_code;

		public Consideration (DataStructure data) {
			//Debug.Log("build consideration");
			List<ulong> code_list = new List<ulong>() {
				0x0100000001010000
			};
			if (data.Contains<string>("code")) {
				code_list.AddRange(NMS.OS.OperatingSystem.CompileNMS(data.Get<string>("code")));
			} else {
				code_list.AddRange(GetValue(data.Get<string>("value")));
				foreach (DataStructure child in data.AllChildren) {
					switch (child.Name) {
					case "Process":
						code_list.AddRange(Process(child));
						break;
					case "ReturnBool":
						code_list.AddRange(ReturnBool(child));
						break;
					default: break;
					}
				}
				//Debug.Log(string.Join("\n", System.Array.ConvertAll(code_list.ToArray(), x => x.ToString("x0000000000000000"))));
			}
			compiled_code = code_list.ToArray();
		}

		///// <summary> 
		/////		Gets a bool directly and saves it to &x100
		/////	</summary>
		///// <param name="data"> incoming data </param>
		///// <returns> Compiled code </returns>
		//public ulong[] GetDirectly (DataStructure data) {
		//	string type = data.Get<string>("type");
		//	switch (type) {
		//	case "random probability":
		//		ushort threshhold = NMS.RAM.Float2Short(data.Get<float>("prob"));
		//		ulong[] code = new ulong [] {
		//			0x120c002000000002,
		//			0x0100000101000000,
		//			0x0200000000000000,
		//		};
		//		code [0] += (ulong) threshhold << 16;
		//		return code;
		//	default:
		//		return new ulong[0];
		//	}
		//}
		
		/// <summary> 
		///		Gets a value and saves it to &x101
		///	</summary>
		/// <param name="data"> incoming data </param>
		/// <returns> Compiled code </returns>
		public static ulong[] GetValue (string value_name) {
			ulong[] code = new ulong[0];
			switch (value_name) {
			case "tgtdistance":
				code = new ulong [] {
					0x0104009101020000,
				};
				break;
			case "ammunition ratio":
				code = new ulong [] {
					0x0104009001010000,
					0x0400101000000000,
					0x0104009101020000,
					0x0400102000000000,
					0x230e010101020101,
					0x010c000000010000,
				};
				break;
			case "random":
				code = new ulong [] {
					0x010c002001010000
				};
				break;
			case "fuel ratio":
				code = new ulong [] {
					0x0104009201010000,
					0x0400101000000000,
					0x0104009301020000,
					0x0400102000000000,
					0x230e010101020101,
					0x010c000000010000,
				};
				break;
			case "missiles ratio":
				code = new ulong [] {
					0x0104009401010000,
					0x0400101000000000,
					0x0104009501020000,
					0x0400102000000000,
					0x230e010101020101,
					0x010c000000010000,
				};
				break;
			default:  break;
			}
			return code;
		}
		
		/// <summary> 
		///		Processes &101 and saves it to &x101 (always float)
		///	</summary>
		/// <param name="data"> incoming data </param>
		/// <returns> Compiled code </returns>
		public static ulong[] Process (DataStructure data) {
			ulong[] code = new ulong[0];
			string type = data.Get<string>("type");
			switch (type) {
			case "power":
				/*
				ushort exponent = (ushort) data.Get<int>("value");
				code = new ulong [] {
					0x0100000001020000,
					0x3104010200010102,
					0x320a010100000101,
					0x1204010200000001,
					//0x0200000000000000,
				};
				code [0] += (ulong) exponent << 32;
				code [2] += (ulong) exponent << 16;
				*/
				break;
			default: break;
			}
			return code;
		}
		
		/// <summary> 
		///		Compares the value of &x101 according to the data and saves 0x0000 (false)
		///		or 0x0001 (true) in &x100
		///	</summary>
		/// <param name="data"> incoming data </param>
		/// <returns> Compiled code </returns>
		public static ulong[] ReturnBool (DataStructure data) {
			ulong[] code = new ulong[0];
			string type = data.Get<string>("type");
			switch (type) {
			case "threshhold up":
				ushort threshhold = NMS.RAM.Float2Short(data.Get<float>("value"));
				code = new ulong [] {
					0x120a000001010002,
					0x0100000101000000,
					//0x0200000000000000,
				};
				code [0] += (ulong) threshhold << 32;
				break;

			case "threshhold down":
				ushort threshhold01 = NMS.RAM.Float2Short(data.Get<float>("value"));
				code = new ulong [] {
					0x120c010100000002,
					0x0100000101000000,
					//0x0200000000000000,
				};
				code [0] += (ulong) threshhold01 << 16;
				break;

			case "random probability":
				code = new ulong [] {
					0x120e002001010002,
					0x0100000101000000,
					//0x0200000000000000,
				};
				break;
			default:
				break;
			}
			return code;
		}

		public bool IsTrue (Ship shp) {
			//Debug.Log("considerate");
			shp.os.cpu.Execute(compiled_code);
			//Debug.Log(shp.os.ram [0x100] == 1);
			return shp.os.ram [0x100] == 1;
		}
	}

	struct Action
	{
		private ulong[] compiled_action;

		public Action (DataStructure data) {
			//Debug.Log("build action");
			compiled_action = new ulong [] { 0 };

		}

		public void Perform (Ship shp) {
			//Debug.Log("perform action");
			shp.os.cpu.Execute(compiled_action);
		}
	}

	class ConsiderationAction
	{
		public List<Consideration[]> considerations = new List<Consideration[]>() ;
		public List<Action> actions = new List<Action>();

		public Ship ship;

		public ConsiderationAction (Ship p_ship) {
			ship = p_ship;
		}

		public void AddConsideration (DataStructure[] data) {
			considerations.Add(System.Array.ConvertAll(data, x => new Consideration(x)));
		}

		public void AddAction (DataStructure data) {
			actions.Add(new Action(data));
		}

		public void Do () {
			bool check = true;
			foreach (Consideration[] consids in considerations) {
				if (!System.Array.Exists(consids, x => x.IsTrue(ship))) {
					check = false;
				}
			}
			if (check) {
				foreach (Action action in actions) {
					action.Perform(ship);
				}
			}
		}
	}

	public enum ConsiderationTypes
	{
		tgtdistance,
		ammunition_ratio,
		random,
		ammunition_left,
		missiles_ratio,
	}

	/// <summary> 
	///		Looks, if another target is wastly more important, than the curretn one.
	///		If yes, switches targets.
	/// </summary>
	private void SearchTgt () {
		var ennemy_ships = low_ai.ennemy_ships;
		if (ennemy_ships.Count == 0) {
			target = Target.None;
			return;
		}
		Ship default_ship = null;
		foreach (Ship s in ennemy_ships) {
			default_ship = s;
			break;
		}
		Ship leading_ship = !target.Exists ? default_ship : target.Ship;
		bool current_over =  Net.Strength_Ratio(leading_ship.Associated) > 3;
		if (!current_over && target.Exists) {
			return;
		}
		foreach (Ship ship in ennemy_ships) {
			if (ship.Exists) {
				bool switch_motivation = ship.Importance > leading_ship.Importance || current_over;
				if (switch_motivation ) {
					leading_ship = ship;
				}
			}
		}
		if (leading_ship.Importance > target.Importance || !target.Exists) {
			target = leading_ship.Associated;
			Net.Switch_Target(net_id, target);
		}
	}

	public void Update_ () {
		if (target == null | Time.frameCount % 100 == net_id % 100) {
			SearchTgt();
		}

		if (turret_aim == null) {
			turret_aim = target;
		}

		if (Time.frameCount == 0) {
			low_ai.action_list.Add(AIActionCommand.ShootTG(own_ship.TurretGroups[0], turret_aim));
		}

		Execute("ShootFixedOn");
		Execute("ShootFixedOff");
		Execute("ShootMissile");
	}

	public void Execute(string action_name) {
		if (main_dict.ContainsKey(action_name)) {
			main_dict [action_name].Do();
		}
	}
}
