using UnityEngine;

public class SoundCollection
{
	private string path;

	public SoundCollection (string p_path) {
		path = p_path;

		Globals.soundcollection = this;
	}


	public AudioClip GetUISoud (string sound) {
		if (!Globals.loaded_data.ui_sounds.ContainsKey(sound)) {
			return Globals.loaded_data.placeholder_sound;
		}
		return Globals.loaded_data.ui_sounds [sound];
	}

	public AudioClip GetComputerSound (string sound) {
		if (!Globals.loaded_data.computer_sounds.ContainsKey(sound)) {
			return Globals.loaded_data.placeholder_sound;
		}
		return Globals.loaded_data.computer_sounds [sound];
	}

	public AudioClip GetShootingSound (string sound) {
		if (!Globals.loaded_data.weapon_sounds.ContainsKey(sound)) {
			return Globals.loaded_data.placeholder_sound;
		}
		return Globals.loaded_data.weapon_sounds[sound];
	}
}

public enum ComputerSound
{
	writing_ticks,
}

public enum UISound
{
	dump_click,
	sharp_click,
	camera_switch,
	soft_crackle,
	soft_click,
}

public enum ShootingSound
{
	gettling,
	c_12mm,
}