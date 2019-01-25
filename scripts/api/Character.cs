using System.Collections.Generic;
using FileManagement;

/* =========================================
 * The Character the player is playing with,
 * With his/her skills/politics ...
 * ========================================= */

/// <summary>
///		The Character, who is played.
///		Each character has his own game/campagne/stats.
/// </summary>
public class Character
{
	/// <summary> Political orentation of the character, expressed as an array of 4 doubles between -1 and 1 </summary>
	public double[] politics = new double[4] { 0d, 0d, 0d, 0d };
	/// <summary> The skills expressed as numbers between 0 and 100 </summary>
	public Dictionary<Skills, ushort> skills = new Dictionary<Skills, ushort>();

	// player-defined variables
	public string forename;
	public string aftername;

	public DataStructure campagne;
	private string campagne_path;
	public ushort story_stage;
	public string chapter;

	public string file_name = string.Empty;

	public List<ICollectable> inventory = new List<ICollectable>();

	private DataStructure datastr;

	public Chapter LoadedChapter {
		get {
			if (!System.Array.Exists(campagne.AllChildren, x => x.Get<string>("name") == chapter)) {
				DeveloppmentTools.Log(string.Format("Could not find chapter {0}", chapter));
				return Chapter.Empty;
			}
			return new Chapter(System.Array.Find(campagne.AllChildren, x => x.Get<string>("name") == chapter));
		}
	}

	/// <param name="sourcedata"> The datastructure containing thedata of the player </param>
	/// <param name="path"> The path, where the player's data is saaved, from /configs on </param>
	/// <example> <c>
	///		DataStructure player_data = DataStructure.Load("saved/characters/ivan.txt", "ivan", null);
	///		Character Ivan = new Character(sourcedata: player_data, path: "saved/characters/ivan.txt");
	/// </c> </example>
	public Character (DataStructure sourcedata, string path) {
		datastr = sourcedata;
		file_name = path;

		DataStructure polit = sourcedata.GetChild("political");
		politics [0] = polit.Get<double>("cap");
		politics [1] = polit.Get<double>("auth");
		politics [2] = polit.Get<double>("nat");
		politics [3] = polit.Get<double>("trad");

		DataStructure skilldata = sourcedata.GetChild("skills");
		skills [Skills.pilot] = skilldata.Get<ushort>("pilot");			
		skills [Skills.computer] = skilldata.Get<ushort>("computer");
		skills [Skills.engineering] = skilldata.Get<ushort>("engineering");
		skills [Skills.trade] = skilldata.Get<ushort>("trade");
		skills [Skills.diplomacy] = skilldata.Get<ushort>("diplomacy");

		DataStructure general = sourcedata.GetChild("stats");
		forename = general.Get<string>("forename");
		aftername = general.Get<string>("aftername");

		DataStructure progress = sourcedata.GetChild("progress");
		campagne = DataStructure.Load(progress.Get("campagne", "campagne/campagne_mars", quiet: true));
		story_stage = progress.Get<ushort>("level");
		chapter = progress.Get<string>("chapter");
	}

	/// <summary> Saves the character as a text-file </summary>
	/// <example> <c>
	///		Characte Ivan = new Character(ivan_ds, "characters/ivan");
	///		Ivan.level++;
	///		Ivan.Save();
	/// </c> </example>
	public void Save () {
		DataStructure polit = datastr.GetChild("political");
		polit.Set("cap", politics[0]);
		polit.Set("auth", politics[1]);
		polit.Set("nat", politics[2]);
		polit.Set("trad", politics[3]);

		DataStructure skilldata = datastr.GetChild("skills");
		skilldata.Set("pilot", skills [Skills.pilot]);
		skilldata.Set("computer", skills [Skills.computer]);
		skilldata.Set("engineering", skills [Skills.engineering]);
		skilldata.Set("trade", skills [Skills.trade]);
		skilldata.Set("diplomacy", skills [Skills.diplomacy]);

		DataStructure general = datastr.GetChild("stats");
		general.Set("forename", forename);
		general.Set("aftername", aftername);

		DataStructure progress = datastr.GetChild("progress");
		progress.Set("level", story_stage);
		progress.Set("chapter", chapter);

		datastr.Save(file_name, true);
	}
}

public interface ICollectable
{

}