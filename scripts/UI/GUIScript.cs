using System.Collections.Generic;
using FileManagement;
using UnityEngine;
using UnityEngine.UI;

/*
 * This module is responsible for managing the player User Interface (UI)
 * This should be part of the player's ship.
 */

public class GUIScript : MonoBehaviour {

	/// <summary> Dictionnary with the "pointers", images, that point at the target, velocity ... </summary>
	private Dictionary<string, Image> pointers = new Dictionary<string, Image> ();
	/// <summary> Dictionnary with the health-, ammo-, ... indicators </summary>
	private Dictionary<string, Slider> slider_bars = new Dictionary<string, Slider> ();
	/// <summary> Dictionnary to store text </summary>
	private Dictionary<string, Text> texts = new Dictionary<string, Text> ();
	/// <summary> Dictionary to store buttons </summary>
	private Dictionary<string, Button> buttons = new Dictionary<string, Button> ();


	/// <summary> Dictionary containing the Buttons of the individual turrets and the corresponding turrets </summary>
	private Dictionary<TurretButton, Turret> TurretGraphs;
	/// <summary> Dictionary containing the Buttons of the turretgroups and the corresponding turretgroups </summary>
	public Dictionary<TurretButton, TurretGroup> TurretGroupGraphs { get; private set; }

	private Dictionary<Weapon, Image> weapon_states = new Dictionary<Weapon, Image>();
	private Dictionary<Turret, Image> turret_states = new Dictionary<Turret, Image>();

	private Slider scroll_bar;

	private Image turret_menu;
	private Image group_menu;
	private Image menu;
	public AddGroupMenu add_group_menu;
	public ConsoleBehaviour console;

	public Vector3 HighlightingPosition { get; set; }

	//Other stuff we need

	private Transform ship_canvas;
	private Transform main_canvas;
	private AudioSource audio_src;

	// Player related stuff
	private ShipControl player_script;
	public Ship PlayerShip { get; private set; }
	private Transform player_transform;

	private Transform camera_transform;
	private Camera MainCam {
		get { return SceneGlobals.general.InMap ? SceneGlobals.map_camera : SceneGlobals.ship_camera; }
	}

	private Vector3 tgting_point = Vector3.zero;

	// Current and selected units
	public Turret selected_turret = null;
	public TurretGroup selected_group = null;
	public List<Turret> selected_turrets = new List<Turret>(){};
	public List<TurretGroup> selected_groups = new List<TurretGroup>(){};
	public bool group_follows_cursor = false;

	private Turret current_turret;
	private TurretGroup current_group;

	private float [] handle_size_ratio = new float[2];

	private bool mouse_on_menu;
	private bool turret_selected = false;
	private bool group_selected = false;
	private Vector3 direction = Vector3.zero;

	private bool group_switch;

	/// <summary> 
	///		False, if individual turrets are shown;
	///		True, if entire groups are shown
	/// </summary>FdiF
	public bool GroupSwitch {
		get { return group_switch; }
		set {
			group_switch = value;
			TurretSliderUpdate();
		}
	}

	public Vector3 CursorWorldDirection {
		get {
			Ray ray = camera_transform.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
			return (ray.direction * 1000000 + (camera_transform.position - player_transform.position));
		}
	}

	public Vector3 Middle {
		get { return new Vector3(Screen.width / 2, Screen.height / 2); }
	}

	private bool _paused;

	public bool Paused {
		get { return _paused; }
		set {
			FileReader.FileLog(value ? "Paused" : "UnPaused", FileLogType.runntime);
			SceneGlobals.general.OnPause(value);
			_paused = value;
		}
	}

