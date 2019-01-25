using UnityEngine;
using UnityEngine.UI;
using FileManagement;

/* =========================================
 * Node in the story editor, that represents 
 * the "get conversation" action
 * ========================================= */

public class ConversationNode : MonoBehaviour, ICommandBehaviour {

	public Dropdown type_chooser;

	public StoryStage Parent { get; set; }
	public byte Num { get; set; }
	public GameObject Obj {
		get { return gameObject; }
	}
	public Commands CType {
		get { return Commands.conversation; }
	}

	private RectTransform own_transform;

	private bool initiated;

	/// <summary> Can be called either manually or at the beginning of the first frame </summary>
	public void Start_ () {
		own_transform = GetComponent<RectTransform>();
		initiated = true;
	}

	private void Start () {
		if (!initiated) Start_();
	}

	/// <summary> Should be called, if an input component is changed </summary>
	public void UpdateInp () {

	}

	/// <summary> Returns the corresponding datastructure </summary>
	/// <param name="parentds"> The planned parent </param>
	/// <returns> A DataStructure object </returns>
	public DataStructure GetDS (DataStructure parentds) {
		DataStructure res = new DataStructure("get conversation", parentds);
		res.Set<ushort>("ID", 0);
		return res;
	}

	/// <summary> Should be called, when the exit is pressed </summary>
	public void Exit () {
		Parent.Shorten(this);
		Destroy(gameObject);
	}
}
