using UnityEngine;

/* ========================================================
 * Ment to be attached to GameObject, which represent hulls
 * ======================================================== */

public class HullAttachment : MonoBehaviour {

	public Hull instance;
	private float timer = 0f;
	private const float lifespan = 6f;

	private void Start () {
		if (instance.objct == null) {
			instance.objct = gameObject;
		}
	}

	private void Update () {
		if (SceneGlobals.Paused) goto PAUSEDRUNNTIME;
		if (timer > lifespan)
			Destroy(gameObject);

		timer += Time.deltaTime;
		instance.PhysicsUpdate(Time.deltaTime);

		PAUSEDRUNNTIME:;
	}
}
