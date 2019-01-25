using UnityEngine;

/* =================================================================
 * This is the script, where the inputs are processed. 
 * Until now, the inputs are predefined
 * ================================================================= */

public class PlayerControl : MonoBehaviour {

	public Vector3[] positions;
	public Quaternion[] rotations;
	public bool[] free_rotation;

	private ShipControl control_script;
	private GUIScript UIScript;

	private int act_cam_num = 0;
	private float act_thrust;
	private float act_angular;

	private Camera cam;
	private Ship ship;

	private MapCameraMouvement mapcam_mv;
	private KeyBindingCollection keys;

	private void Start () {
		control_script = GetComponent<ShipControl>();
		UIScript = SceneGlobals.ui_script;

		ship = control_script.myship;
		cam = GameObject.Find("ShipCamera").GetComponent<Camera>();
		mapcam_mv = SceneGlobals.map_camera.GetComponent<MapCameraMouvement>();

		act_thrust = 1;
		act_angular = 1;

		Switch_Camera(act_cam_num);
		keys = Globals.bindings;
	}

	/// <summary> Fine tune the rcs engines, s that they don't have to run on full throttle </summary>
	/// <param name="multiplyer"> The multiplyer, to regulate the RCS. must be between 0 and 1 </param>
	public void FineTune(float multiplyer) {
		act_thrust = multiplyer;
		act_angular = multiplyer;
	}

	/// <summary> Switches the camera in Shipview. </summary>
	/// <param name="cam_setting"> The cameras are labelled from 0 to some number. Indicate the label </param>
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

	/// <summary> Steering functions </summary>
	private void Steering () {
		float x_force=0, y_force=0, z_force=0, pitch_force=0, yaw_force=0, roll_force=0;
		
		// Rotation
		if (keys.pitch_down.IsPressed()){
			pitch_force = act_angular;
		}
		if (keys.pitch_up.IsPressed()){
			pitch_force = -act_angular;
		}
		if (keys.yaw_right.IsPressed()){
			yaw_force = act_angular;
		}
		if (keys.yaw_left.IsPressed()){
			yaw_force = -act_angular;
		}
		if (keys.roll_right.IsPressed()){
			roll_force = -act_angular;
		}
		if (keys.roll_left.IsPressed()){
			roll_force = act_angular;
		}

		control_script.inp_torque_vec = new Vector3(pitch_force, yaw_force, roll_force);
		control_script.torque_player = new Vector3(pitch_force, yaw_force, roll_force) != Vector3.zero;
		
		// Translation
		if (keys.translate_left.IsPressed()){
			x_force = -act_thrust;
		}
		if (keys.translate_right.IsPressed()){
			x_force = act_thrust;
		}
		if (keys.translate_fore.IsPressed()){
			z_force = act_thrust;
		}
		if (keys.translate_back.IsPressed()){
			z_force = -act_thrust;
		}
		if (keys.translate_up.IsPressed()){
			y_force = act_thrust;
		}
		if (keys.translate_down.IsPressed()){
			y_force = -act_thrust;
		}

		control_script.inp_thrust_vec.Set(x_force, y_force, z_force);
		control_script.rcs_thrust_player = new Vector3(x_force, y_force, z_force) != Vector3.zero;

		// Engine thrust
		if (keys.increase_throttle.IsPressed()){
			ship.Throttle = Mathf.Min(1, ship.Throttle + .01f);
			ship.control_script.engine_thrust_player = true;
		}
		if (keys.decrease_throttle.IsPressed()){
			ship.Throttle = Mathf.Max(0, ship.Throttle - .01f);
			ship.control_script.engine_thrust_player = ship.Throttle > 0;
		}
		if (keys.kill_rotation.IsPressed()) {
			control_script.KillVelocity();
			control_script.torque_player = true;
		}
		if (keys.throttle_max.IsPressed()){
			ship.control_script.engine_thrust_player = true;
			ship.Throttle = 1;
		}
		if (keys.throttle_min.IsPressed()){
			ship.control_script.engine_thrust_player = false;
			ship.Throttle = 0;
		}
	}

	/// <summary> Miscellangelous actions </summary>
	private void Actions () {
		// Shooting
		if (keys.shoot.ISPressedDown()) {
			control_script.Trigger_Shooting(true);
			foreach (TurretGroup tg in control_script.turretgroup_list) {
				tg.ShootSafe();
			}
		}

		if (keys.shoot.ISUnPressed()) {
			control_script.Trigger_Shooting(false);
		}

		// Fire missile
		if (keys.fire_missile.ISPressedDown()) {
			control_script.FireMissile();
		}
	}

	/// <summary> Actions, that can be executed, while the game is paused </summary>
	private void PauseActions () {
		// Stop mouse following
		if (keys.cancel_mouse_following.IsPressed()) {
			UIScript.group_follows_cursor = false;
		}

		// Set dircetion via mouse
		if (Input.GetMouseButtonDown(0) && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))) {
			UIScript.SetDirection();
		}
		
		// Camera
		if (keys.camera_switch.ISPressedDown()) {
			Globals.audio.UIPlay(UISound.camera_switch);
			bool is_shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
			if (!is_shift) {
				act_cam_num = (act_cam_num + 1) % positions.Length;
			}
			Switch_Camera(act_cam_num);
		}

		// Menu
		if (keys.menu.ISPressedDown()) {
			UIScript.ToggleMenu();
		}

		// Pause
		if (keys.pause.ISPressedDown()) {
			UIScript.Paused = !UIScript.Paused;
		}

		// Toggle map
		if (keys.togglemap.ISPressedDown()) {
			SceneGlobals.general.InMap = !SceneGlobals.general.InMap;
		}
	}

	/// <summary> The camera mouvement in map view </summary>
	private void CameraMouvement () {
		if (SceneGlobals.in_console | !SceneGlobals.general.InMap) return;
		Vector3 mouvement = Vector3.zero;
		if (keys.map_move_left.IsPressed()) mouvement.x -= 1;
		if (keys.map_move_right.IsPressed()) mouvement.x += 1;
		if (keys.map_move_back.IsPressed()) mouvement.z -= 1;
		if (keys.map_move_fore.IsPressed()) mouvement.z += 1;
		if (keys.map_move_up.IsPressed()) mouvement.y += 1;
		if (keys.map_move_down.IsPressed()) mouvement.y -= 1;
		if (mouvement != Vector3.zero)
			mapcam_mv.TunePivotPoint(mouvement);
	}

	private void Update () {
		if (!SceneGlobals.in_console) {
			if (!UIScript.Paused) Actions();
			PauseActions();
		}
		if (SceneGlobals.general.InMap) {
			CameraMouvement();
		}
	}

	private void FixedUpdate () {
		if (!UIScript.Paused && !SceneGlobals.in_console) {
			Steering();
		}
	}
}