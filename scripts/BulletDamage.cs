using UnityEngine;

public class BulletDamage : MonoBehaviour {

	public Bullet instance;

	private int countID;
	private float lifespan = 0f;
	private float speed;

	public Ray Infront {
		get {
			return new Ray(transform.position, instance.Velocity);
		}
	}

	private void Start () {
		if (instance.objct == null) {
			instance.objct = gameObject;
		}
		countID = Random.Range(0, 60);

		speed = instance.Velocity.magnitude;

		UpdateTgts();
	}

	private void UpdateTgts () {
		byte ship_counter = 0x00;
		foreach (Ship s in SceneData.ship_list) {
			Vector3 d_vec = s.Position - transform.position;
			float dot = Vector3.Dot(d_vec, instance.Velocity);
			if (dot > 0) {
				float dist_rel_sqr = (d_vec.sqrMagnitude - dot * dot / instance.Velocity.sqrMagnitude) / (s.radius * s.radius);
				if (dist_rel_sqr < 25) {
					ship_counter++;
				}
			}
		}

		if (lifespan > 3 && ship_counter == 0) {
			instance.Destroy();
		}
	}

	private void Update () {
		instance.Update();
		if (Time.frameCount % 60 == countID) {
			UpdateTgts();
		}

		RaycastHit hit;
		if (Physics.Raycast(Infront, out hit, speed * Time.deltaTime, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide)) {
			BulletCollisionDetection behaviour = hit.collider.GetComponent<BulletCollisionDetection>();
			if (behaviour != null) {
				behaviour.Collide(instance);
				instance.Explode();
				instance.Destroy();
			}
		}

		lifespan += Time.deltaTime;

		if (lifespan > 20) {
			Destroy(gameObject);
		}
	}
}
