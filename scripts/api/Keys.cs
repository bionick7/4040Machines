using UnityEngine;
using System.Collections.Generic;
using FileManagement;
using System;

/* ===============================================
 * The KEy Bindings and how to save/load/call them
 * ================================================ */

/// <summary> The structure, that remembers all the keybindings </summary>
public class KeyBindingCollection
{
	#region key fields
	public KeyBinding togglemap;
	public KeyBinding pause;
	public KeyBinding menu;

	public KeyBinding camera_switch;
	public KeyBinding fire_missile;
	public KeyBinding shoot;
	public KeyBinding turret_shoot;

	public KeyBinding map_move_fore;
	public KeyBinding map_move_back;
	public KeyBinding map_move_left;
	public KeyBinding map_move_right;
	public KeyBinding map_move_up;
	public KeyBinding map_move_down;

	public KeyBinding yaw_left;
	public KeyBinding yaw_right;
	public KeyBinding pitch_up;
	public KeyBinding pitch_down;
	public KeyBinding roll_left;
	public KeyBinding roll_right;

	public KeyBinding translate_up;
	public KeyBinding translate_down;
	public KeyBinding translate_left;
	public KeyBinding translate_right;
	public KeyBinding translate_fore;
	public KeyBinding translate_back;

	public KeyBinding increase_throttle;
	public KeyBinding decrease_throttle;
	public KeyBinding throttle_max;
	public KeyBinding throttle_min;

	public KeyBinding kill_rotation;
	public KeyBinding cancel_mouse_following;
	#endregion

	public Dictionary<string, List<KeyBinding>> binding_dict = new Dictionary<string, List<KeyBinding>>();
	public KeyBinding[] AllBindings {
		get {
			List<KeyBinding> res = new List<KeyBinding>();
			foreach (List<KeyBinding> binding_list in binding_dict.Values) {
				res.AddRange(binding_list);
			}
			return res.ToArray();
		}
	}

	/// <summary> The datastructure containing the keybindings </summary>
	private DataStructure keydata;
	private string path;

	private DataStructure general;
	private DataStructure map;
	private DataStructure movement;
	private DataStructure engine;

	/// <param name="pdata"> The datastruncture of the file, where the keybindings are saved </param>
	public KeyBindingCollection (string datapath) {
		path = datapath;
		DataStructure data = DataStructure.Load(datapath, "keybindings");
		keydata = data.GetChild("keys");
		general = keydata.GetChild("general");
		map = keydata.GetChild("map");
		movement = keydata.GetChild("movement");
		engine = keydata.GetChild("engine");
		Load();
	}

	/// <summary>
	///		Initializes every binding
	/// </summary>
	private void Load () {
		binding_dict.Clear();

		togglemap = Get("map", general);
		pause = Get("pause", general);
		menu = Get("menu", general);

		camera_switch = Get("camera", general);
		fire_missile = Get("fire missile", general);
		shoot = Get("shoot", general);
		turret_shoot = Get("shoot turret", general);

		map_move_fore  = Get("map move forward", map);
		map_move_back  = Get("map move backward", map);
		map_move_left  = Get("map move left", map);
		map_move_right = Get("map move right", map);
		map_move_up    = Get("map move up", map);
		map_move_down  = Get("map move down", map);

		pitch_up = Get("pitch up", movement);
		pitch_down = Get("pitch down", movement);
		yaw_left = Get("yaw left", movement);
		yaw_right = Get("yaw right", movement);
		roll_left = Get("roll left", movement);
		roll_right = Get("roll right", movement);

		translate_fore  = Get("translate forward", movement);
		translate_back  = Get("translate backward", movement);
		translate_left  = Get("translate left", movement);
		translate_right = Get("translate right", movement);
		translate_up    = Get("translate up", movement);
		translate_down  = Get("translate down", movement);

		increase_throttle = Get("increase throttle", engine);
		decrease_throttle = Get("decrease throttle", engine);
		throttle_max = Get("max throttle", engine);
		throttle_min = Get("min throttle", engine);

		kill_rotation = Get("kill rotation", keydata);
		cancel_mouse_following = Get("cancel mouse following", keydata);
	}

	public void Save () {
		var data = new DataStructure();
		data.children.Add( "keys", keydata);
		data.Save(path);
		Load();
	}

