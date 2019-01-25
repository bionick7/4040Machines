using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FileManagement;

/* =========================================
 * Node in the story editor, that represents 
 * the "spaw" action
 * ========================================= */

public class SpawnNode : MonoBehaviour, ICommandBehaviour {

	public Dropdown type_choser;
	public RectTransform child_template;
	public RectTransform content_area;

	/// <summary> A list of all the spawning instances of the command </summary>
	public List<SpawnField> containing = new List<SpawnField>();

	/// <summary> Evcerything, that possibily could be spawned </summary>
	private SpawnInst[] spawn_arr;
	private SpawnInst curr_inst;
	private RectTransform own_transform;
	private float d_height;

	/// <summary> The storystage, this belongs to </summary>
	public StoryStage Parent { get; set; }
	public byte Num { get; set; }
	public GameObject Obj {
		get { return gameObject; }
	}
	public Commands CType {
		get { return Commands.spawn; }
	}

	private bool initialized = false;

	/// <summary> Gets called either manually, or at the first frame </summary>
	public void Start_ () {
		spawn_arr = new SpawnInst [EditorGeneral.squadron_list.Count + EditorGeneral.target_list.Count];
		SpawnInst[] ship_arr = System.Array.ConvertAll(EditorGeneral.squadron_list.ToArray(), x => new SpawnInst(x.name, "squadron"));
		SpawnInst[] target_arr = System.Array.ConvertAll(EditorGeneral.target_list.ToArray(), x => new SpawnInst(x.name, "target"));
		ship_arr.CopyTo(spawn_arr, 0);
		target_arr.CopyTo(spawn_arr, ship_arr.Length);

		type_choser.options = new List<Dropdown.OptionData>(
			System.Array.ConvertAll(spawn_arr, x => new Dropdown.OptionData(x.name))
		);

		own_transform = GetComponent<RectTransform>();
		d_height = child_template.rect.height;
		initialized = true;
	}

	private void Start () {
		if (!initialized) Start_();
	}

	private void Update () {
		if (spawn_arr.Length != EditorGeneral.squadron_list.Count + EditorGeneral.target_list.Count) {
			spawn_arr = new SpawnInst [EditorGeneral.squadron_list.Count + EditorGeneral.target_list.Count];
			SpawnInst[] ship_arr = System.Array.ConvertAll(EditorGeneral.squadron_list.ToArray(), x => new SpawnInst(x.name, "squadron"));
			SpawnInst[] target_arr = System.Array.ConvertAll(EditorGeneral.target_list.ToArray(), x => new SpawnInst(x.name, "target"));
			ship_arr.CopyTo(spawn_arr, 0);
			target_arr.CopyTo(spawn_arr, ship_arr.Length);

			type_choser.options = new List<Dropdown.OptionData>(
				System.Array.ConvertAll(spawn_arr, x => new Dropdown.OptionData(x.name))
			);
		}
	}

	/// <summary> Should be called, when the dropdown selector is changed </summary>
	public void UpdateInp () {
		curr_inst = spawn_arr [type_choser.value];
	}

	/// <summary> Should be called, if the add button is pressed </summary>
	public void Add () {
		Add(curr_inst);
	}

	/// <summary> Adds an order to the list of spawning things </summary>
	/// <param name="inst"> The instance to spawn </param>
	public void Add (SpawnInst inst) {
		float size_d = child_template.rect.height;

		var rectT = Instantiate(child_template.gameObject).GetComponent<RectTransform>();

		rectT.SetParent(transform);

		Parent.total.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Parent.total.rect.height + d_height);
		own_transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, own_transform.rect.height + d_height);
		Parent.total.position -= new Vector3(0, d_height / 2);
		own_transform.position -= new Vector3(0, d_height / 2);
		if (Parent.after != null)
			Parent.after.Translate(new Vector3(0, -d_height));

		rectT.transform.position = own_transform.position + new Vector3(20, own_transform.rect.yMin + d_height - 10);

		rectT.GetComponentInChildren<Button>().onClick.AddListener(Remove);
		rectT.GetComponentInChildren<Text>().text = inst.name;

		for (int i=Parent.commands.IndexOf(this) + 1; i < Parent.commands.Count; i++) {
			Parent.commands [i].Obj.transform.Translate(0, -d_height, 0);
		}

		containing.Add(new SpawnField() { inst = inst, transform = rectT });
	}

	/// <summary> Removes an order from the list </summary>
	public void Remove () {
		Vector3 mousepos = Input.mousePosition;
		int deletenum = (int)(((own_transform.position - mousepos).y + own_transform.rect.yMax) / child_template.rect.height) - 2;
		Destroy(containing [deletenum].transform.gameObject);
		containing.RemoveAt(deletenum);
		for(int i=deletenum; i < containing.Count; i++) {
			containing [i].transform.position += new Vector3(0, d_height);
		}

		Parent.total.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Parent.total.rect.height - d_height);
		own_transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, own_transform.rect.height - d_height);
		Parent.total.position += new Vector3(0, d_height / 2);
		own_transform.position += new Vector3(0, d_height / 2);
		if (Parent.after != null)
			Parent.after.Translate(new Vector3(0, d_height));

		for (int i=Parent.commands.IndexOf(this) + 1; i < Parent.commands.Count; i++) {
			Parent.commands [i].Obj.transform.Translate(0, d_height, 0);
		}
	}

	/// <summary> Gets the datastructure for this spawn window </summary>
	/// <param name="parentds"> The planned parent of the datastructure </param>
	/// <returns> The datastructure </returns>
	public DataStructure GetDS (DataStructure parentds) {
		DataStructure res = new DataStructure("spawn", parentds);
		res.Set("types", System.Array.ConvertAll(containing.ToArray(), x => x.inst.type));
		res.Set("names", System.Array.ConvertAll(containing.ToArray(), x => x.inst.name));
		return res;
	} 

	/// <summary> Should be called, if the exit button is pressed </summary>
	public void Exit () {
		Parent.Shorten(this);
		Destroy(gameObject);
	}
}

/// <summary>
///		Represents a "possibility" for spawning
/// </summary>
public struct SpawnInst
{
	public string name;
	public string type;

	public SpawnInst(string pname, string ptype) {
		name = pname;
		type = ptype;
	}
}

/// <summary>
///		Represents one spawning "command"
/// </summary>
public struct SpawnField
{
	public SpawnInst inst;
	public RectTransform transform;
}