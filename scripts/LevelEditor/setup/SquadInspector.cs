using UnityEngine;
using UnityEngine.UI;

/* ======================================================
 * The Behaviour of the inspector examining a whole squad
 * ====================================================== */

public class SquadInspector : MonoBehaviour
{
	public InputField name_inp;
	public Toggle friendly;

	private RectTransform rect_trans;

	public static SquadInspector active;

	private bool _shown;
	/// <summary> If inspector is shown at the moment </summary>
	public bool Shown {
		get { return _shown; }
		set {
			rect_trans.anchoredPosition = value ? new Vector3(-100, rect_trans.anchoredPosition.y) : new Vector3(100, rect_trans.anchoredPosition.y);
			if (value) Reload();
			_shown = value;
		}
	}

	/// <summary> The squad currently selected </summary>
	private Squadron CurrentSquad {
		get {
			if (CurrentSquadIndex > EditorGeneral.squadron_list.Count) {
				EditorGeneral.Throw("There is no such index: " + CurrentSquadIndex.ToString());
			}
			return EditorGeneral.squadron_list [CurrentSquadIndex];
		}
		set { EditorGeneral.squadron_list [CurrentSquadIndex] = value; }
	}

	/// <summary> The index of the currently selected squad </summary>
	public int CurrentSquadIndex { get; set; }

	private void Start () {
		rect_trans = GetComponent<RectTransform>();
		active = this;
		Shown = false;
		CurrentSquadIndex = 0;
	}

	/// <summary> Reload Input labels </summary>
	private void Reload () {
		name_inp.text = CurrentSquad.name;
		friendly.isOn = CurrentSquad.friendly;
	}

	/// <summary> Should be called, when the name is changed </summary>
	public void NameChange () {
		var current = CurrentSquad;
		current.name = name_inp.text;
		CurrentSquad = current;
		ShipInspector.active.ReloadSquads();
	}

	/// <summary> Should be called, when the "friendly" toggle is triggert </summary>
	public void Toggle () {
		var current = CurrentSquad;
		current.friendly = friendly.isOn;
		CurrentSquad = current;
		ShipInspector.active.ReloadSquads();
	}

	/// <summary> Should be called, if the "Delete" button is pressed </summary>
	public void Delete () {
		var current = CurrentSquad;
		EditorGeneral.squadron_list.Remove(current);
		if (current.leader != null)
			current.leader.Squad = Squadron.default_squadron;
		EDShip[] ship_li_copy  = current.ships.ToArray();
		foreach (EDShip ship in ship_li_copy) {
			ship.Squad = Squadron.default_squadron;
		}
		ShipInspector.active.ReloadSquads();
	}

	/// <summary> Should be called, when the "Ship Inspector" button is pressed </summary>
	public void ChangeInspector () {
		EditorGeneral.InspectorType = InspectorType.ship;
	}
}
