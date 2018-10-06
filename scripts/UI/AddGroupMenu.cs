using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class AddGroupMenu : MonoBehaviour {

	private Dictionary<Button, Turret> turret_graphs= new Dictionary<Button, Turret>();

	private RectTransform rect;
	private GUIScript gui_script;
	private ShipControl player_script;

	private GameObject source_button;
	private Transform mask_transform;
	private Slider slider;
	private Dropdown tgt_choice;
	private Dropdown ammo_choice;
	private InputField name_input;

	private float slider_ratio;

	private List<Vector3> init_positions = new List<Vector3>();

	public TurretGroup group;
	public List<Turret> preselected_turrets;
	public List<Turret> selected_turrets;
	public TurretGroup composed_group;

	public bool Edit { get; set; }
	public int TGTPointing { get; set; }
	public string CurrGroupName;

	private bool menu_enabled;
	/// <summary>
	///		True, if the menu is shown.
	///		This will freze the game.
	/// </summary>
	public bool Enabled {
		get { return menu_enabled; }
		set {
			if (value) {
				Init();
			} else {
				Exit();
			}
			menu_enabled = value;
		}
	}

	private Vector2Int Middle {
		get {
			return new Vector2Int( Screen.width / 2, Screen.height / 2 );
		}
	}

	void Start () {
		rect = GetComponent<RectTransform>();
		GameObject player_obj = SceneObject.PlayerObj();
		gui_script = SceneData.ui_script;
		player_script = GameObject.FindGameObjectWithTag("Player").GetComponent<ShipControl>();
		source_button = GameObject.Find("turr_slide");
		mask_transform = GetComponentInChildren<RectMask2D>().transform;
		slider = GetComponentInChildren<Slider>();
		tgt_choice = GameObject.Find("TG_selecter").GetComponent<Dropdown>();
		ammo_choice = GameObject.Find("Ammo_selecter").GetComponent<Dropdown>();
		name_input = GetComponentInChildren<InputField>();
	}

	public void UpdatePositions () {
		float pos_multiplyer = 300 / slider_ratio - 300;

		List<Button> butt_list = new List<Button> (turret_graphs.Keys);
		for (int i=0; i < butt_list.Count; i++) {
			Button button = butt_list[i];
			Vector3 init_pos = init_positions[i];
			Vector3 plus_pos = new Vector3(0, slider.value * pos_multiplyer);
			button.transform.position = init_pos + plus_pos;
		}
	}

	public void Finish () {
		if (Edit) {
			for (int i=0; i < group.Count; i++) {
				group.TurretList[i].Group = TurretGroup.Trashbin;
			}
			foreach (Turret turr in selected_turrets) {
				turr.Group = group;
			}
			group.name = name_input.text;
		} else {
			group = new TurretGroup(Target.None, selected_turrets.ToArray(), name_input.text) {
				parentship = gui_script.player_ship
			};
			gui_script.AddTurretGroup(group);
		}
		Enabled = false;
	}

	void Init () {
		rect.position = new Vector3(Middle.x, Middle.y);

		selected_turrets = new List<Turret>();
		List<Turret> all_turrets = new List<Turret>();
		foreach (Turret [] turrs in player_script.turrets.Values) {
			all_turrets.AddRange(turrs);
		}
		for (int i=0; i < all_turrets.Count; i++) {
			Turret turret = all_turrets[i];
			Vector3 pos = new Vector3( Middle.x + 70f , Middle.y + 150f - (50 * i));
			GameObject button_object = Instantiate(source_button);
			button_object.transform.position = pos;
			button_object.transform.SetParent(mask_transform, true);

			TurretButton tb = button_object.GetComponent<TurretButton>();
			tb.Initiate(this, (uint) i, turret);

			if (Edit) {
				if (group.Contains(turret)) {
					tb.On = true;
				}
			} else {
				if (preselected_turrets.Contains(turret)) {
					tb.On = true;
				}
			}

			Button butt = button_object.GetComponent<Button>();
			turret_graphs.Add(butt, turret);
			init_positions.Add(button_object.transform.position);
		}
		slider_ratio = Mathf.Min(1, 300f / (50f * (float) all_turrets.Count));
		slider.transform.SetAsFirstSibling();

		if (Edit) {
			if (group.follow_target) {
				tgt_choice.value = (int) TargetChoice.target;
			} else {
				tgt_choice.value = (int) TargetChoice.cursor;
			}
			name_input.text = group.name;
		}

		gui_script.Paused = true;
	}

	void Exit () {
		turret_graphs = new Dictionary<Button, Turret>();
		init_positions = new List<Vector3>();

		rect.position = new Vector3(200, -200);
		gui_script.Paused = false;
		gui_script.Click();
	}

	void Update () {
		if (Enabled) {
			UpdatePositions();
		}
	}

	// Button functions

	public void ToGroup () {
		Edit = false;
		preselected_turrets = gui_script.selected_turrets;
		Enabled = true;
	}

	public void TgtPointing () {
		TGTPointing = GameObject.Find("TG_selecter").GetComponent<Dropdown>().value;
	}
}