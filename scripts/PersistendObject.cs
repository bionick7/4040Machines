using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistendObject : MonoBehaviour {

	private DataStructure persistend_data;

	public Loader loader = null;
	public Character current_character;

	public NMS.OS.OperatingSystem base_os;

	private void Awake () {
		// Make this persistent
		if (GameObject.FindGameObjectsWithTag("Persistent").Length > 1) {
			Destroy(gameObject);
			return;
		}
		DontDestroyOnLoad(gameObject);
		Data.persistend = this;

		// Debug Logging
		string[] dir_split = Application.dataPath.Split('/');
		string last_path = dir_split[dir_split.Length - 1];
		FileReader.logfile = Application.dataPath.Substring(0, Application.dataPath.Length - last_path.Length) + "logfile.txt";
		System.IO.File.WriteAllText(FileReader.logfile, string.Empty);

		// Initialize everything
		Data.persistend_data = persistend_data = new DataStructure("persistend_data");

		base_os = new NMS.OS.OperatingSystem(null);
		Data.current_os = base_os;
		_Update();

		foreach (Ammunition ammo in Loader.LoadAmmo(Data.ammunition)) {
			if (!ammo.IsNone) {
				Data.ammunition_insts.Add(ammo.name, ammo);
			}
		}

		Data.characters = Loader.Load_Characters(DataStructure.GeneralPath + "saved/characters");
		current_character = Data.characters[0];

		// Provisory
		DataStructure campagne = Loader.Load_Campagnes(DataStructure.GeneralPath + "campagne")[0];
		Data.loaded_chapter = new Chapter(campagne.AllChildren[current_character.chapter]);

		Testing();
	}

	/// <summary> Use this to test things out </summary>
	public void Testing () {
		foreach (DataStructure Ship in Data.premade_ships.AllChildren) {
			//Debug.Log(Ship.Name);
		}
	}

	/// <summary> Called to update data </summary>
	public void _Update () {
		Data.settings =  DataStructure.Load("saved/settings.txt", "settings", persistend_data);
		Data.parts = DataStructure.LoadFromDir("parts", "parts", persistend_data);
		Data.premade_ships = DataStructure.Load("ships/premade_ships.txt", "premade", persistend_data);
		Data.battle_list = DataStructure.Load("battles/battle_list.txt", "battles", persistend_data);
		Data.ammunition = DataStructure.LoadFromDir("ammunition", "ammunition", persistend_data);
	}

	/// <summary> Loads a battle </summary>
	public void LoadBattle (string battle_name) {
		if (!Data.battle_list.ContainsChild(battle_name)) { return; }
		DataStructure battle_inforamtion = Data.battle_list.GetChild(battle_name);
		string battle_file = "battles/" + battle_inforamtion.Get<string>("name") + ".txt";
		DataStructure battle_data = DataStructure.Load(battle_file, "battle_data");

		SceneManager.LoadScene(sceneName: "battlefield");
		loader = new Loader(battle_data);
	}

	/// <summary> Loads next battle of campaign </summary>
	public void Campagne () {
		Battle battle = Data.loaded_chapter[current_character.chapter];

		SceneManager.LoadScene(sceneName: "battlefield");
		loader = new Loader(battle.own_data);
	}

	/// <summary> Loads a character </summary>
	public void LoadCharacter (Character chrctr) {
		Character chosen = chrctr;
		SceneManager.LoadScene(sceneName: "menu");
	}

	/// <summary> To end the game </summary>
	/// <param name="won"> True, if the game is won, else false </param>
	public void EndBattle (bool won, bool progress = false) {
		SceneManager.LoadScene(sceneName: "menu");
	}
}