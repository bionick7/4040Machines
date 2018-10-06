using UnityEngine;
using System.Collections.Generic;

public class PlayerControl : MonoBehaviour {

	public Vector3[] positions;
	public Quaternion[] rotations;
	public bool[] free_rotation;

	private ShipControl control_script;
	private GUIScript UIScript;

	private AudioSource audio_src;

	private AudioClip camera_switch;

	private int act_cam_num = 0;
	private float act_thrust;
	private float act_angular;

	private Camera cam;
	private Ship ship;

	private MapCameraMouvement mapcam_mv;

	private void Start () {
		camera_switch = Resources.Load<AudioClip>("sounds/camera_switch");
		audio_src = GetComponent<AudioSource>();

		control_script = GetComponent<ShipControl>();
		UIScript = SceneData.ui_script;

		ship = control_script.myship;
		cam = GameObject.Find("ShipCamera").GetComponent<Camera>();
		mapcam_mv = SceneData.map_camera.GetComponent<MapCameraMouvement>();

		act_thrust = 1;
		act_angular = 1;

		Switch_Camera(act_cam_num);
	}

	public void FineTune(float multiplyer) {
		act_thrust = multiplyer;
		act_angular = multiplyer;
	}

	public void Switch_Camera (int cam_setting){
		cam.transform.SetParent(null);
		for (int i = 0; i < positions.Length; i++) {
			if (i == cam_setting) {
				cam.transform.position = ship.Position + (transform.rotation * positions [i]);
				cam.transform.rotation = transform.rotation * rotations [i];
				cam.GetComponent<CameraMovement>().FreeRotation = free_rotation[i];
			}
		}
		cam.transform.SetParent(transform, true);
	}

	private void Steering () {
		float x_force=0, y_force=0, z_force=0, pitch_force=0, yaw_force=0, roll_force=0;
		
		if (Input.GetKey(KeyCode.A)){
			pitch_force = -act_angular;
		}
		if (Input.GetKey(KeyCode.D)){
			pitch_force = act_angular;
		}
		if (Input.GetKey(KeyCode.W)){
			yaw_force = act_angular;
		}
		if (Input.GetKey(KeyCode.S)){
			yaw_force = -act_angular;
		}
		if (Input.GetKey(KeyCode.Q)){
			roll_force = act_angular;
		}
		if (Input.GetKey(KeyCode.E)){
			roll_force = -act_angular;
		}

		control_script.inp_torque_vec.Set(yaw_force, pitch_force, roll_force);
		
		if (Input.GetKey(KeyCode.Keypad4)){
			x_force = -act_thrust;
		}
		if (Input.GetKey(KeyCode.Keypad6)){
			x_force = act_thrust;
		}
		if (Input.GetKey(KeyCode.Keypad9)){
			z_force = act_thrust;
		}
		if (Input.GetKey(KeyCode.Keypad3)){
			z_force = -act_thrust;
		}
		if (Input.GetKey(KeyCode.Keypad8)){
			y_force = act_thrust;
		}
		if (Input.GetKey(KeyCode.Keypad2)){
			y_force = -act_thrust;
		}
		control_script.inp_thrust_vec.Set(x_force, y_force, z_force);

		if (Input.GetKey(KeyCode.Keypad7)){
			ship.Throttle = Mathf.Min(1, ship.Throttle + .01f);
		}
		if (Input.GetKey(KeyCode.Keypad1)){
			ship.Throttle = Mathf.Max(0, ship.Throttle - .01f);
		}
		if (Input.GetKey(KeyCode.Keypad5)) {
			control_script.KillVelocity();
		}
		if (Input.GetKey(KeyCode.KeypadPlus)){
			ship.Throttle = 1;
		}
		if (Input.GetKey(KeyCode.KeypadMinus)){
			ship.Throttle = 0;
		}
	}

	private void Actions() {
		if (Input.GetKeyDown(KeyCode.Space)) {
			control_script.Trigger_Shooting(true);
			foreach (TurretGroup tg in control_script.turret_aims) {
				tg.ShootSafe();
			}
		}

		if (Input.GetKeyUp(KeyCode.Space)) {
			control_script.Trigger_Shooting(false);
		}

		if (Input.GetKeyDown(KeyCode.C)) {
			audio_src.PlayOneShot(camera_switch);
			bool is_shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
			if (!is_shift) {
				act_cam_num = (act_cam_num + 1) % positions.Length;
			}
			Switch_Camera(act_cam_num);
		}

		if (Input.GetKeyDown(KeyCode.RightAlt)) {
			control_script.FireMissile();
		}

		if (Input.GetKeyDown(KeyCode.KeypadEnter)) {
			UIScript.group_follows_cursor = false;
		}

		if (Input.GetMouseButtonDown(0) && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))) {
			UIScript.SetDirection();
		}
	}

	private void CameraMouvement () {
		Vector3 mouvement = Vector3.zero;
		if (Input.GetKey(KeyCode.LeftArrow)) mouvement.x -= 1;
		if (Input.GetKey(KeyCode.RightArrow)) mouvement.x += 1;
		if (Input.GetKey(KeyCode.DownArrow)) mouvement.z -= 1;
		if (Input.GetKey(KeyCode.UpArrow)) mouvement.z += 1;
		if (Input.GetKey(KeyCode.PageUp)) mouvement.y += 1;
		if (Input.GetKey(KeyCode.PageDown)) mouvement.y -= 1;
		mapcam_mv.TunePivotPoint(mouvement);

	}

	private void Update () {
		if (Input.GetKeyDown(KeyCode.Escape)) {
			UIScript.ToggleMenu();
		}
		if (Input.GetKeyDown(KeyCode.P)) {
			UIScript.Paused = !UIScript.Paused;
		}
		if (Input.GetKeyDown(KeyCode.M)) {
			SceneData.general.InMap = !SceneData.general.InMap;
		}
		if (!SceneData.general.InMap) {
			if (!UIScript.Paused && !SceneData.in_console) {
				Actions();
			}
		} else {
			CameraMouvement();
		}
	}

	private void FixedUpdate () {
		if (!UIScript.Paused && !SceneData.in_console) {
			Steering();
		}
	}

	private void OnDestroy () {
		Debug.Log("Game Over");
		Data.persistend.EndBattle(false);
	}

	private void OnGUI () {
		GUI.Label(new Rect(0, 0, 100, 100), (1f / Time.deltaTime).ToString());
	}
}