	public void Start_ () {
		ship_canvas = GameObject.Find("MainCanvas").transform;
		main_canvas = GameObject.Find("PermanentCanvas").transform;
		console = main_canvas.GetComponentInChildren<ConsoleBehaviour>();
		console.Start_();
		menu = GameObject.Find("menu").GetComponent<Image>();
		camera_transform = GameObject.Find("ShipCamera").transform;
		audio_src = GetComponent<AudioSource>();
		HighlightingPosition = new Vector3(-200, -200);

		// The dictionnaries are filled here
		#region dictionnaries
		pointers.Add("prograde_marker", GameObject.Find("prograde_marker").GetComponent<Image> ());
		pointers.Add("target_velocity_marker", GameObject.Find("tgt_vel_marker").GetComponent<Image>());
		pointers.Add("turret_position", GameObject.Find("turr_pos").GetComponent<Image>());
		pointers.Add("direction_pointer", GameObject.Find("direction_marker").GetComponent<Image>());
		pointers.Add("prograde", GameObject.Find("prog").GetComponent<Image>());

		texts.Add("velocity_indicator", GameObject.Find("vel").GetComponent<Text>());
		texts.Add("angular_momentum_indicator", GameObject.Find("ang_mom").GetComponent<Text>());
	
		slider_bars.Add("fuel_bar", GameObject.Find("FuelBar").GetComponent<Slider> ());
		slider_bars.Add("rcs_bar", GameObject.Find("RCSBar").GetComponent<Slider> ());
		slider_bars.Add("ammo_bar", GameObject.Find("AmmoBar").GetComponent<Slider> ());
		slider_bars.Add("missile_bar", GameObject.Find("RocketBar").GetComponent<Slider> ());
		slider_bars.Add("hitpoints_bar", GameObject.Find("HPBar").GetComponent<Slider> ());

		buttons.Add("turret_switch", GameObject.Find("turr_switch").GetComponent<Button> ());

		scroll_bar = GameObject.Find("turret_scroll").GetComponent<Slider>();
		turret_menu = GameObject.Find("turret_menu").GetComponent<Image>();
		group_menu = GameObject.Find("turretgroup_menu").GetComponent<Image>();
		add_group_menu = GameObject.Find("add_group_menu").GetComponent<AddGroupMenu>();
		#endregion
	}

	public void ResetPlayer (Ship p_player) {
		PlayerShip = p_player;

		// Initialize player related stuff
		GameObject player = PlayerShip.Object;
		player_script = player.GetComponent<ShipControl>();
		player_transform = player.transform;

		// Reset Collections
		weapon_states.Clear();
		turret_states.Clear();

		foreach (Weapon w in PlayerShip.Parts.GetAll<Weapon>()) {
			GameObject obj = Instantiate(GameObject.Find("tgt_point")) as GameObject;
			obj.transform.SetParent(ship_canvas);
			Image img = obj.GetComponent<Image>();
			weapon_states.Add(w, img);
		}

		foreach (Turret t in PlayerShip.Parts.GetAll<Turret>()) {
			GameObject obj = Instantiate(GameObject.Find("tgt_point")) as GameObject;
			obj.transform.SetParent(ship_canvas);
			Image img = obj.GetComponent<Image>();
			turret_states.Add(t, img);
		}


		// Fill turret_graphs and initialize the buttons' scripts
		// -------------------------------------------------------

		TurretGraphs = new Dictionary<TurretButton, Turret>();
		TurretGroupGraphs = new Dictionary<TurretButton, TurretGroup>();

		GameObject turr_img_instance = GameObject.Find("turr_slide");
		uint i=0;
		foreach (Turret [] turrs in player_script.turrets.Values) {
			foreach (Turret turr in turrs) {
				GameObject obj = Instantiate(turr_img_instance);
				obj.transform.SetParent(SceneGlobals.permanent_canvas.transform);
				obj.transform.position = new Vector3(Screen.width - 50, Screen.height - 75 - (i * 50));
				obj.transform.SetAsFirstSibling();

				TurretButton button = obj.GetComponent<TurretButton>();
				button.Initiate(this, i, turr);

				TurretGraphs.Add(button, turr);

				i++;
			}
		}

		handle_size_ratio[0] = Mathf.Min(1, ((float) Screen.height - 250f) / (50f * i));

		// Fill group_graphs and initialize the buttons' scripts
		//------------------------------------------------------

		for (int j=0; j < player_script.turretgroup_list.Count; j++) {
			TurretGroup tg = player_script.turretgroup_list[j];
			GameObject obj = Instantiate(turr_img_instance);
			obj.transform.SetParent(SceneGlobals.permanent_canvas.transform);
			obj.transform.position = new Vector3(Screen.width + 50, Screen.height - 75 - (j * 50));
			obj.transform.SetAsFirstSibling();

			TurretButton button = obj.GetComponent<TurretButton>();
			button.Initiate(this, (uint) j, tg);

			TurretGroupGraphs.Add(button, tg);
		}

		handle_size_ratio [1] = Mathf.Min(1, ((float) Screen.height - 250f) / (50f * player_script.turretgroup_list.Count));

		ButtonLabelUpdate();
	}

