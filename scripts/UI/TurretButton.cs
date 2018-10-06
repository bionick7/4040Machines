using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/*
 * This script goes on the buttons on the left side, representing the turrets or turretgroups of the ship
 */

[RequireComponent(typeof(Button))]
public class TurretButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
	private GUIScript script;
	private AddGroupMenu script1;

	private uint number;
	private Button button;
	private Image img;
	private Turret own_turret;
	private TurretGroup own_group;
	private bool single_turret = false;

	public bool in_add_menu;
	private AudioSource audiosrc;

	/// <summary> The turret corresponding to this button, if the button is referring to a turret </summary>
	public Turret OwnTurret {
		get {
			if (single_turret) {
				if (own_turret == null) {
					throw new System.NullReferenceException("object not set to an instance");
				} else {
					return own_turret;
				}
			} else {
				throw new System.Exception("Button is not referring to a turret");
			}
		}
		set {
			if (single_turret) {
				own_turret = value;
			} else {
				throw new System.Exception("Button is not referring to a turret");
			}
		}
	}

	/// <summary> The turretgroup corresponding to this button, if the button is referring to a turretgroup </summary>
	public TurretGroup OwnTurretGroup {
		get {
			if (!single_turret) {
				if (own_group == null) {
					throw new System.NullReferenceException("object not set to an instance");
				} else {
					return own_group;
				}
			} else {
				throw new System.Exception("Button is not referring to a turretgroup");
			}
		}
		set {
			if (!single_turret) {
				own_group = value;
			} else {
				throw new System.Exception("Button is not referring to a turretgroup");
			}
		}
	}

	private bool on;

	/// <summary> True if the button is currently selected? </summary>
	public bool On {
		get {
			return on;
		}
		set {
			on = value;
			img.color = value ? new Color(.8f, .8f, .8f) : Color.white;
			if (in_add_menu) {
				if (value) {
					script1.selected_turrets.Add(OwnTurret);
				} else {
					script1.selected_turrets.Remove(OwnTurret);
				}
			} else {
				if (single_turret) {
					if (value) {
						script.selected_turrets.Add(OwnTurret);
					} else {
						script.selected_turrets.Remove(OwnTurret);
					}
				} else {
					if (value) {
						script.selected_groups.Add(OwnTurretGroup);
					} else {
						script.selected_groups.Remove(OwnTurretGroup);
					}
				}
			}
		}
	}

	private void Start () {
		audiosrc = GetComponent<AudioSource>();
	}

	/// <summary>
	///		Like an initializer
	/// </summary>
	/// <param name="ui"> GUI_sript, thid belongs to </param>
	/// <param name="num"> ID number of the button </param>
	/// <param name="turret"> The turret, the button is referring to </param>
	public void Initiate (GUIScript ui, uint num, Turret turret) {
		script = ui;
		number = num;
		single_turret = true;
		in_add_menu = false;

		gameObject.name = turret.name;

		img = GetComponent<Image>();
		button = GetComponent<Button>();
		OwnTurret = turret;

		SetUp();
	}

	/// <summary>
	///		Like an initializer
	/// </summary>
	/// <param name="ui"> GUI_sript, thid belongs to </param>
	/// <param name="num"> ID number of the button </param>
	/// <param name="turret"> The turretgroup, the button is referring to </param>
	public void Initiate (GUIScript ui, uint num, TurretGroup group) {
		script = ui;
		number = num;
		single_turret = false;
		in_add_menu = false;

		gameObject.name = group.name;

		img = GetComponent<Image>();
		button = GetComponent<Button>();
		OwnTurretGroup = group;

		SetUp();
	}

	/// <summary>
	///		Like an initializer
	/// </summary>
	/// <param name="ui"> addmenu-script, thid belongs to </param>
	/// <param name="num"> ID number of the button </param>
	/// <param name="turret"> The turret, the button is referring to </param>
	public void Initiate (AddGroupMenu ui, uint num, Turret turret) {
		script1 = ui;
		number = num;
		single_turret = true;
		in_add_menu = true;

		img = GetComponent<Image>();
		button = GetComponent<Button>();
		OwnTurret = turret;

		SetUp();
	}

	/// <summary> Updates ammunition text </summary>
	public void AmmoUpdate () {
		uint ammo = single_turret ? OwnTurret.ammo_count : OwnTurretGroup.Ammunition;
		transform.GetChild(1).GetComponent<Text>().text = ammo.ToString();
	}

	///<summary> Updates the name label </summary>
	public void LabelUpdate () {
		transform.GetChild(0).GetComponent<Text>().text = name;
	}

	/// <summary> Sets up everything (nearly) non dependable on initialized values </summary>
	void SetUp () {
		string name = single_turret? OwnTurret.name: OwnTurretGroup.name;
		gameObject.name = name;
		transform.GetChild(0).GetComponent<Text>().text = name;

		AmmoUpdate();
	}

	/// <summary> Is called, when the cursor enters the button </summary>
	public void OnPointerEnter(PointerEventData data) {
		if (!in_add_menu) {
			if (single_turret) {
				script.TurretButtonHover(number, true);
			} else {
				script.GroupButtonHover(number, true);
			}
		}
	}

	/// <summary> Is called, when the cursor exits the button </summary>
	public void OnPointerExit(PointerEventData data) {
		if (!in_add_menu) {
			if (single_turret) {
				script.TurretButtonHover(number, false);
			} else {
				script.GroupButtonHover(number, false);
			}
		}
	}

	/// <summary> Is called, when clicked </summary>
	public void OnPointerClick(PointerEventData data) {
		if (single_turret) {
			On = !On;
			audiosrc.Play();
		}
	}
}
