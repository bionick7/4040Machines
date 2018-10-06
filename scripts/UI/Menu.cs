using UnityEngine;

public class Menu : MonoBehaviour {

	private ProfileBehaviour profile;
	private SettingsBehaviour settings;
	private LevelChoice level_choice;

	private void Start () {
		profile = GameObject.Find("Profile").GetComponent<ProfileBehaviour>();
		settings = GameObject.Find("Settings").GetComponent<SettingsBehaviour>();
		level_choice = GameObject.Find("Level_choice").GetComponent<LevelChoice>();

		//GameObject.Find("resume").GetComponentInChildren<UnityEngine.UI.Text>().text = Application.dataPath;
	}

	public void Resume () {
		Data.persistend.Campagne();
	}
	
	public void NewGame () {

	}

	public void ProceduralGame () {
		profile.Shown = false;
		settings.Shown = false;
		level_choice.Shown = !level_choice.Shown;
	}

	public void Settings () {
		profile.Shown = false;
		level_choice.Shown = false;
		settings.Shown = !settings.Shown;
	}

	public void Profile () {
		settings.Shown = false;
		level_choice.Shown = false;
		profile.Shown = !profile.Shown;
	}

	public void Exit () {
		Application.Quit();
	}

	public void OnGUI () {
		GUI.Label(new Rect(0, 0, 1000, 100), Application.dataPath);
	}
}
