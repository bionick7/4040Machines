using FileManagement;
using UnityEngine;


public class AudioManager : MonoBehaviour
{
	public float TotalVolume { get; set; }
	public float UIVolume { get; set; }
	public float SpaceCraftVolume { get; set; }

	public AudioSource ui_audio_source;
	public AudioSource rcs_audio_source;
	public AudioSource weapon_audio_source;
	public AudioSource computer_audio_source;

	public SoundCollection collection;

	private void Start () {
		collection = Globals.soundcollection;

		ui_audio_source = Loader.EnsureComponent<AudioSource>(gameObject);
		rcs_audio_source = gameObject.AddComponent<AudioSource>();
		rcs_audio_source.loop = true;
		rcs_audio_source.clip = Globals.loaded_data.rcs_sound;

		computer_audio_source = gameObject.AddComponent<AudioSource>();
		computer_audio_source.loop = true;

		UpdateVolumes();
	}

	AudioClip[] ui_clips;

	private void LoadSounds () {
		ui_clips = Resources.LoadAll<AudioClip>("sounds/UI");
	}

	public void UpdateVolumes () {
		DataStructure volumes = Globals.settings.GetChild("sound volume");
		TotalVolume = volumes.Get<float>("total");
		UIVolume = volumes.Get<float>("UIsound");
		SpaceCraftVolume = volumes.Get<float>("spacecraft");
	}

	public void RCSPlay () {
		if (rcs_audio_source.isPlaying) return;
		rcs_audio_source.Play();
	}

	public void RCSStop () {
		rcs_audio_source.Stop();
	}

	public void Explosion (Explosion concerned) {
		GameObject emitter = new GameObject("explosion_sound_source");
		emitter.transform.SetParent(concerned.explosion_obj.transform);
	}

	public void ShootingSound (ShootingSound sound, float p_volume = 1f) {
		AudioClip clip = collection.GetShootingSound (sound.ToString());
		ui_audio_source.PlayOneShot(clip, p_volume * TotalVolume * SpaceCraftVolume);
	}

	public void ShootingSound (string sound, float p_volume=1f) {
		AudioClip clip = collection.GetShootingSound (sound);
		ui_audio_source.PlayOneShot(clip, p_volume * TotalVolume * SpaceCraftVolume);
	}

	public void UIPlay (UISound sound, float p_volume=1f) {
		AudioClip clip = collection.GetUISoud (sound.ToString());
		ui_audio_source.PlayOneShot(clip, p_volume * TotalVolume * UIVolume);
	}

	public void UIPlay (string sound, float p_volume=1f) {
		AudioClip clip = collection.GetUISoud (sound);
		ui_audio_source.PlayOneShot(clip, p_volume * TotalVolume * UIVolume);
	}

	public void ComputerPlay (ComputerSound sound, float p_volume=1f, bool ploop=false) {
		AudioClip clip = collection.GetComputerSound (sound.ToString());
		if (ploop) {
			computer_audio_source.clip = clip;
			computer_audio_source.Play();
		} else {
			computer_audio_source.PlayOneShot(clip, p_volume * TotalVolume * UIVolume);
		}
	}

	public void ComputerPlay (string sound, float p_volume=1f, bool ploop=false) {
		AudioClip clip = collection.GetComputerSound (sound);
		if (ploop) {
			computer_audio_source.clip = clip;
			computer_audio_source.Play();
		} else {
			computer_audio_source.PlayOneShot(clip, p_volume * TotalVolume * UIVolume);
		}
	}

	public void StopComputerSoundLoop () {
		computer_audio_source.Stop();
	}
}
