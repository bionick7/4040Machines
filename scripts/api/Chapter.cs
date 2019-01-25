using System.Collections.Generic;
using FileManagement;

/* ========================================================================================
 * The chapter is a bigger part of a campagn containing multiple conversations and battles
 * There is everytime only one chapter loaded at a time
 * ======================================================================================== */

public class Chapter
{
	/// <summary> The battle, that are int he chapter, in chronological order </summary>
	public ChapterBattle[] battles;
	/// <summary> The conversations of the chapter, in chronological order </summary>
	public ChapterConversation[] conversations;
	public ChapterJump[] jumps;

	public IChapterEvent[] all_events;

	/// <summary> The played character </summary>
	public Character character;

	public string name;
	public string directory_name;

	// Indexer indexes the balttles
	public ChapterBattle this [ushort index] {
		get { return battles [index]; }
		set { battles [index] = value; }
	}

	/// <summary> Constructor directly interprets the Data structures </summary>
	/// <param name="data"> The datastructure in question </param>
	public Chapter (DataStructure data) {
		//UnityEngine.Debug.Log(data);

		NMS.OS.OperatingSystem os = Globals.current_os;

		name = data.Get<string>("name", quiet:true);
		directory_name = "campagne/" + data.Get<string>("directory", quiet:true) + "/";

		List<ChapterBattle> battle_list = new List<ChapterBattle>();
		List<ChapterConversation> conversation_list = new List<ChapterConversation>();
		List<ChapterJump> jump_list = new List<ChapterJump>();
		foreach (DataStructure child in data.AllChildren) {
			switch (child.Name) {
			case "battle":
				DataStructure datastr = DataStructure.Load(directory_name + child.Get<string>("filename"), parent: child);
				battle_list.Add(new ChapterBattle() {
					AviableOn = child.Get<ushort[]>("aviable"),
					Name = child.Get<string>("name"),
					path = directory_name + child.Get<string>("filename"),
					own_data = datastr,
					planet_name = child.Get("planet", "Trantor"),
					progress = child.Get<ushort>("progress", 0),
				});
				break;
			case "conversation":
				conversation_list.Add(new ChapterConversation() {
					AviableOn = child.Get<ushort []>("aviable"),
					Name = child.Get<string>("name"),
					own_data = child,
					progress = child.Get<ushort>("progress"),
				});
				break;
			case "jump chapter":
				jump_list.Add(new ChapterJump() {
					AviableOn = child.Get<ushort []>("aviable"),
					Name = child.Get<string>("name"),
					new_chapter = child.Get<string>("chapter name"),
				});
				break;
			}
		}

		battles = battle_list.ToArray();
		conversations = conversation_list.ToArray();
		jumps = jump_list.ToArray();

		all_events = new IChapterEvent [battles.Length + conversations.Length + jumps.Length];
		battles.CopyTo(all_events, 0);
		conversations.CopyTo(all_events, battles.Length);
		jumps.CopyTo(all_events, battles.Length + conversations.Length);
	}

	public static Chapter Empty {
		get { return new Chapter(DataStructure.Empty); }
	}
}

public interface IChapterEvent
{
	ushort [] AviableOn { get; set; }
	string Name { get; set; }
}

/// <summary> Defines a battle for the campaign/chapter purposes </summary>
public struct ChapterBattle : IChapterEvent
{
	public ushort[] AviableOn { get; set; }
	public string Name { get; set; }
	public string path;
	public DataStructure own_data;
	public string planet_name;
	public ushort progress;

	public static readonly ChapterBattle None = new ChapterBattle() {
		AviableOn = new ushort[0],
		Name = "NULL",
		path = "C:",
		own_data = DataStructure.Empty,
		planet_name = "Trantor",
		progress = 0,
	};

	public override string ToString () {
		return string.Format("<ChapterBattle: \"{0}\" >", Name);
	}
}

public struct ChapterConversation : IChapterEvent
{
	public ushort[] AviableOn { get; set; }
	public string Name { get; set; }
	public DataStructure own_data;
	public ushort progress;

	public override string ToString () {
		return string.Format("<ChapterConversation: \"{0}\" >", Name);
	}
}

public struct ChapterJump : IChapterEvent
{
	public ushort[] AviableOn { get; set; }
	public string Name { get; set; }
	public string new_chapter;

	public override string ToString () {
		return string.Format("<ChapterJump: \"{0}\" >", Name);
	}
}