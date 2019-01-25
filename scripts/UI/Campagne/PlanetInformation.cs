using UnityEngine;
using UnityEngine.UI;

public class PlanetInformation : Retractable
{
	private Text name_t;
	private Text description;
	
	public static PlanetInformation Active { get; private set; }
	public static ChapterBattle chosen = ChapterBattle.None;

	private void Start () {
		var texts = GetComponentsInChildren<Text>();
		name_t = texts [0];
		description = texts [1];
		Shown = false;
		GetComponent<RectTransform>().sizeDelta = new Vector2(500, Screen.height * .5f - 50);
		Active = this;
		transform.position = new Vector3(-525, transform.position.y);
		SetVariables(1, transform.position, new Vector3(-10, transform.position.y));
	}

	private new void Update () {
		Shown = !CampagneManager.planet_view.none;
		base.Update();
	}

	public void UpdateLabels (CelestialData data) {
		name_t.text = data.name;
		description.text = data.description;
	}
}
