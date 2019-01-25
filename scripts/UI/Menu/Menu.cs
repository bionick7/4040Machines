using UnityEngine;

public class Menu : MonoBehaviour {

	private ProfileBehaviour profile;
	private SettingsBehaviour settings;
	private LevelChoice level_choice;
	private KeyBindingOptions key_binding_options;

	private void Start () {
		GameObject canvas = GameObject.Find("Canvas");

		profile = canvas.GetComponentInChildren<ProfileBehaviour>();
		settings = canvas.GetComponentInChildren<SettingsBehaviour>();
		level_choice = canvas.GetComponentInChildren<LevelChoice>();
		key_binding_options = canvas.GetComponentInChildren<KeyBindingOptions>();

		settings.Start_();

		profile.Shown = false;
		settings.Shown = false;
		level_choice.Shown = false;
		key_binding_options.Shown = false;
	}

	public void Resume () {
		Globals.persistend.Campagne();
		Globals.audio.UIPlay(UISound.soft_crackle);
	}
	
	public void NewGame () {
		Globals.persistend.LoadSave("saved/Saves/def_save");
		Globals.audio.UIPlay(UISound.soft_crackle);
	}

	public void ProceduralGame () {
		profile.Shown = false;
		settings.Shown = false;
		key_binding_options.Shown = false;
		level_choice.Shown = !level_choice.Shown;
		Globals.audio.UIPlay(UISound.soft_crackle);
	}

	public void Settings () {
		profile.Shown = false;
		level_choice.Shown = false;
		key_binding_options.Shown = false;
		settings.Shown = !settings.Shown;
		Globals.audio.UIPlay(UISound.soft_crackle);
	}

	public void KeyBindings () {
		profile.Shown = false;
		level_choice.Shown = false;
		settings.Shown = false;
		key_binding_options.Shown = !key_binding_options.Shown;
		Globals.audio.UIPlay(UISound.soft_crackle);
	}

	public void Profile () {
		settings.Shown = false;
		level_choice.Shown = false;
		key_binding_options.Shown = false;
		profile.Shown = !profile.Shown;
		Globals.audio.UIPlay(UISound.soft_crackle);
	}

	public void Exit () {
		Application.Quit();
		Globals.audio.UIPlay(UISound.soft_crackle);
	}

	public void LevelEditor () {
		UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName: "editor");
	}

	public void OnGUI () {
		GUI.Label(new Rect(0, 0, 1000, 100), Application.dataPath);
	}
}
