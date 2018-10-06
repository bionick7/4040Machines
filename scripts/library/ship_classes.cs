using System.Collections.Generic;
using UnityEngine;

/*==================================
 * Physics engine
 * ================================= */

/// <summary>
///		All Object affected by the in-game pure-newtonian physics engine
/// </summary>
public interface IPhysicsObject
{
	/// <summary> The position of the Object in Space </summary>
	Vector3 Position { get; set; }
	/// <summary> Velocity as an 3D-Vector (m*s^-1) </summary>
	Vector3 Velocity { get; set; }
	/// <summary> Orientation in the form of a Quaternion number </summary>
	Quaternion Orientation { get; set; }
	/// <summary> Angular velocity as an 3D-Vector of axis (°*s^-1) </summary>
	Vector3 AngularVelocity { get; set; }

	/// <summary> Mass of the object (kg) </summary>
	double Mass { get; set; }

	/// <summary> Pushes the object, vhanges velocity, but not angular velocity </summary>
	/// <param name="force"> Force applied in N </param>
	void Push (Vector3 force);
	void Torque (Vector3 force);
	void PhysicsUpdate (float deltatime);
}

public abstract class SceneObject : IPhysicsObject
{
	public bool side;
	public bool is_none = false;

	public static float deltatime;
	public SceneObjectType sceneObjectType;

	public GameObject Object { get; protected set; }
	public Transform Transform {
		get { return Object.transform; }
	}

	protected static readonly MissingReferenceException not_exist = new MissingReferenceException ("SceneObject does not exist");

	/// <summary>
	///		The Importance of the object (more important objects get taken more seriousely
	/// </summary>
	public abstract float Importance { get; }

	#region Physics
	public Vector3 Position {
		get {
			if (!Exists) throw not_exist;
			return Transform.position;
		}
		set {
			if (!Exists) throw not_exist;
			Transform.position = value;
		}
	}

	public Vector3 Velocity { get; set; }

	/// <summary> Acceleration as a 3D-Vector (m*s^-2) </summary>
	public Vector3 Acceleration { get; set; }

	public Quaternion Orientation {
		get {
			if (!Exists) {
				throw not_exist;
			}
			return Transform.rotation;
		}
		set {
			if (!Exists) {
				throw not_exist;
			}
			Transform.rotation = value;
		}
	}

	public Vector3 AngularVelocity { get; set; }

	/// <summary> Angular velocity as an 3D-Vector of axis (°*s^-2) </summary>
	public Vector3 AngularAcceleration { get; set; }

	public abstract double Mass { get; set; }

	public void PhysicsUpdate (float p_deltatime) {

		deltatime = p_deltatime;

		Velocity += Acceleration;
		Position += Velocity * deltatime;

		AngularVelocity += AngularAcceleration;
		Orientation *= Quaternion.Euler(AngularVelocity * deltatime);
	}

	public void Push (Vector3 force) {
		Velocity += (force / (float) Mass) * Time.deltaTime;
	}

	public void Torque (Vector3 force) {
		AngularVelocity += (force / (float) Mass) * Time.deltaTime * Mathf.Rad2Deg;
	}
	#endregion

	/// <summary>
	///		Boolean: True, if the object Exists
	/// </summary>
	public bool Exists {
		get { return Object; } 
	}

	public SceneObject (SceneObjectType obj_type) {
		SceneData.physics_objects.Add(this);
		sceneObjectType = obj_type;

		GameObject map_pointer = UnityEngine.Object.Instantiate(GameObject.Find("map_pointer"));
		var img = map_pointer.GetComponent<UnityEngine.UI.Image>();
		img.color = Color.blue;
		map_pointer.transform.SetParent(SceneData.map_canvas.transform);
		MapDrawnObject map_image = SceneData.mapdrawer.AddSingleSprite(map_pointer, this);
	}

	public static GameObject PlayerObj () {
		int players = GameObject.FindGameObjectsWithTag("Player").Length;
		if (players == 1) {
			return GameObject.FindGameObjectWithTag("Player");
		} else if (players == 0) {
			return null;
		} else {
			throw new System.Exception("Only 1 player at a time permittet");
		}
	}

