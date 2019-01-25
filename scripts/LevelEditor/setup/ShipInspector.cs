using UnityEngine;
using UnityEngine.UI;

/* ======================================================
 * The Behaviour of the inspector examining a single ship
 * ====================================================== */

public class ShipInspector : MonoBehaviour
{
	public Dropdown squadselect;
	public Toggle leader;
	public Toggle player;
	public Sprite img_template;

	private RectTransform rect_trans;

	public static ShipInspector active;

	private bool _shown;
	/// <summary> If inspector is shown at the moment </summary>
	public bool Shown {
		get { return _shown; }
		set {
			rect_trans.anchoredPosition = value ? new Vector3(-100, rect_trans.anchoredPosition.y) : new Vector3(100, rect_trans.anchoredPosition.y);
			_shown = value;
			Reload();
		}
	}

	private EDShip _current_ship;
	/// <summary> The ship currently selected </summary>
	private EDShip CurrentShip {
		get { return _current_ship; }
		set {
			_current_squad.ships [Index] = value;
		}
	}

	/// <summary> The squadron, to which the currently selected ship is belonging </summary>
	private Squadron _current_squad;

	/// <summary> The index of the currently selected ship </summary>
	private int Index {
		get { return _current_squad.ships.IndexOf(_current_ship); }
	}


	private void Start () {
		rect_trans = GetComponent<RectTransform>();
		active = this;
		Shown = true;
		ReloadSquads();
	}

	/// <summary> Reloads the squads for the dropdown </summary>
	public void ReloadSquads() {
		squadselect.options.Clear();
		foreach (Squadron squad in EditorGeneral.squadron_list) {
			squadselect.options.Add(new Dropdown.OptionData(squad.name));
		}
		Reload();
	}

	/// <summary> Reloads the labels of the UI </summary>
	private void Reload () {
		if (EditorGeneral.current_movable == null || !(EditorGeneral.current_movable.correspondence is EDShip)) return;
		_current_ship = EditorGeneral.current_movable.correspondence as EDShip;
		_current_squad = _current_ship.Squad;

		squadselect.value = EditorGeneral.squadron_list.IndexOf(_current_squad);
		leader.isOn = _current_ship.IsLeader;
		player.isOn = _current_ship.IsPlayer;
	}
	
	/// <summary> Should be called, when the squad-dropdown is changed </summary>
	public void SquadChanged () {
		var current = _current_ship;
		_current_squad = EditorGeneral.squadron_list [squadselect.value];
		current.Squad = _current_squad;
		_current_ship = current;
	}

	/// <summary> Should be called, when the "Leader" toggle is triggert </summary>
	public void Toggle () {
		if (!_current_ship.IsLeader && leader.isOn)
			_current_squad.ChangeLeader(_current_ship);
	}

	/// <summary> Should be called, when the "Player" toggle is triggert </summary>
	public void TogglePlayer () {
		_current_ship.IsPlayer = player.isOn;
	}

	/// <summary> Should be called, when the "Squad Inspector" button is pressed </summary>
	public void SwitchInspector () {
		SquadInspector.active.CurrentSquadIndex = EditorGeneral.squadron_list.IndexOf(_current_squad);
		EditorGeneral.InspectorType = InspectorType.squad;
	}
}
