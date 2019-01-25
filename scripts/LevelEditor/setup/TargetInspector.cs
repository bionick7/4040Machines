using UnityEngine;
using UnityEngine.UI;

/* =======================================================
 * The Behaviour of the inspector examining a whole target
 * ======================================================= */

public class TargetInspector : MonoBehaviour
{
	public InputField mass_inp;
	public InputField health_inp;
	public InputField name_inp;
	public Toggle friendly;

	private RectTransform rect_trans;

	public static TargetInspector active;
	
	/// <summary> The target currently selected </summary>
	private EDTarget CurrentTarget {
		get {
			if (CurrentTargetIndex >= 0) {
				return EditorGeneral.target_list [CurrentTargetIndex];
			}
			return EDTarget.none;
		}
		set {
			if (CurrentTargetIndex >= 0) {
				EditorGeneral.target_list [CurrentTargetIndex] = value;
				EditorGeneral.current_movable.correspondence = value;
			}
		}
	}
		
	/// <summary> The index of the currently selected target </summary>
	private int CurrentTargetIndex {
		get {
			if (EditorGeneral.current_movable == null) return -1;
			if (EditorGeneral.current_movable.correspondence is EDTarget)
				return EditorGeneral.target_list.IndexOf((EDTarget) EditorGeneral.current_movable.correspondence);
			return -1;
		}
	}
	

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

	private void Start () {
		rect_trans = GetComponent<RectTransform>();
		active = this;
		Shown = false;
	}

	/// <summary> Reloads the labels of the UI </summary>
	private void Reload () {
		var current = CurrentTarget;
		mass_inp.text = current.mass.ToString();
		health_inp.text = current.hp.ToString();
		name_inp.text = current.name;
		friendly.isOn = current.friendly;
	}

	/// <summary> Should be called, if the mass is changed in the UI </summary>
	public void MassChanged () {
		double mass = 0;
		System.Double.TryParse(mass_inp.text, out mass);
		var current = CurrentTarget;
		if (current.Exists) {
			current.mass = mass;
			CurrentTarget = current;
		}
	}

	/// <summary> Should be called, if the hps are changed in the UI </summary>
	public void HPChanged () {
		float health = 0;
		System.Single.TryParse(health_inp.text, out health);
		var current = CurrentTarget;
		if (current.Exists) {
			current.hp = health;
			CurrentTarget = current;
		}
	}

	/// <summary> Should be called, if the name is changed in the UI </summary>
	public void NameChanged () {
		var current = CurrentTarget;
		if (current.Exists) {
			current.name = name_inp.text;
			CurrentTarget = current;
		}
	}

	/// <summary> Should be called, if the "Friendly" toggle is triggert </summary>
	public void Toggle () {
		var current = CurrentTarget;
		if (current.Exists) {
			current.friendly = friendly.isOn;
			CurrentTarget = current;
		}
	}
}
