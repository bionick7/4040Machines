using System.Collections.Generic;

/* ========================================================================================
 * The chapter is a bigger part of a campagn containing multiple conversations and battles
 * There is everytime only one chapter loaded at a time
 * ======================================================================================== */

public class Chapter
{
	public Battle[] battles;
	public Conversation[] conversations;
	public Character character;

	public string name;
	public string directory_name;

	public Battle this [ushort index] {
		get {
			return battles [(int) index];
		}
		set {
			battles [(int) index] = value;
		}
	}

	public Chapter (DataStructure data) {
		NMS.OS.OperatingSystem os = Data.current_os;

		name = data.Get<string>("name");
		directory_name = "campagne/" + data.Get<string>("directory") + "/";

		List<Battle> battle_list = new List<Battle>();
		List<Conversation> conversation_list = new List<Conversation>();
		foreach (DataStructure child in data.AllChildren) {
			switch (child.Name) {
			case "battle":
				DataStructure datastr = DataStructure.Load(directory_name + child.Get<string>("filename"), parent: child);
				battle_list.Add(new Battle() {
					aviable_on = (byte) child.Get<ushort>("aviable"),
					name = child.Get<string>("name"),
					own_data = datastr
				});
				break;
			case "conversation":
				conversation_list.Add(new Conversation(child, os));
				break;
			}
		}

		battles = battle_list.ToArray();
		conversations = conversation_list.ToArray();
	}
}

public struct Battle
{
	public byte aviable_on;
	public string name;
	public DataStructure own_data;
}