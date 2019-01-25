using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FileManagement;

/* =========================================
 * Node in the story editor, that represents 
 * the add "objective" action
 * ========================================= */

public class ObjectiveNode : MonoBehaviour, ICommandBehaviour {

	public Dropdown type_selector;
	public Dropdown name_selector;

	public StoryStage Parent { get; set; }
	public byte Num { get; set; }
	public GameObject Obj {
		get { return gameObject; }
	}
	public Commands CType {
		get { return Commands.spawn; }
	}

	private RectTransform own_transform;
	private string[] instance_arr;

	/// <summary> Constant array with all possible objective types </summary>
	private readonly string[] type_arr = new string[] {
		"kill squadron",
		"escort squadron",
		"kill target",
		"escort target"
	};
	/// <summary> selected target name </summary>
	public string current_instance = "default";
	/// <summary> selected objective type </summary>
	public string current_type = "kill squadron";

	private bool instantiated = false;

	private void Start () {
		if (!instantiated) Start_();
	}

	/// <summary> Start is either called manually, or at the beginning of the first frame </summary>
	public void Start_ () {
		own_transform = GetComponent<RectTransform>();
		type_selector.options = new List<Dropdown.OptionData>(
			System.Array.ConvertAll(type_arr, x => new Dropdown.OptionData(x))
		);
		UpdateLabel();

		name_selector.value = 0;
		for (int i=0; i < instance_arr.Length; i++) {
			if (instance_arr [i] == current_instance) name_selector.value = i;
		}

		type_selector.value = 0;
		for (int i=0; i < type_arr.Length; i++) {
			if (type_arr [i] == current_type) type_selector.value = i;
		}

		instantiated = false;
	}

	/// <summary> Should be called, if one of the dropdowns changes </summary>
	public void UpdateInp () {
		current_instance = instance_arr [name_selector.value];
		switch (type_selector.value) {
		default:
		case 0:
			current_type = "kill squadron";
			break;
		case 1:
			current_type = "escort squadron";
			break;
		case 2:
			current_type = "kill target";
			break;
		case 3:
			current_type = "escort target";
			break;
		}
	}

	/// <summary> Updates the labels </summary>
	private void UpdateLabel () {
		instance_arr = new string [EditorGeneral.squadron_list.Count + EditorGeneral.target_list.Count];
		string[] ship_arr = System.Array.ConvertAll(EditorGeneral.squadron_list.ToArray(), x => x.name);
		string[] target_arr = System.Array.ConvertAll(EditorGeneral.target_list.ToArray(), x => x.name);
		ship_arr.CopyTo(instance_arr, 0);
		target_arr.CopyTo(instance_arr, ship_arr.Length);

		name_selector.options = new List<Dropdown.OptionData>(
			System.Array.ConvertAll(instance_arr, x => new Dropdown.OptionData(x))
		);
	}

	private void Update () {
		if (instance_arr.Length != EditorGeneral.squadron_list.Count + EditorGeneral.target_list.Count) {
			UpdateLabel();
		}
	}

	/// <summary> Returns the datastrcture associated with the command </summary>
	/// <param name="parentds"> The planned parent datastructure </param>
	/// <returns> A DataStructure object </returns>
	public DataStructure GetDS (DataStructure parentds) {
		DataStructure res = new DataStructure("objective", parentds);
		res.Set("objective type", current_type);
		res.Set("target name", current_instance);
		return res;
	}

	/// <summary> Should be called, if the exit button is pressed </summary>
	public void Exit () {
		Parent.Shorten(this);
		Destroy(gameObject);
	}
}