	#region public_functions

	/// <summary> should be called, if mouse enter/exits over a button referring to a turret </summary>
	/// <param name="button_id"> The ID of the concerned button </param>
	/// <param name="enter"> True, if mouse enters the button; False, if it exits it</param>
	public void TurretButtonHover (uint button_id, bool enter) {
		if (enter) {
			float plus_value = scroll_bar.value * ((Screen.height - 250) / handle_size_ratio[0] - (Screen.height - 250));
			turret_menu.rectTransform.position = new Vector3(Screen.width - 170, Screen.height - 100 - button_id * 50 + plus_value);
			selected_turret = new List<Turret>(TurretGraphs.Values) [(int) button_id];

			int group_number = 0;
			List<Dropdown.OptionData> opts = new List<Dropdown.OptionData>();
			for (int i=0; i < player_script.turretgroup_list.Count; i++) {
				TurretGroup tg = player_script.turretgroup_list[i];
				opts.Add(new Dropdown.OptionData(tg.name));
				if (selected_turret.Group == tg) {
					group_number = i;
				}
			}

			Dropdown group_select = turret_menu.GetComponentInChildren<Dropdown>();
			group_select.options = opts;
			group_select.value = group_number;

			turret_selected = true;
		} else {
			turret_selected = false;
			HighlightingPosition = new Vector3(-200, -200);
		}
	}

	/// <summary> should be called, if mouse enter/exits over a button referring to a turretgroup </summary>
	/// <param name="button_id"> The ID of the concerned button </param>
	/// <param name="enter"> True, if mouse enters the button; False, if it exits it</param>
	public void GroupButtonHover (uint button_id, bool enter) {
		if (enter) {
			TurretGroup concerned_group = player_script.turretgroup_list [(int) button_id];
			TurretButton concerned_button = null;
			foreach (KeyValuePair<TurretButton, TurretGroup> pair in TurretGroupGraphs) {
				if (pair.Value == concerned_group) concerned_button = pair.Key;
			}
			if (concerned_button == null) throw new System.ArgumentException("Turretgroup ID does not exist");
			uint button_number = 0;
			foreach (TurretButton bt in TurretGroupGraphs.Keys) {
				if (bt.transform.position.y > concerned_button.transform.position.y) button_number++;
			}

			float plus_value = scroll_bar.value * ((Screen.height - 250) / handle_size_ratio[1] - (Screen.height - 250));
			group_menu.rectTransform.position = new Vector3(Screen.width - 170, Screen.height - 150 - button_number * 50 + plus_value);
			selected_group = player_script.turretgroup_list [(int) button_id];
			group_selected = true;
		}
		if (!enter) {
			group_selected = false;
		}
	}


	public void ChangeGroup () {
		TurretButton selected_button = null;
		foreach (KeyValuePair<TurretButton, Turret> pair in TurretGraphs) {
			if (pair.Value == selected_turret) {
				selected_button = pair.Key;
			}
		}
		if (selected_button != null) {
			int group_number = selected_button.GetComponentInChildren<Dropdown>().value;
			selected_turret.Group = player_script.turretgroup_list [group_number];
		}
	}

	/// <summary>
	///		This should be called, if a turretgroup should be added.
	/// </summary>
	public void AddTurretGroup (TurretGroup tg) {
		// Adds the group to the playerobjects main script
		player_script.turretgroup_list.Add(tg);

		int groups_num = player_script.turretgroup_list.Count;

		// Adds new buttons
		GameObject obj = Instantiate(GameObject.Find("turr_slide"));
		obj.transform.SetParent(main_canvas);
		obj.transform.position = new Vector3(Screen.width + 50, Screen.height - 75 - (groups_num * 50));
		obj.transform.SetAsFirstSibling();
		Text txt = obj.transform.GetChild(0).GetComponent<Text>();
		txt.text = tg.name;
		TurretButton button = obj.GetComponent<TurretButton>();
		TurretGroupGraphs.Add(button, tg);

		obj.GetComponent<TurretButton>().Initiate(this, (uint) groups_num - 1u, tg);

		handle_size_ratio [1] = Mathf.Min(1, ((float) Screen.height - 250f) / (50f * (float) groups_num));

	}