	/// <summary> Searches a keybinding from the file </summary>
	/// <param name="inp"> The name of the binding </param>
	/// <param name="source"> The source datastructure (where to search)</param>
	/// <returns> The "KeyBinding" Object </returns>
	private KeyBinding Get(string inp, DataStructure source) {
		KeyBinding res;
		if (source.Contains<ushort[]>(inp))
			res = GetFromString(source.Get<ushort[]>(inp), inp);
		else
			res = GetFromString(source.Get<ushort>(inp), inp);
		if (binding_dict.ContainsKey(source.Name))
			binding_dict [source.Name].Add(res);
		else
			binding_dict.Add(source.Name, new List<KeyBinding>() { res });
		return res;
	}

	private KeyBinding GetFromString (ushort pkeyid, string pfunction) {
		KeyCode[] code = new KeyCode[] { (KeyCode) pkeyid };
		KeyBinding res = new KeyBinding() {
			act_code = code,
			function = pfunction,
			keyids = new ushort[] { pkeyid }
		};
		return res;
	}

	private KeyBinding GetFromString (ushort[] pkeyid, string pfunction) {
		KeyCode[] code = new KeyCode[pkeyid.Length];
		for (int i=0; i < pkeyid.Length; i++) {
			code [i] = (KeyCode) pkeyid [i];
		}
		KeyBinding res = new KeyBinding() {
			act_code = code,
			function = pfunction,
			keyids = pkeyid
		};
		return res;
	}

	public void Change (string func, KeyBinding newbinding) {
		foreach(DataStructure child in keydata.AllChildren) {
			if (child.Contains<ushort>(func)) {
				if (newbinding.act_code.Length == 1) {
					child.Set(func, newbinding.keyids [0]);
				} else {
					child.short_integers_arr.Remove(func);
					child.short_integers.Add(func, newbinding.keyids [0]);
				}
				return;
			}
			if (child.Contains<ushort []>(func)) {
				if (newbinding.act_code.Length == 1) {
					child.short_integers.Remove(func);
					child.short_integers_arr.Add(func, newbinding.keyids);
				} else {
					child.Set(func, newbinding.keyids);
				}
				return;
			}
		}
		DeveloppmentTools.LogFormat("Keybinding \"{0}\" not found", func);
	}

	/// <summary>
	///		Returns the current binding of a certain funtion
	/// </summary>
	public KeyBinding GetByFunction (string function) {
		foreach (KeyBinding binding in AllBindings) {
			if (binding.function == function) {
				return binding;
			}
		}
		DeveloppmentTools.LogFormat("No such key function exists: \"{0}\"", function);
		return KeyBinding.None;
	}

	private void SaveAt (KeyBinding binding, DataStructure source) {
		if (binding.keyids.Length == 1)
			source.short_integers.Add(binding.function, binding.keyids [0]);
		else
			source.short_integers_arr.Add(binding.function, binding.keyids);
	}
}

/// <summary> A particular keybinding </summary>
public struct KeyBinding
{
	public KeyCode[] act_code;
	public string function;
	public ushort[] keyids;

	/// <summary> If a keybinding is pressed </summary>
	/// <returns> bool, true if pressed else false </returns>
	public bool IsPressed () {
		return Array.TrueForAll(act_code, key => Input.GetKey(key));
	}

	/// <summary> Same as IsPressed, but only trigers once per pressing </summary>
	public bool ISPressedDown () {
		return Array.TrueForAll(act_code, key => Input.GetKeyDown(key));
	}

	/// <summary> Same as IsPressed, but only trigers once per pressing </summary>
	public bool ISUnPressed () {
		return Array.TrueForAll(act_code, key => Input.GetKeyUp(key));
	}

	public static implicit operator KeyCode[] (KeyBinding binding) {
		return binding.act_code;
	}

	public override string ToString () {
		return string.Format("{0} : {1}", function, string.Join(",\n", 
			Array.ConvertAll(act_code, c => string.Format("Key[\"{0}\"]", c.ToString()))
		));
	}

	public static readonly KeyBinding None = new KeyBinding {
		act_code = new KeyCode [0],
		function = "NONEBINDING",
		keyids = new ushort [0]
	};
}