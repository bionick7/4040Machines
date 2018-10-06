using UnityEngine;
using UnityEngine.UI;

public class ConsoleExpander : MonoBehaviour
{
	public Sprite down;
	public Sprite up;

	private Image img;
	private GUIScript ui_script;

	private bool _on;
	public bool On {
		get {
			return _on;
		}
		set {
			img.sprite = value ? up : down;
			ui_script.ConsolePos = value ? ConsolePosition.lower : ConsolePosition.shown;
			_on = value;
		}
	}

	private void Start () {
		img = Loader.EnsureComponent<Image>(gameObject);
		ui_script = SceneData.ui_script;
		_on = ui_script.ConsolePos != ConsolePosition.lower;
	}



	public void OnClick () {
		On = !On;
	}
}