	/// <summary> Lets the selected group aim at the current direction </summary>
	public void TurretGroupAimDirection () {
		if (selected_group == null) { return; }
		selected_group.follow_target = false;
		selected_group.direction = true;
		group_follows_cursor = false;
	}

	public void TurretGroupAimCursor () {
		if (selected_group == null) { return; }
		selected_group.follow_target = false;
		selected_group.direction = true;
		group_follows_cursor = !group_follows_cursor;
		selected_group.SetTgtDir(direction);
		selected_group.direction = true;
		current_group = selected_group;
	}

	/// <summary> Lets the selected group aim at the current target </summary>
	public void TurretGroupAimTarget () {
		if (selected_group == null) { return; }
		selected_group.target = PlayerShip.TurretAim;
		selected_group.follow_target = true;
		group_follows_cursor = false;
		selected_group.direction = false;
	}

	/// <summary> Deletes the selected group </summary>
	public void DeleteTurretGroup () {
		if (selected_group == null) { return; }
		Turret [] turretarr = new Turret[selected_group.Count];
		selected_group.TurretArray.CopyTo(turretarr, 0);
		foreach (Turret t in turretarr) {
			t.Group = TurretGroup.Trashbin;
		}
		TurretButton bt = null;
		foreach (KeyValuePair<TurretButton, TurretGroup> pair in TurretGroupGraphs) {
			if (pair.Value == selected_group) {
				bt = pair.Key;
			}
		}
		if (bt != null) {
			TurretGroupGraphs.Remove(bt);
			Destroy(bt.gameObject);
		}
	}

	/// <summary> Calls the turretgroup menu group </summary>
	/// <param name="is_edit"> True if the turretgroup should just be edited </param>
	public void CallGroupMenu (bool is_edit) {
		if (is_edit && selected_group == null) { return; }
		add_group_menu.Edit = is_edit;
		if (is_edit) {
			add_group_menu.group = selected_group;
			current_group = selected_group;
		} else {
			add_group_menu.preselected_turrets = selected_turrets;
		}
		add_group_menu.Enabled = true;
	}

	/// <summary> Calls the menu </summary>
	public void ToggleMenu () {
		bool call = menu.transform.position.y < 0;
		menu.transform.position = call ? Middle : new Vector3(600, -1000);
		if (call) Paused = true;
	}

	public void SetDirection () {
		direction = CursorWorldDirection;
	}

	#endregion

	/// <summary> 
	///		Updates everythin in "slider_bars".
	///		Should be updated each frame.
	/// </summary>
	private void SliderUpdate () {
		slider_bars ["hitpoints_bar"].value = PlayerShip.HPRatio;
		slider_bars ["fuel_bar"].value = PlayerShip.FuelRatio;
		slider_bars ["rcs_bar"].value = PlayerShip.RCSFuelRatio;
		slider_bars ["ammo_bar"].value = PlayerShip.AmmoRatio;
		slider_bars ["missile_bar"].value = PlayerShip.MissileRatio;

		foreach(Slider slt in slider_bars.Values) {
			Color color = new Color(1 - slt.value, slt.value, .07f);
			slt.fillRect.gameObject.GetComponent<Image>().color = color;
		}
	}

	/// <summary>
	///		Updates the turret- and groupbuttons.
	///		Should be updated each frame.
	/// </summary>
	private void TurretSliderUpdate () {
		float act_value = scroll_bar.value;

		float pos_multiplyer = (Screen.height - 250) / handle_size_ratio[0] - (Screen.height - 250);
		List<TurretButton> all_buttons = new List<TurretButton>(TurretGraphs.Keys);
		float side_add = GroupSwitch ? +50 : -70;
		for (int i=0; i < TurretGraphs.Count; i++) {
			TurretButton bt = all_buttons[i];
			bt.transform.position = new Vector3(Screen.width + side_add, Screen.height - 75 - i*50 + act_value * pos_multiplyer);
		}

		float pos_multiplyer_1 = (Screen.height - 250) / handle_size_ratio[1] - (Screen.height - 250);
		List<TurretButton> all_buttons_1 = new List<TurretButton>(TurretGroupGraphs.Keys);
		side_add = !GroupSwitch ? +50 : -70;
		for (int i = 0; i < TurretGroupGraphs.Count; i++) {
			TurretButton bt = all_buttons_1[i];
			bt.transform.position = new Vector3(Screen.width + side_add, Screen.height - 75 - i * 50 + act_value * pos_multiplyer_1);
		}
	}

