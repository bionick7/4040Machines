using System.Collections.Generic;

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
	public ushort age;
	public Sex sex;
	public string forename;
	public string aftername;

	public ushort level;
	public ushort chapter;

	public string file_name = string.Empty;

	public List<ICollectable> inventory = new List<ICollectable>();

	private DataStructure datastr;

	/// <param name="sourcedata"> The datastructure containing thedata of the player </param>
	/// <param name="path"> The path, where the player's data is saaved, from /configs on </param>
	/// <example> <c>
	///		DataStructure player_data = DataStructure.Load("saved/characters/ivan.txt", "ivan", null);
	///		Character Ivan = new Character(sourcedata: player_data, path: "saved/characters/ivan.txt");
	/// </c> </example>
	public Character (DataStructure sourcedata, string path) {
		datastr = sourcedata;
		file_name = path;

		//UnityEngine.Debug.Log(sourcedata);

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
		age = general.Get<ushort>("age");
		sex = (Sex) general.Get<ushort>("sex");
		forename = general.Get<string>("forename");
		aftername = general.Get<string>("aftername");

		DataStructure progress = sourcedata.GetChild("progress");
		level = progress.Get<ushort>("level");
		chapter = progress.Get<ushort>("chapter");
	}

	/// <summary> Saves the character as a text-file </summary>
	/// <example> <c>
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
		general.Set("age", age);
		general.Set("sex", (ushort) sex);
		general.Set("forename", forename);
		general.Set("aftername", aftername);

		DataStructure progress = datastr.GetChild("progress");
		progress.Set("level", level);
		progress.Set("chapter", chapter);

		UnityEngine.Debug.Log(datastr);

		datastr.Save(file_name, true);
	}
}

public interface ICollectable
{

}