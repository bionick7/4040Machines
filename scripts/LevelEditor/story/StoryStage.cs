using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FileManagement;

/* ==================================
 * The graphical element to represent
 * stages in the story for the editor
 * ================================== */

/// <summary>
///		Graphival representation of story stages
/// </summary>
public class StoryStage : MonoBehaviour, IPointerDownHandler {

	/// <summary> The attachment node above the stage </summary>
	public RectTransform uppernode;
	/// <summary> The attachment node beneath the stage </summary>
	public RectTransform lowernode;
	/// <summary> Containing everything inside </summary>
	public RectTransform content;
	/// <summary> Toggle, if the story beginns with this </summary>
	public Toggle firsttoggle;
	/// <summary> The selection of commands </summary>
	public Dropdown commandchoice;

	public GameObject spawn_template;
	public GameObject conversation_template;
	public GameObject objective_template;

	/// <summary> All stages are in here </summary>
	public static HashSet<StoryStage> totalstages = new HashSet<StoryStage>();

	private bool dragging;
	private Vector3 bef_pos;

	/// <summary> The storystage, that comes before this one (null if there is none) </summary>
	public StoryStage before;
	/// <summary> The storystage, that comes after this one (null if there is none) </summary>
	public StoryStage after;
	public Commands current_command;
	public RectTransform total;

	public List<ICommandBehaviour> commands = new List<ICommandBehaviour>();

	public ushort id;

	/// <summary> The stage, with whom the story beginns </summary>
	public static StoryStage FirstStage { get; private set; }

	/// <summary> returns true, if this is the friststage (see above) </summary>
	public bool IsFirst {
		get {
			return this == FirstStage;
		}
		set {
			if (value) FirstStage = this;
			else {
				if (IsFirst)
					FirstStage = null;
			}
			foreach (var stg in totalstages) stg.UpdateUI();
		}
	}

	private void Start () {
		dragging = false;
		bef_pos = Vector3.zero;

		id = (ushort) totalstages.Count;
		totalstages.Add(this);

		before = null;
		after = null;
		total = GetComponent<RectTransform>();

		UpdateUI();
		ChildrenUodate();
	}

	/// <summary> Checks, if any new children should be added </summary>
	private void ChildrenUodate () {
		foreach (StoryStage stage in totalstages) {
			Vector3 upper_d_loc = stage.lowernode.position - uppernode.position;
			if ((upper_d_loc).sqrMagnitude <= 100) {
				Translate(upper_d_loc);
				before = stage;
				transform.SetSiblingIndex(before.transform.GetSiblingIndex() - 1);
				stage.after = this;
			}
			Vector3 lower_d_loc = stage.uppernode.position - lowernode.position;
			if ((lower_d_loc).sqrMagnitude <= 100) {
				Translate(lower_d_loc);
				after = stage;
				transform.SetSiblingIndex(after.transform.GetSiblingIndex() + 1);
				stage.before = this;
			}
		}

		if (after != null && (after.uppernode.position - lowernode.position).sqrMagnitude > 1000) {
			after.before = null;
			after = null;
		}
		if (before != null && (before.lowernode.position - uppernode.position).sqrMagnitude > 1000) {
			before.after = null;
			before = null;
		}
	}

	private void Update () {
		if (dragging) {
			Vector3 d_position = Input.mousePosition - bef_pos;
			Translate(d_position);
			bef_pos = Input.mousePosition;
			if (Input.GetMouseButtonUp(0)) {
				dragging = false;
			}
			ChildrenUodate();
		}
	}

	/// <summary> Checks, if the whole thing is still on the screen after a certain translation</summary>
	/// <param name="vec"> The translation to perform </param>
	/// <returns> True, if everything is still on the scree; false if not </returns>
	private bool InScreen (Vector3 vec) {
		float w = Screen.width - 400;
		float h = Screen.height;
		return new Rect(-w/2, -h/2, w, h).Contains(transform.position + vec);
	}

	#region recursives

	/// <summary> Returns the number of storystages connected after this one </summary>
	/// <remarks> Recursive </remarks>
	public ushort CountChildren () {
		if (after == null) return 0;
		return (ushort) (after.CountChildren() + 1);
	}

	/// <summary> A list with all the storystages connected after this one </summary>
	/// <remarks> Recursive </remarks>
	public List<StoryStage> GetChildren () {
		if (after == null) return new List<StoryStage>() { this };
		var li = after.GetChildren();
		li.Add(this);
		return li;
	}

	/// <summary> Translates node with all his children, if possible </summary>
	/// <param name="vec"> The translation vector </param>
	/// <returns> If translation was sucessfull </returns>
	/// <remarks> Recursive </remarks>
	public bool Translate(Vector3 vec, bool ignoreboundaries=false) {
		if (!InScreen(vec) & ignoreboundaries) return false;
		if (after != null) {
			if (after.Translate(vec)) {
				transform.Translate(vec);
				return true;
			}
			return false;
		} else {
			transform.Translate(vec);
			return true;
		}
	}

	#endregion

	/// <summary> Updates all the User interface elements </summary>
	public void UpdateUI () {
		firsttoggle.isOn = IsFirst;
		commandchoice.options = new List<Dropdown.OptionData>(System.Array.ConvertAll(System.Enum.GetNames(typeof(Commands)), x => new Dropdown.OptionData(x)));
	}

	public void CommandChoiceUpdate () {

	}

	/// <summary> Spawns a "spawn" node with given parameters </summary>
	/// <param name="types"> array with the types of the spawn commands </param>
	/// <param name="names"> array with the names of the spawn commands </param>
	public void Spawn_SpawnNode (string[] types, string[] names) {
		SpawnNode node = Instantiate(spawn_template).GetComponent<SpawnNode>();
		node.Start_();
		Add(node);
		for (int i=0; i < types.Length; i++) {
			node.Add(new SpawnInst(names[i], types[i]));
		}
	}

