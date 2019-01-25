using System;
using UnityEngine;
using UnityEngine.UI;

/* ========================================
 * The General Inspector in the upper-right
 * ======================================== */

public class Inspector : MonoBehaviour {

	public InputField xpos;
	public InputField ypos;
	public InputField zpos;

	public Slider xrot;
	public Slider yrot;
	public Slider zrot;

	public Transform velocity_par;
	public Transform angular_velocity_par;

	#region ValuesStored
	private InputField vel_x;
	private InputField vel_y;
	private InputField vel_z;
	private InputField ang_x;
	private InputField ang_y;
	private InputField ang_z;
	#endregion

	#region InputValues
	/// <summary> The position, that has been put in the inspector </summary>
	public Vector3 InputPosition {
		get {
			float x = Single.Parse(xpos.text);
			float y = Single.Parse(ypos.text);
			float z = Single.Parse(zpos.text);
			return new Vector3(x, y, z);
		}
	}
	
	/// <summary> The rotation, that has been put in the inspector </summary>
	public Vector3 InputRotation {
		get {
			float x = (xrot.value - .5f) * 360;
			float y = (yrot.value - .5f) * 360;
			float z = (zrot.value - .5f) * 360;
			return new Vector3(x, y, z);
		}
	}
	
	/// <summary> The velocity, that has been put in the inspector </summary>
	public Vector3 InputVelocity {
		get {
			float x = Single.Parse(vel_x.text);
			float y = Single.Parse(vel_y.text);
			float z = Single.Parse(vel_z.text);
			return new Vector3(x, y, z);
		}
	}
	
	/// <summary> The angular velocity, that has been put in the inspector </summary>
	public Vector3 InputAngularVelocity {
		get {
			float x = Single.Parse(ang_x.text);
			float y = Single.Parse(ang_y.text);
			float z = Single.Parse(ang_z.text);
			return new Vector3(x, y, z);
		}
	}
	#endregion

	private void Start () {
		InputField[] velocity_inps = velocity_par.GetComponentsInChildren<InputField>();
		vel_x = velocity_inps [0];
		vel_y = velocity_inps [1];
		vel_z = velocity_inps [2];
		InputField[] angular_velocity_inps = angular_velocity_par.GetComponentsInChildren<InputField>();
		ang_x = angular_velocity_inps [0];
		ang_y = angular_velocity_inps [1];
		ang_z = angular_velocity_inps [2];
	}

	#region UpdateValues
	/// <summary> Should be called, if the position is changed </summary>
	public void UpdatePos () {
		try {
			EditorGeneral.current_movable.Position = Arrows.UI2Engine(InputPosition);
		} catch (FormatException) {
			EditorGeneral.Throw("Illegal position input");
		} catch (NullReferenceException) {
			EditorGeneral.Throw("Nothing Selected");
		}
	}

	/// <summary> Should be called, if the rotation is changed </summary>
	public void UpdateRot () {
		try {
			EditorGeneral.current_movable.Rotation = Arrows.UI2Engine(InputRotation);
		} catch (FormatException) {
			EditorGeneral.Throw("Illegal rotation input");
		} catch (NullReferenceException) {
			EditorGeneral.Throw("Nothing Selected");
		}
	}

	/// <summary> Should be called, if the velocity is changed </summary>
	public void UpdateVelocity () {
		try {
			EditorGeneral.current_movable.Velocity = Arrows.UI2Engine(InputVelocity);
		} catch (FormatException) {
			EditorGeneral.Throw("Illegal velocity input");
		} catch (NullReferenceException) {
			EditorGeneral.Throw("Nothing Selected");
		}
	}

	/// <summary> Should be called, if the angular velocity is changed </summary>
	public void UpdateAngularVelocity () {
		try {
			EditorGeneral.current_movable.AngularVelocity = Arrows.UI2Engine(InputAngularVelocity);
		} catch (FormatException) {
			EditorGeneral.Throw("Illegal angular velocity input");
		} catch (NullReferenceException) {
			EditorGeneral.Throw("Nothing Selected");
		}
	}
	#endregion

	#region UpdateFields
	/// <summary> Update the UI for the position </summary>
	public void UpdatePositionFileds () {
		if (EditorGeneral.current_movable == null) return;
		Vector3 position = Arrows.Engine2UI(EditorGeneral.current_movable.Position);
		xpos.text = position.x.ToString();
		ypos.text = position.y.ToString();
		zpos.text = position.z.ToString();
	}

	/// <summary> Update the UI for rotation </summary>
	public void UpdateRotationFields () {
		Vector3 rotation = Arrows.Engine2UI(EditorGeneral.current_movable.Rotation);
		xrot.value = rotation.x / 360 + .5f;
		yrot.value = rotation.y / 360 + .5f;
		zrot.value = rotation.z / 360 + .5f;
	}

	/// <summary> Update the UI for the rotation and the angular rotation </summary>
	public void UpdateVelocities () {
		Vector3 velocity = Arrows.Engine2UI(EditorGeneral.current_movable.Velocity);
		vel_x.text = velocity.x.ToString();
		vel_y.text = velocity.y.ToString();
		vel_z.text = velocity.z.ToString();

		Vector3 angular = Arrows.Engine2UI(EditorGeneral.current_movable.AngularVelocity);
		ang_x.text = angular.x.ToString();
		ang_y.text = angular.y.ToString();
		ang_z.text = angular.z.ToString();
	}
	#endregion

	/// <summary> Should be called, if the delete button is pressed </summary>
	public void Delete () {
		EditorGeneral.arrows.transform.SetParent(null, true);
		EditorGeneral.arrows.parent = null;
		EditorGeneral.active.RemoveMovables(EditorGeneral.current_movable);
		Destroy(EditorGeneral.current_movable.gameObject);
	}
}