	public void OnWindowResize () {
		int groups_num = TurretGroupGraphs.Count;
		int turrets_num = TurretGraphs.Count;
		handle_size_ratio = new float [2] { Mathf.Min(1, ((float) Screen.height - 250f) / (50f * (float) turrets_num)),
											Mathf.Min(1, ((float) Screen.height - 250f) / (50f * (float)  groups_num)) };
	}

	/// <summary>
	///		Updates the ammunition text on the buttons.
	///		Hasn't necessarily to be called every frame
	/// </summary>
	public void AmmoUpdate () {
		foreach (KeyValuePair<TurretButton, Turret> pair in TurretGraphs) {
			pair.Key.AmmoUpdate();
		}
		foreach (KeyValuePair<TurretButton, TurretGroup> pair in TurretGroupGraphs) {
			pair.Key.AmmoUpdate();
		}
	}


	public void ButtonLabelUpdate () {
		foreach (TurretButton bt in TurretGraphs.Keys) {
			bt.LabelUpdate();
		}
		foreach (TurretButton bt in TurretGroupGraphs.Keys) {
			bt.LabelUpdate();
		}
	}


    /// <summary> 
	///		Projects a vector in tree dimensions on the screen, in form of an arrow, which points at the
    ///		direction of the projected vector on the camera
	///	</summary>
	///	<param name="vec"> 3-dimensinal Vector </param>
	///	<param name="camera_transform"> The transform of the current camera </param>
	///	<param name="source"> The reference to the image to project </param>
	///	<param name="is_rot"> If the image is rotatet along </param>
	private void ProjectVecOnScreen (Vector3 vec, Transform camera_transform, ref Image source, bool is_rot=true) {
		Vector3 marker = Quaternion.Inverse(camera_transform.rotation) * Vector3.ProjectOnPlane(vec, camera_transform.rotation * Vector3.forward);
		Vector3 fin_marker = marker.normalized;
		if (fin_marker == Vector3.zero) {fin_marker.z = 1f;}
		source.rectTransform.anchoredPosition = new Vector2(fin_marker.x, fin_marker.y) * 80f;
		if (is_rot) {
			float angle = (fin_marker.y > 0f? Mathf.Asin(-fin_marker.x / fin_marker.magnitude):Mathf.PI - Mathf.Asin(-fin_marker.x / fin_marker.magnitude)) * Mathf.Rad2Deg;
			source.transform.rotation = Quaternion.Euler(0f, 0f, angle);
		}
	}

