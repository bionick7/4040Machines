using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/*
 * This script goes on the buttons on the left side, representing the turrets or turretgroups of the ship
 */

[RequireComponent(typeof(Image))]
public class TurretButton : MonoBehaviour, 
IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler ,IPointerUpHandler
{
	private GUIScript script;
	private AddGroupMenu script1;

	private uint number;
	private Image img;
	private RectTransform own_rect_transform;
	private RectTransform reload_indicator;

	private bool hover_ = false;
	public bool Hover {
		get {
			return hover_;
		}
		set {
			if (!in_add_menu) {
				if (single_turret) {
					script.TurretButtonHover(number, value);
				} else {
					script.GroupButtonHover(number, value);
				}
			}
			hover_ = value;
		}
	}

	private Turret own_turret;
	private TurretGroup own_group;
	private bool single_turret = false;
	private string Name {
		get {
			if (single_turret) return own_turret.name;
			return own_group.name;
		}
	}

	public bool in_add_menu;

	private Color Idle {
		get { return on ? new Color(.8f, .8f, .8f) : Color.white; }
	}	
	private readonly Color hover = new Color(.8f, .8f, .8f);
	private readonly Color clicked = new Color(.81f, .68f, .68f);

	private bool dragging = false;

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
		get { return on; }
		set {
			on = value;
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

	private void Start () {	}

	private void Update () {
		if (reload_indicator != null) {
			if (single_turret) {
				reload_indicator.sizeDelta = new Vector2((1 - Mathf.Min(1, own_turret.delta_time / own_turret.reload_speed)) * 100, 1);
			} else {
				reload_indicator.sizeDelta = Vector2.zero;
			}
		}

		if (dragging) {
			if (Input.GetMouseButtonUp(0)) {
				PinLabel.Dragging = dragging = false;
				PinLabel.Active.Context = PinLabel.PinContext.none;
				if (PinLabel.Active.Object == null) {

				} else {
					if (single_turret) {

					} else {
						OwnTurretGroup.target = PinLabel.Active.Object;
					}
				}
			}
		}
		if (Hover) {
			if (!own_rect_transform.rect.Contains(Input.mousePosition - own_rect_transform.position)) {
				Hover = false;
			}
		}
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
		own_rect_transform = GetComponent<RectTransform>();
		reload_indicator = transform.GetChild(2).GetComponent<RectTransform>();
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
		own_rect_transform = GetComponent<RectTransform>();
		reload_indicator = transform.GetChild(2).GetComponent<RectTransform>();
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
		own_rect_transform = GetComponent<RectTransform>();
		reload_indicator = transform.GetChild(2).GetComponent<RectTransform>();
		OwnTurret = turret;

		SetUp();
	}

	/// <summary> Updates ammunition text </summary>
	public void AmmoUpdate () {
		uint ammo = single_turret ? OwnTurret.Ammunition : OwnTurretGroup.Ammunition;
		transform.GetChild(1).GetComponent<Text>().text = ammo.ToString();
	}

	///<summary> Updates the name label </summary>
	public void LabelUpdate () {
		transform.GetChild(0).GetComponent<Text>().text = Name;
	}

	/// <summary> Sets up everything (nearly) non dependable on initialized values </summary>
	void SetUp () {
		gameObject.name = Name;
		transform.GetChild(0).GetComponent<Text>().text = Name;

		AmmoUpdate();
	}

	/// <summary> Is called, when the cursor enters the button </summary>
	public void OnPointerEnter(PointerEventData data) {
		Hover = true;
		img.color = hover;
	}

	/// <summary> Is called, when the cursor exits the button </summary>
	public void OnPointerExit(PointerEventData data) {
		Hover = false;
		img.color = Idle;
	}

	/// <summary> Is called, when clicked </summary>
	public void OnPointerUp(PointerEventData data) {
		if (single_turret & Hover) {
			On = !On;
			Globals.audio.UIPlay(UISound.dump_click);
			img.color = clicked;
		}
	}

	/// <summary> Is called, when clicked </summary>
	public void OnPointerDown(PointerEventData data) {
		if (data.button == PointerEventData.InputButton.Left) {
			PinLabel.Active.Context = PinLabel.PinContext.turret;
			PinLabel.Dragging = dragging = true;
		}
		img.color = hover;
	}
}
