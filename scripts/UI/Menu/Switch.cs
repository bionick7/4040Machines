using UnityEngine;
using UnityEngine.UI;

public class Switch : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler {

	public Sprite on_sprite;
	public Sprite off_sprite;

	private Image img;
	private AudioClip sound;
	private AudioSource audio_src;

	private bool _on;
	public bool On {
		get {
			return _on;
		}
		set {
			SwitchSprite(value);
			_on = value;
		}
	}

	private void Start () {
		img = GetComponent<Image>();
		audio_src = Loader.EnsureComponent<AudioSource>(transform.root.gameObject);
		sound = Resources.Load<AudioClip>("sounds/switch_click");
		_on = false;
	}
	
	private void SwitchSprite(bool on) {
		img.sprite = on ? on_sprite : off_sprite;
		if (_on != on) {
			audio_src.PlayOneShot(sound, 1);
		}
	}

	public void TriggerQuiet(bool on) {
		_on = on;
		img.sprite = on ? on_sprite : off_sprite;
	}

	public void OnPointerClick(UnityEngine.EventSystems.PointerEventData data) {
		On = !On;
		Globals.audio.UIPlay(UISound.sharp_click);
	}
}
