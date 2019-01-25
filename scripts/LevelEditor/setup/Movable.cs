using UnityEngine;
using UnityEngine.UI;

/* ===============================================================
 * Behavoiour of everything, that can be rotated, repositioned and
 * has velocity and angular velocity (ships & targets)
 * =============================================================== */

public class Movable : MonoBehaviour
{
	private Arrows arrows;
	public IInteractable correspondence = null;
	public Image circ;

	private static readonly Color target_color = new Color(.5f, .47f, .4f);

	/// <summary> Position in global space </summary>
	public Vector3 Position {
		get { return transform.position; }
		set {
			transform.position = value;
			if (correspondence != null)
				correspondence.Position = value;
		}
	}

	private Vector3 _rotation;
	/// <summary> Rotation in global space </summary>
	public Vector3 Rotation {
		get { return _rotation; }
		set {
			_rotation = value;
			transform.rotation = Quaternion.Euler(value);
			if (correspondence != null)
				correspondence.Rotation = value;
		}
	}

	private Vector3 _velocity;
	/// <summary> Velocity in global space </summary>
	public Vector3 Velocity {
		get { return _velocity; }
		set {
			_velocity = value;
			if (correspondence != null)
				correspondence.Velocity = value;
		}
	}

	private Vector3 _angular_velocity;
	/// <summary> Angular velocity in global space </summary>
	public Vector3 AngularVelocity {
		get { return _angular_velocity; }
		set {
			_angular_velocity = value;
			if (correspondence != null)
				correspondence.AngularVelocity = value;
		}
	}
	
	private void Start () {
		EditorGeneral.active.AddMovable(this);
		circ = Instantiate(GameObject.Find("circle_ind")).GetComponent<Image>();
		circ.transform.SetParent(EditorGeneral.mainV.transform);
		circ.transform.SetAsFirstSibling();
		if (correspondence != null) {
			Position = correspondence.Position;
			Rotation = correspondence.Rotation;
		}
	}

	private void Update () {
		circ.transform.position = EditorGeneral.maincam.WorldToScreenPoint(Position);
		if (correspondence is EDTarget) circ.color = target_color;
		else if (correspondence is EDShip) circ.color = (correspondence as EDShip).Squad.color;
	}

	/// <summary> Should be called on mousclick for every movable </summary>
	/// <param name="hit"> The ray that represents all the points behind the mouse on the screen </param>
	public void CheckClicked (RaycastHit hit) {
		Vector3 mousepos = Input.mousePosition;
		mousepos.z = 0;
		Vector3 selfpos = EditorGeneral.maincam.WorldToScreenPoint(Position);
		selfpos.z = 0;
		if ((mousepos - selfpos).sqrMagnitude <= 625) {
			EditorGeneral.arrows.Select(this);
			if (correspondence is EDShip) {
				EditorGeneral.InspectorType = InspectorType.ship;
			} else if (correspondence is EDTarget) {
				EditorGeneral.InspectorType = InspectorType.target;
			}
		}
	}
}
