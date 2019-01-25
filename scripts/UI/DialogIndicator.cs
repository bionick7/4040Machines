using UnityEngine;

public class DialogIndicator : MonoBehaviour {

	public static DialogIndicator Active { get; set; }

	private RectTransform rect_trans;
	private Vector3 target_size;
	private const float zoom_speed = 300;

	private	static Vector3 ScreenSize {
		get { return new Vector3(Screen.width, Screen.height); }
	}

	private void Start () {
		rect_trans = GetComponent<RectTransform>();
		Active = this;
	}

	private void Update () { 
		if (rect_trans.sizeDelta.x > target_size.x) {
			rect_trans.sizeDelta -= Vector2.one * zoom_speed * Time.deltaTime;
		}
	}

	public void Set (float x_pos, float y_pos, float x_size, float y_size, Anchor anchor) {
		Vector3 offset = Vector3.zero;
		target_size = new Vector3(x_size, y_size);
		rect_trans.sizeDelta = target_size + new Vector3(100, 100);
		switch (anchor) {
		case Anchor.TopLeft:
			offset = new Vector3(0, Screen.height);
			break;
		case Anchor.TopMid:
			offset = new Vector3(Screen.width * .5f, Screen.height);
			break;
		case Anchor.TopRight:
			offset = ScreenSize;
			break;
		case Anchor.MidLeft:
			offset = new Vector3(0, Screen.height * .5f);
			break;
		case Anchor.MidMid:
			offset = ScreenSize * .5f;
			break;
		case Anchor.MidRight:
			offset = new Vector3(Screen.width, Screen.height * .5f);
			break;
		case Anchor.LowerMid:
			offset = new Vector3(Screen.width * .5f, 0);
			break;
		case Anchor.LowerRight:
			offset = new Vector3(Screen.width, 0);
			break;
		case Anchor.LowerLeft:
		default: break;
		}
		rect_trans.position = new Vector3(x_pos, y_pos) + offset;
	}

	public enum Anchor
	{
		TopLeft = 1,
		TopMid = 2,
		TopRight = 3,
		MidLeft = 4,
		MidMid = 5,
		MidRight = 6,
		LowerLeft = 7,
		LowerMid = 8,
		LowerRight = 9
	}
}