	/// <summary> Spawns a "conversation" node with given parameters </summary>
	/// <param name="id"> The identifying number of the conversation </param>
	public void Spawn_ConversationNode (ushort id) {
		ConversationNode node = Instantiate(conversation_template).GetComponent<ConversationNode>();
		node.Start_();
		Add(node);
	}

	/// <summary> Spawns a "objective" node with given parameters </summary>
	/// <param name="types"> type of the objective </param>
	/// <param name="names"> name of the objective's target </param>
	public void Spawn_ObjectiveNode (string type, string name) {
		ObjectiveNode node = Instantiate(objective_template).GetComponent<ObjectiveNode>();
		node.current_type = type;
		node.current_instance = name;
		Add(node);
	}

	/// <summary> Should be called, if the add button is pressed </summary>
	public void SpawnCommand () {
		current_command = (Commands) commandchoice.value;
		switch (current_command) {
		case Commands.spawn:
			Spawn_SpawnNode(new string [0], new string [0]);
			break;
		case Commands.conversation:
			Spawn_ConversationNode(0);
			break;
		default:
		case Commands.objective:
			Spawn_ObjectiveNode(string.Empty, string.Empty);
			break;
		}
	}

	/// <summary> Adds a command </summary>
	/// <param name="beh"> The command in question </param>
	private void Add (ICommandBehaviour beh) {
		var rectT = beh.Obj.GetComponent<RectTransform>();
		rectT.SetParent(transform);

		float d_size = rectT.rect.height;
		total.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, total.rect.height + d_size);
		total.position -= new Vector3(0, d_size / 2);
		if (after != null)
			after.Translate(new Vector3(0, -d_size));

		beh.Obj.transform.position = content.position + new Vector3(0, content.rect.yMin + d_size / 2);

		beh.Parent = this;
		commands.Add(beh);
	}
	
	/// <summary> Removes a command </summary>
	/// <param name="beh"> The command in question </param>
	public void Shorten (ICommandBehaviour beh) {
		var rectT = beh.Obj.GetComponent<RectTransform>();
		float d_size = rectT.rect.height;

		total.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, total.rect.height - d_size);
		total.position += new Vector3(0, d_size / 2);
		if (after != null)
			after.Translate(new Vector3(0, d_size));
		int beh_indx = commands.IndexOf(beh);
		commands.Remove(beh);

		for (int i=beh_indx; i < commands.Count; i++) {
			commands [i].Obj.transform.position += new Vector3(0, d_size);
		}
	}

	/// <summary> Should be called, if the first togge is toggled </summary>
	public void ToggleFirst () {
		IsFirst = firsttoggle.isOn;
	}

	/// <summary> Should be called, if the exit button is pressed </summary>
	public void Exit () {
		if (before != null & after != null) {
			before.after = after;
			after.before = before;
			after.Translate(before.lowernode.position - after.uppernode.position);
		} else {
			if (before != null) before.after = null;
			if (after != null) after.before = null;
		}
		totalstages.Remove(this);
		Destroy(gameObject);
	}

	/// <summary> Sets the node before this one </summary>
	/// <param name="parent"></param>
	public void SetParent(StoryStage parent) {
		Vector3 dist = parent.lowernode.position - uppernode.position;
		Translate(dist);
		before = parent;
	}

	/// <summary> Get the story datastructure from this one on </summary>
	/// <param name="parentds"> The planned parent </param>
	/// <returns> DataStructure object </returns>
	/// <remarks> Should only be called from the "first" storystage </remarks>
	public DataStructure GetTotalDS (DataStructure parentds) {
		DataStructure res = new DataStructure("story", parentds);
		var childcount = (ushort) (CountChildren() + 1);
		var children = GetChildren();
		children.Reverse();
		res.Set<ushort>("startstage", 0);
		res.Set("stagenum", childcount);
		for(ushort i=0; i < childcount; i++) {
			children [i].GetDS(res, i, i == childcount - 1);
		}
		return res;
	}

	/// <summary> The datastructure of the stage </summary>
	/// <param name="parentds"> The planned parent datastructure </param>
	/// <param name="index"> The number of the stage </param>
	/// <param name="is_last"> If it is the last stage in the story </param>
	/// <returns> DataStructure object </returns>
	public DataStructure GetDS (DataStructure parentds, ushort index, bool is_last=false) {
		DataStructure res = new DataStructure(string.Format("stage{0:000}", index), parentds);
		foreach (ICommandBehaviour command in commands) {
			command.GetDS(res);
		}
		// Provisory
		if (is_last) {
			var finish = new DataStructure("finish mission", res);
			finish.Set("won", true);
		} else {
			var goto_ = new DataStructure("goto", res);
			goto_.Set("stage", (ushort) (index + 1));
		}
		return res;
	}

	public void OnPointerDown(PointerEventData data) {
		if (data.button == PointerEventData.InputButton.Left) {
			dragging = true;
			bef_pos = Input.mousePosition;
		}
	}

	public override string ToString () {
		return string.Format("<Storystage: {0}>", string.Join(", ", System.Array.ConvertAll(commands.ToArray(), x => x.CType.ToString())));
	}
}

/// <summary> Common interface for the implication of the commends in the story stage </summary>
public interface ICommandBehaviour
{
	StoryStage Parent { get; set; }
	GameObject Obj { get; }
	Commands CType { get; }

	DataStructure GetDS (DataStructure parentds);
}

/// <summary> Enum with the 3 possible commands </summary>
public enum Commands
{
	spawn,
	conversation,
	objective
}