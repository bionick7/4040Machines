using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/* ==========================================
 * 
 * OBSOLETE
 * 
 * =========================================== */


public class AimingPointer : MonoBehaviour {

	public float speed = 1; // m/s

	private SpriteRenderer sprite_renderer;
	private Transform camera_transform;

	private bool direction;
	private float currentoffset = 2;

	private IAimable PlayerAim {
		get {
			if (SceneGlobals.Player == null) return null;
			return SceneGlobals.Player.TurretAim;
		}
	}

	void Start () {
		sprite_renderer = GetComponent<SpriteRenderer>();
		camera_transform = SceneGlobals.map_camera.transform;
	}
	
	// Update is called once per frame
	void Update () {
		if (!PlayerAim.Exists | !SceneGlobals.general.InMap) {
			sprite_renderer.enabled = false;
			return;
		}

		sprite_renderer.enabled = true;
		transform.rotation = camera_transform.rotation;
		currentoffset += speed * (direction ? 1 : -1) * Time.deltaTime;
		if ((currentoffset > 3  & direction) || (currentoffset < 2 & !direction)) {
			direction = !direction;
		}
		transform.position = PlayerAim.Position + camera_transform.up * currentoffset;
	}
}
