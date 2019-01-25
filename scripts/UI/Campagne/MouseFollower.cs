using UnityEngine;
using UnityEngine.UI;

public class MouseFollower : MonoBehaviour {

	private Image img;
	private Text text;

	private static readonly Vector3 offset = new Vector3(0, 0);

	private bool _shown;
	public bool Shown {
		get { return _shown; }
		set {
			img.enabled = value;
			text.enabled = value;
			_shown = value;
		}
	}

	private void Start () {
		img = GetComponent<Image>();
		text = GetComponentInChildren<Text>();
	}

	private void Update () {
		transform.position = Input.mousePosition + offset;
		if (CampagneManager.planet_hover.none) {
			Shown = false;
		} else {
			Shown = true;
			text.text = string.Format("{0} -\n {1} battles", CampagneManager.planet_hover.name, CampagneManager.planet_hover.battles.Length);
		}
	}
}
