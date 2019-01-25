using UnityEngine;
using UnityEngine.UI;
using FileManagement;

public class NatonInformation : Retractable
{
	private Text name_t;
	private Text description;
	private RectTransform scroll_view_content;
	private Image flag;

	public static NatonInformation Active { get; private set; }

	private static DataStructure RogueDS {
		get {
			var res = new DataStructure("Rogue Nation");
			res.Set("description", "There is currently no faction controlling this celestial body");
			return res;
		}
	}

	private void Start () {
		var texts = GetComponentsInChildren<Text>();
		name_t = texts [0];
		description = texts [1];
		flag = transform.GetChild(1).GetComponent<Image>();
		scroll_view_content = transform.GetChild(2).GetChild(0).GetChild(0).GetComponent<RectTransform>();
		GetComponent<RectTransform>().sizeDelta = new Vector2(500, Screen.height * .5f - 50);
		Active = this;
		transform.position = new Vector3(-525, transform.position.y);
		SetVariables(1, transform.position, new Vector3(-10, transform.position.y));
	}

	private new void Update () {
		Shown = !CampagneManager.planet_view.none;
		base.Update();
	}

	public void UpdateLabels () {
		DataStructure nation_ds;
		if (CampagneManager.occupation_data.ContainsKey(CampagneManager.planet_view.name))
			nation_ds = Globals.nation_information.GetChild(CampagneManager.occupation_data[CampagneManager.planet_view.name]);
		else {
			nation_ds = RogueDS;
		}
		name_t.text = nation_ds.Name;
		description.text = nation_ds.Get<string>("description");
		Texture2D texture = nation_ds.Get<Texture2D>("flag");
		flag.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
	}
}