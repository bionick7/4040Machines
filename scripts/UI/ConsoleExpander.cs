using UnityEngine;
using UnityEngine.UI;

public class ConsoleExpander : MonoBehaviour
{
	public Sprite down;
	public Sprite up;

	private ConsoleBehaviour console;
	private Image img;

	private bool _on;
	public bool On {
		get { return _on; }
		set {
			img.sprite = value ? up : down;
			console.ConsolePos = value ? ConsolePosition.lower : ConsolePosition.shown;
			_on = value;
		}
	}

	private void Start () {
		img = Loader.EnsureComponent<Image>(gameObject);
		console = GetComponentInParent<ConsoleBehaviour>();
		_on = console.ConsolePos != ConsolePosition.lower;
	}

	public void OnClick () {
		On = !On;
	}
}
