using FileManagement;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
	private AudioSource _audio;
	private bool initialized = false;

	private void Start () {
		if (!initialized) Start_();
	}

	public void Start_ () {
		if (Globals.music_player == null) {
			Globals.music_player = this;
			transform.SetParent(GameObject.FindGameObjectWithTag("Persistent").transform, true);
		} else {
			Destroy(gameObject);
		}
		_audio = GetComponent<AudioSource>();
		_audio.loop = true;
		UpdateVolume(0);
		initialized = true;
	}

	public void PlayMusic (string title) {
		if (Globals.loaded_data.music_dict.ContainsKey(title)) {
			_audio.clip = Globals.loaded_data.music_dict[title][(int) Mathf.Floor(Random.Range(0, Globals.loaded_data.music_dict[title].Length - .001f))];
			_audio.Play();
		} else {
			Debug.LogFormat("Title does not exist: {0};\n {1}", title, DeveloppmentTools.LogIterable(Globals.loaded_data.music_dict.Keys));
		}
	}

	public void UpdateVolume (float _) {
		_audio.volume =  Volumes.music * Volumes.music * Volumes.total * Volumes.total;
	}
}

public static class Volumes
{
	public static float total;
	public static float music;
	public static float sfx;
	public static float ui;

	public static void Initialize (DataStructure ds) {
		total = ds.Get<float>("total");
		music = ds.Get<float>("music");
		sfx = ds.Get<float>("spacecraft");
		ui = ds.Get<float>("UIsound");
	}

	public static void UpdateTotal (float v) {
		total = v;
	}
	
	public static void UpdateMusic (float v) {
		music = v;
	}
	
	public static void UpdateSFX (float v) {
		sfx = v;
	}
	
	public static void UpdateUI (float v) {
		ui = v;
	}
}