    ///<summary> This should be called from the third frame on </summary>
    private void BattleSymbolsUpdate () {
        // Defines Target
        Target target = player_script.target;
		ReferenceSystem ref_sys = SceneGlobals.ReferenceSystem;
        // Projects the player's "absolute" velocity (relative to the origin of the scene)
        if (PlayerShip != null && ref_sys.RelativeVelocity(PlayerShip.Velocity) != Vector3.zero) {
			Image prog = pointers["prograde_marker"];
            ProjectVecOnScreen(ref_sys.RelativeVelocity(PlayerShip.Velocity), camera_transform, ref prog);
        }

		// Projects the players relative velocity to his selected target
		if (target.Exists) {
			if (!target.virt_ship) {
				if (PlayerShip.Velocity - target.Ship.Velocity != Vector3.zero) {
					Image tgt_vel = pointers["target_velocity_marker"];
					ProjectVecOnScreen(PlayerShip.Velocity - target.Ship.Velocity, camera_transform, ref tgt_vel);
				}
			}
		}

		// Weapon indicators
		foreach (Weapon w in PlayerShip.Parts.GetAll<Weapon>()) {
			Image img = weapon_states[w];
			Vector3 new_pos = MainCam.WorldToScreenPoint(player_transform.position + (player_transform.forward * 100000));
			new_pos.z = 0;
			img.rectTransform.position = new_pos;
			if (w.ooo_time > 0) {
				float ratio = 1 - (w.ooo_time  / w.init_ooo_time) * .75f;
				img.color = new Color(ratio, ratio, ratio);
			} else {
				img.color = new Color(1f, 1 - w.heat, 1 - w.heat);
			}
		}

		// Turret Indicators
		foreach (Turret t in PlayerShip.Parts.GetAll<Turret>()) {
			Image img = turret_states[t];
			Vector3 new_pos = MainCam.WorldToScreenPoint(transform.position + (t.BarrelRot * Vector3.forward * 100000f));
			new_pos.z = 0;
			img.rectTransform.position = new_pos;
			if (t.ooo_time > 0) {
				float ratio = 1 - (t.ooo_time  / t.init_ooo_time) * .75f;
				img.color = new Color(ratio, ratio, ratio);
			} else {
				img.color = new Color(1f, 1 - t.heat, 1 - t.heat);
			}
		}

		// Draws the "direction pointer" (the velocity direction)
		Vector3 direction_projection = direction == Vector3.zero ? new Vector3(-200, -200) : MainCam.WorldToScreenPoint(direction + transform.position);
		direction_projection.z = 0;
		pointers ["direction_pointer"].transform.position = direction_projection;

		Vector3 velocity_dir = PlayerShip.Velocity == Vector3.zero ? new Vector3(-200, -200) : MainCam.WorldToScreenPoint(PlayerShip.Position + PlayerShip.Velocity.normalized * 1e9f);
		velocity_dir.z = 0;
		pointers ["prograde"].transform.position = velocity_dir;
		pointers ["prograde"].color = Vector3.Angle(PlayerShip.Velocity, camera_transform.forward) < 90f ? Color.green : Color.red;


		if (player_script.target != null) {
			if (player_script.target.Exists) {
				tgting_point = player_script.target.Position;
			}
		}

		if (group_follows_cursor) {
			if (current_group != null) {
				current_group.SetTgtDir(CursorWorldDirection);
			}
		}
	}

	private void ShipStatsUpdate () {
		// Writes information in the bottem_left corner
		float relative_vel = SceneGlobals.ReferenceSystem.RelativeVelocity(PlayerShip.Velocity).magnitude;
		texts["velocity_indicator"].text = (Mathf.Round(relative_vel * 100f) / 100f).ToString() + " m/s";
        texts["angular_momentum_indicator"].text = (Mathf.Round(PlayerShip.AngularVelocity.magnitude * Mathf.Deg2Rad * 100f) / 100f).ToString() + " rad/s";

		// Points to one turret
		if (turret_selected) HighlightingPosition = MainCam.WorldToScreenPoint(selected_turret.Position);

		if (HighlightingPosition == Vector3.zero) {
			pointers ["turret_position"].transform.position = new Vector3(-200, -200, 0);
		} else {
			Vector3 screen_pos = HighlightingPosition;
			screen_pos.z = 0f;
			pointers ["turret_position"].transform.position = HighlightingPosition;
			pointers ["turret_position"].transform.Rotate(0, 0, Time.deltaTime * 90);
		}

		// Updates stuff
		SliderUpdate();
		TurretSliderUpdate();

		if (Input.mouseScrollDelta.y > 0 && (selected_turret != null || selected_group != null)) {
			// up
			scroll_bar.value -= .05f;
		}
		if (Input.mouseScrollDelta.y < 0 && (selected_turret != null || selected_group != null)) {
			// down
			scroll_bar.value += .05f;
		}

		mouse_on_menu = !GroupSwitch ? turret_menu.rectTransform.rect.Contains(Input.mousePosition - turret_menu.rectTransform.position):
									    group_menu.rectTransform.rect.Contains(Input.mousePosition - group_menu.rectTransform.position);

		if (!mouse_on_menu && !turret_selected) {
			turret_menu.rectTransform.position = new Vector3(-200, -200, 0);
			selected_turret = null;
		}
		if (!mouse_on_menu && !group_selected) {
			group_menu.rectTransform.position = new Vector3(-100, -200, 0);
			selected_group = null;
		}
	}

	private void Update () {
		AmmoUpdate();
		ShipStatsUpdate();
		if (!SceneGlobals.general.InMap) {
			BattleSymbolsUpdate();
		}
	}
}
