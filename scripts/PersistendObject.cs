using UnityEngine;
using UnityEngine.SceneManagement;
using FileManagement;

public class PersistendObject : MonoBehaviour {

	private DataStructure persistend_data;

	public Loader loader = null;
	public Character current_character;

	public NMS.OS.OperatingSystem base_os;

	private bool is_updatemenu = false;
	private bool menu_music_played = false;

	private string saved_path_loaded = "";

	private void Awake () {
		// Make this persistent
		if (GameObject.FindGameObjectsWithTag("Persistent").Length > 1) {
			Destroy(gameObject);
			return;
		}
		DontDestroyOnLoad(gameObject);
		Globals.persistend = this;

		var sound_coll = new SoundCollection("sounds");

		SetCursor();
		UpdateOnMenu();

		Volumes.Initialize(Globals.settings.GetChild("sound volume"));
		DeveloppmentTools.Testing();
	}

	private void OnLevelWasLoaded (int level) {
		if (is_updatemenu && level == 1) {
			UpdateOnMenu();
		}
		if (level == 2 && saved_path_loaded != "") {
			LoadSaving.Load(saved_path_loaded);
			saved_path_loaded = "";
		}
	}

	private void Update () {
		if (!menu_music_played) {
			Globals.music_player.PlayMusic("MenuMusic");
			menu_music_played = true;
		}
	}

	private void UpdateOnMenu() {
		Globals.audio = FindObjectOfType<AudioManager>();
		ConsoleBehaviour console = FindObjectOfType<ConsoleBehaviour>();

		console.Start_();

		base_os = new NMS.OS.OperatingSystem(console, null);
		Globals.current_os = base_os;

		is_updatemenu = false;
	}

	private void SetCursor () {
		Cursor.lockState = CursorLockMode.None;
		Cursor.SetCursor(Resources.Load<Texture2D>("Pointer"), Vector2.zero, CursorMode.Auto);
	}

	/// <summary> Loads a battle </summary>
	public void LoadBattle (string battle_name) {
		TerminateScene();
		if (!Globals.battle_list.ContainsChild(battle_name)) { return; }
		DataStructure battle_inforamtion = Globals.battle_list.GetChild(battle_name);
		string battle_file = battle_inforamtion.Get<string>("path");

		DataStructure battle_data = DataStructure.Load(battle_file, "battle_data", is_general:true);
		if (battle_data == null) DeveloppmentTools.Log(battle_file + " does not exist");
		
		SceneGlobals.is_save = false;
		SceneManager.LoadScene(sceneName: "battlefield");
		loader = new Loader(battle_data) {
			path = battle_file
		};
	}

	/// <summary> Loads a battle </summary>
	public void LoadBattle (DataStructure battle_data, string path) {
		TerminateScene();

		SceneGlobals.is_save = false;
		SceneManager.LoadScene(sceneName: "battlefield");
		loader = new Loader(battle_data) {
			path = path
		};
	}

	/// <summary> Loads next battle of campaign </summary>
	public void Campagne () {
		TerminateScene();
		Globals.music_player.PlayMusic("CampagneMusic");
		SceneManager.LoadScene(sceneName: "Campagne");
	}

	public void LoadSave (string path) {
		if (!System.IO.File.Exists(DataStructure.GeneralPath + path + ".cfgt")) {
			DeveloppmentTools.Log(path + " does not exist");
			return;
		}

		SceneGlobals.is_save = true;
		SceneManager.LoadScene(sceneName: "battlefield");
		saved_path_loaded = path;
	}

	/// <summary> Loads a character </summary>
	public void LoadCharacter (Character chosen) {
		TerminateScene();
		SceneManager.LoadScene(sceneName: "menu");
	}

	/// <summary> To end the game </summary>
	/// <param name="won"> True, if the game is won, else false </param>
	public void Back2Menu () {
		TerminateScene();
		menu_music_played = false;
		SceneManager.LoadScene(sceneName: "menu");
		is_updatemenu = true;
	}

	public void TerminateScene () {
		Globals.current_os.console.Terminate();
	}
}