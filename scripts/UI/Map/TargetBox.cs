using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image), typeof(RectTransform))]
public class TargetBox : MonoBehaviour
{
	public bool is_aim;

	private Image img;
	private RectTransform rect_trans;
	private Camera cam;
	private IAimable parent;

	private float pending_value;
	private float alpha;
	private float size;
	
	// Constant values
	private static readonly Color target_color = new Color(1, 0, 0);
	private static readonly Color aim_color = new Color(1, 1, 0);
	private const float min_size = 50f;
	private const float max_size = 100f;
	private const float min_alpha = .2f;
	private const float max_alpha = .96f;

	private bool _shown;
	private bool Shown {
		get { return _shown; }
		set {
			img.enabled = value;
			_shown = value;
		}
	}

	private void Start () {
		img = GetComponent<Image>();
		rect_trans = GetComponent<RectTransform>();
		cam = SceneGlobals.map_camera;
		pending_value = is_aim ? 0 : Mathf.PI / 2;
	}

	private void Update () {
		parent = is_aim ? SceneGlobals.Player.TurretAim : SceneGlobals.Player.Target;
		Shown = parent != null && parent.Exists;

		if (Shown && Vector3.Angle(parent.Position - cam.transform.position, cam.transform.forward) < 90)
			transform.position = cam.WorldToScreenPoint(parent.Position);
		UpdateColor_Size();
	}

	private void UpdateColor_Size () {
		pending_value += Time.deltaTime * 2;

		// Color
		alpha = min_alpha + (Mathf.Sin(pending_value) * .5f + .5f) * (max_alpha - min_alpha);
		Color new_col = is_aim ? aim_color : target_color;
		new_col.a = alpha;
		img.color = new_col;

		// Size
		size = min_size + (.5f - Mathf.Sin(pending_value) * .5f) * (max_size - min_size);
		rect_trans.sizeDelta = new Vector2(size, size);
	}
}
