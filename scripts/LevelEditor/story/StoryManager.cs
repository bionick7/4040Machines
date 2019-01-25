using UnityEngine;

/* ===================================================
 * The behaviour of the left sidebar in the story view
 * =================================================== */

public class StoryManager : MonoBehaviour {

	public RectTransform content;
	public StoryStage sstemplate;

	public static StoryManager active;

	private void Start () {
		active = this;
	}

	/// <remarks> For the buttons </remarks>
	public void SpawnStoryStageB () { SpawnStoryStage(); }

	/// <summary> Spawns a story stage </summary>
	/// <returns> The spawned stage </returns>
	public StoryStage SpawnStoryStage () {
		GameObject obj = Instantiate(sstemplate.gameObject);
		obj.transform.SetParent(content);
		obj.transform.position = new Vector3(500, 200);
		return Loader.EnsureComponent<StoryStage>(obj);
	}

	/// <summary> Should be called, when the exit button is pressed </summary>
	public void Exit () {
		EditorGeneral.StoryView = false;
	}
}
