using UnityEngine;
using UnityEngine.UI;

public class ProfileBehaviour : MonoBehaviour {

	private Character character;

	public InputField forename;
	public InputField aftname;
	public InputField age;
	public Dropdown sex;

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
		get {
			return _shown;
		}
		set {
			_shown = value;
			if (value) {
				Init();
			} else {
				Exit();
			}
		}
	}

	private void Start () {
		Shown = false;
		rect = GetComponent<RectTransform>();

		character = Data.persistend.current_character;
	}

	public void UpdateFile () {
		Debug.Log(forename.text);
		character.forename = forename.text;
		character.aftername = aftname.text;
		character.age = System.UInt16.Parse(age.text);
		character.sex = (Sex) sex.value;

		character.Save();
	}

	private void UpdateProfile () {
		if (character == null) { return; }

		forename.text = character.forename;
		aftname.text = character.aftername;
		age.text = character.age.ToString();
		sex.value = ((int) character.sex);

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
		rect.position = new Vector3(300, Screen.height / 2 - 25);
		UpdateProfile();
	}

	private void Exit () {
		transform.position = new Vector3(-500, 0);
	}
}