	public override string ToString () {
		return Object.name;
	}
}

public class Target : SceneObject
{
	private Ship _ship;
	public Ship Ship {
		get {
			if (virt_ship) { return null; }
			return _ship;
		}
		set {
			if (virt_ship) {
				throw new System.ArgumentException("Targets of virtual ships cannot be set");
			}
			else {
				_ship = value;
			}
		}
	}

	new public Vector3 Position {
		get {
			if (Exists) {
				if (virt_ship) {
					return Object.transform.position;
				}
					return Ship.Position;
			}
			return Vector3.zero;
		}
		set {
			if (Exists) {
				if (virt_ship) {
					Object.transform.position = value;
				} else {
					Ship.Position = value;
				}
			}
		}
	}

	new public bool Exists {
		get {
			if (is_none) {
				return false;
			}
			return base.Exists;
		}
	}

	public bool virt_ship = true;
	public string name;

	private float importance = 0;

	public static Target None {
		get { return new Target(GameObject.Find("Placeholder"), 0, true) { is_none = true }; }
	}

	public override float Importance {
		get {
			if (is_none) {
				return 0;
			}
			if (!virt_ship) {
				return Ship.Importance;
			}
			return importance;
		}
	}

	private double _mass;
	public override double Mass {
		get {
			if (virt_ship) return _mass;
			return _ship.Mass;
		}
		set {
			if (virt_ship) _mass = value;
		}
	}

	public Target(GameObject objct, double mass, bool has_no_ship=false) : base(SceneObjectType.target){
		side = false;
		_mass = mass;
		if (!has_no_ship){
			Ship = objct.GetComponent<ShipControl>().myship;
			virt_ship = false;
		}
		Object = objct;
		name = objct.name;
	}

	public Target(Ship ship) : base(SceneObjectType.target){
		side = false;
		virt_ship = false;
		Ship = ship;
		Object = ship.Object;
		name = ship.name;
	}
}

public abstract class DestroyableObject
{
	public bool side = false;

	protected float _hp = 0;
	public float HP {
		get {
			return _hp;
		}
		set {
			float act_value = Mathf.Max(value, 0f);
			_hp = act_value;
		}
	}

	public GameObject OwnObject { get; protected set; }

	public bool Exists {
		get { return OwnObject; }
	}

	/// <summary> If hit by a bullet </summary>
	/// <param name="hit"> The bullet </param>
	public void Hit (Bullet hit) {
		float dammage = Bullet.Dammage(hit, Vector3.zero, side);
		HP -= dammage;

		if (HP <= 0f) Destroy();
	}

	/// <summary> If hit by a bullet </summary>
	/// <param name="hit"> The bullet </param>
	public void Hit (Missile hit) {
		Object.Destroy(hit.Object);
		float dammage = hit.Dammage(side);
		HP -= dammage;

		hit.Explode();

		if (HP <= 0f) Destroy();
	}

	public virtual void Destroy () {
		Object.Destroy(OwnObject);
	}
}

public class DestroyableTarget : DestroyableObject, IPhysicsObject
{

	#region target inheritance
	public Vector3 Position {
		get { return Target.Position; }
		set { Target.Position = value; }
	}

	public Vector3 Velocity {
		get { return Target.Position; }
		set { Target.Position = value;  }
	}

	public Quaternion Orientation {
		get { return Target.Orientation; }
		set { Target.Orientation = value; }
	}

	public Vector3 AngularVelocity {
		get { return Target.AngularVelocity; }
		set { Target.AngularVelocity = value; }
	}

	public double Mass {
		get { return Target.Mass; }
		set { Target.Mass = value; }
	}

	public void Push (Vector3 force) {
		Target.Push(force);
	}

	public void Torque (Vector3 force) {
		Target.Torque(force);
	}

	public void PhysicsUpdate (float delta_time) {
		Target.PhysicsUpdate(delta_time);
	}

	#endregion

	public Target Target { get; set; }

	public DestroyableTarget (float hp, Target target) {
		HP = hp;
		Target = target;
		side = target.side;
		OwnObject = target.Object;
	}
}

public enum SceneObjectType
{
	ship,
	target,
	missile,
	network,
	none,
}