using UnityEngine;
using UnityEngine.UI;

public class ProfileBehaviour : MonoBehaviour {

	private Character character;

	public InputField forename;
	public InputField aftname;

	public Image cross1;
	public Image cross2;

	public Slider pilotS;
	public Slider computerS;
	public Slider enginearS;
	public Slider tradeS;
	public Slider diplomaticS;

	private RectTransform rect;

	private bool _shown;
	public bool Shown {
		get { return _shown; }
		set {
			_shown = value;
			if (value) {
				Init();
			} else {
				Exit(false);
			}
		}
	}

	private void Start () {
		Shown = false;
		rect = GetComponent<RectTransform>();

		character = Globals.current_character;
	}

	public void UpdateFile () {
		character.forename = forename.text;
		character.aftername = aftname.text;

		character.Save();
	}

	private void UpdateProfile () {
		if (character == null) { return; }

		forename.text = character.forename;
		aftname.text = character.aftername;

		cross1.transform.localPosition = new Vector3(100f * (float) character.politics[0],
													 100f * (float) character.politics[1]);
		cross2.transform.localPosition = new Vector3(100f * (float) character.politics[2],
													 100f * (float) character.politics[3]);

		pilotS.value = character.skills[Skills.pilot] / 100f;
		computerS.value = character.skills[Skills.computer] / 100f;
		enginearS.value = character.skills[Skills.engineering] / 100f;
		tradeS.value = character.skills[Skills.trade] / 100f;
		diplomaticS.value = character.skills[Skills.diplomacy] / 100f;
	}

	private void Init () {
		transform.position = new Vector3(300, Screen.height / 2 - 25);
		UpdateProfile();
	}

	public void Exit (bool from_button) {
		transform.position = new Vector3(-500, 0);
		if (from_button) {
			Globals.audio.UIPlay(UISound.soft_crackle);
			UpdateFile();
		}
	}
}
