using UnityEngine;
using System.Collections.Generic;
using FileManagement;

/* ===================================
 * Physics engine and Fundament of all
 * other objects
 * =================================== */

/// <summary> Things, that can get aimed at (only need position) </summary>
public interface IAimable
{
	/// <summary> The position of the Object in Space </summary>
	Vector3 Position { get; }
	bool Exists { get; }
	bool Friendly { get; }
}

public interface ISavable
{
	string Name { get; }
	DataStructure Save (DataStructure ds);
}

/// <summary> All Object affected by the in-game pure-newtonian physics engine </summary>
public interface IPhysicsObject : IAimable
{
	/// <summary> Velocity as an 3D-Vector (m*s^-1) </summary>
	Vector3 Velocity { get; set; }
	/// <summary> Orientation in the form of a Quaternion number </summary>
	Quaternion Orientation { get; set; }
	/// <summary> Angular velocity as an 3D-Vector of axis (°*s^-1) </summary>
	Vector3 AngularVelocity { get; set; }

	/// <summary> Mass of the object (kg) </summary>
	double Mass { get; }

	/// <summary> Pushes the object, vhanges velocity, but not angular velocity </summary>
	/// <param name="force"> Force applied in N </param>
	void Push (Vector3 force);
	void Torque (Vector3 force);
	void PhysicsUpdate (float deltatime);
}

public interface IMarkerParentObject : IAimable {
	string Name { get; }
	MapTgtMarker Marker { get; set; }
}

/// <summary> Things, one can target </summary>
public interface ITargetable {
	Target Associated { get; }
}

public abstract class SceneObject : 
IPhysicsObject, ISavable
{
	public int ID { get; set; }
	public static SortedDictionary<int, SceneObject> TotObjectList = new SortedDictionary<int, SceneObject>();

	public bool is_none = false;

	public abstract string Name { get; protected set; }

	public static float deltatime;
	public SceneObjectType sceneObjectType;

	public GameObject Object { get; protected set; }
	public Transform Transform {
		get { return Object.transform; }
	}
	public ArrowIndicator arrows;

	protected MissingReferenceException NotExist {
		get { return new MissingReferenceException(string.Format("SceneObject \"{0}\" does not exist", Name)); }
	}

	public bool Friendly { get; set; }

	/// <summary>
	///		The Importance of the object (more important objects get taken more seriousely
	/// </summary>
	public abstract float Importance { get; }

	#region Physics
	public Vector3 Position {
		get {
			if (!Exists) throw NotExist;
			return Transform.position;
		}
		set {
			if (!Exists) throw NotExist;
			Transform.position = value;
		}
	}

	public Vector3 Velocity { get; set; }

	/// <summary> Acceleration as a 3D-Vector (m*s^-2) </summary>
	public Vector3 Acceleration { get; set; }

	public Quaternion Orientation {
		get {
			if (!Exists) {
				throw NotExist;
			}
			return Transform.rotation;
		}
		set {
			if (!Exists) {
				throw NotExist;
			}
			Transform.rotation = value;
		}
	}

	/// <summary> Angular velocity as an 3D-Vector of axis (°*s^-1) </summary>
	public Vector3 AngularVelocity { get; set; }

	/// <summary> Angular velocity as an 3D-Vector of axis (°*s^-2) </summary>
	public Vector3 AngularAcceleration { get; set; }

	public abstract double Mass { get; protected set; }

	public virtual void PhysicsUpdate (float p_deltatime) {
		deltatime = p_deltatime;

		Velocity += Acceleration * deltatime;
		Position += SceneGlobals.ReferenceSystem.RelativeVelocity(Velocity) * deltatime;

		AngularVelocity += AngularAcceleration * deltatime;
		try {
			Orientation *= Quaternion.Euler(AngularVelocity * deltatime);
		} catch (System.Exception) {
			DeveloppmentTools.Log("Angular Velocity: " + AngularVelocity);
		}
	}

	/// <summary> Pushes the object </summary>
	/// <param name="force"> Force in kN </param>
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

	public SceneObject (SceneObjectType obj_type, int id=-1) {
		SceneGlobals.physics_objects.Add(this);
		sceneObjectType = obj_type;

		if (id < 0) {
			ID = 3;
			// Attention to while loop
			while (TotObjectList.ContainsKey(ID)) ID++;
			TotObjectList.Add(ID, this);
		} else {
			ID = id;
			TotObjectList [id] = this;
		}
	}

	public virtual DataStructure Save (DataStructure ds) {
		ds.Set("position", Position);
		ds.Set("velocity", Velocity);
		ds.Set("orientation", Orientation);
		ds.Set("angular velocity", AngularVelocity);
		ds.Set("id", ID);
		return ds;
	}

	protected void InitializeArrows (Vector3 offset) { 
		if (Exists) {
			arrows = ArrowIndicator.GetArrowIndicator(Transform.position + Transform.rotation * offset, Vector3.forward, .5f);
			arrows.transform.SetParent(SceneGlobals.map_canvas.transform);
			arrows.parent_object = this;
		}
	}

	public override string ToString () {
		return Name;
	}
}

public class Target : SceneObject,
IMarkerParentObject, ITargetable
{
	private Ship _ship;
	public Ship Ship {
		get {
			if (virt_ship) return null;
			return _ship;
		}
		set {
			if (virt_ship) {
				DeveloppmentTools.Log("Targets of virtual ships cannot be set");
			} else _ship = value;
		}
	}

	public Target Associated { get { return this; } }
	public MapTgtMarker Marker { get; set; }

	protected string _name;
	public override string Name {
		get {
			if (_name == null) return string.Empty;
			return _name;
		}
		protected set {	_name = value; }
	}

	new public Vector3 Position {
		get {
			if (Exists) {
				if (virt_ship) return Object.transform.position;
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
			if (is_none) return false;
			return base.Exists;
		}
	}

	public bool virt_ship = true;

	private readonly float importance = 0;

	public static readonly Target None = new Target(GameObject.Find("Placeholder"), 0, true, true, 0);

	public override float Importance {
		get {
			if (is_none) return 0;
			if (!virt_ship) return Ship.Importance;
			return importance;
		}
	}

	private double _mass;
	public override double Mass {
		get {
			if (virt_ship) return _mass;
			return _ship.Mass;
		}
		protected set {
			throw new System.NotImplementedException();
		}
	}

	public Target(GameObject objct, double mass, bool has_no_ship=false, bool pnone=false, int id=-1) : base(SceneObjectType.target, id){
		Friendly = false;
		_mass = mass;
		is_none = pnone;
		if (!has_no_ship){
			Ship = objct.GetComponent<ShipControl>().myship;
			Friendly = Ship.Friendly;
			virt_ship = false;
		}
		Object = objct;
		if (!is_none) InitializeArrows(Vector3.zero);
		if (!is_none) MapCore.Active.ObjectSpawned(this);

		if (is_none) Name = "<NULL Target>";
		// Provisory
		else Name = Object.name;
	}

	public Target(Ship ship, int id=-1) : base(SceneObjectType.target, id){
		Friendly = ship.Friendly;
		virt_ship = false;
		Ship = ship;
		Object = ship.Object;
		Name = ship.Name != null ? ship.Name : ship.Object.name;
	}

	public void OnDestroy () { }

	public override DataStructure Save (DataStructure ds) {
		ds.Set("mass", Mass);
		return base.Save(ds);
	}

	public override string ToString () {
		return string.Format("<Target {0}>", Name);
	}
}

public abstract class DestroyableObject
{
	public bool Friendly = false;

	protected float _hp = float.MaxValue;
	public float HP {
		get { return _hp; }
		set {
			_hp = value;
			if (_hp < 0) Destroy();
		}
	}

	private GameObject _own_object;
	public GameObject OwnObject { get; protected set; }
	public MapTgtMarker Marker { get; set; }

	public bool Exists {
		get { return OwnObject; }
	}

	/// <summary> If hit by a bullet </summary>
	/// <param name="hit"> The bullet </param>
	public virtual void Hit (Bullet hit) {
		float dammage = Bullet.KineticDammage(hit, Vector3.zero, Friendly);
		HP -= dammage;
		hit.Destroy();
		hit.Explode();
	}

	/// <summary> If hit by a bullet </summary>
	/// <param name="hit"> The bullet </param>
	public virtual void Hit (Missile hit) {
		float dammage = hit.Dammage(Friendly);
		HP -= dammage;
		hit.Destroy();
		hit.Explode();
	}

	public virtual void Destroy () {
		Object.Destroy(OwnObject);
	}
}

public class DestroyableTarget : DestroyableObject,
IPhysicsObject, IMarkerParentObject, ITargetable, ISavable
{

	#region target inheritance
	public Vector3 Position {
		get { return Associated.Position; }
		set { Associated.Position = value; }
	}

	public Vector3 Velocity {
		get { return Associated.Velocity; }
		set { Associated.Velocity = value;  }
	}

	public Quaternion Orientation {
		get { return Associated.Orientation; }
		set { Associated.Orientation = value; }
	}

	public Vector3 AngularVelocity {
		get { return Associated.AngularVelocity; }
		set { Associated.AngularVelocity = value; }
	}

	public double Mass {
		get { return Associated.Mass; }
		protected set {	throw new System.NotImplementedException(); }
	}

	public void Push (Vector3 force) {
		Associated.Push(force);
	}

	public void Torque (Vector3 force) {
		Associated.Torque(force);
	}

	public void PhysicsUpdate (float delta_time) {
		Associated.PhysicsUpdate(delta_time);
	}

	public DataStructure Save (DataStructure ds) {
		ds.Set("type", 3);
		ds.Set("hp", HP);
		ds.Set("pref", prefab);
		return Associated.Save(ds);
	}
	#endregion

	public Target Associated { get; set; }

	public string Name {
		get { return Associated.Name;  }
	}

	public override void Destroy () {
		Associated.OnDestroy();
		base.Destroy();
	}

	public new bool Friendly {
		get { return base.Friendly; }
	}

	private DSPrefab prefab;

	public DestroyableTarget (float hp, Target target, DSPrefab p_prefab) {
		HP = hp;
		Associated = target;
		base.Friendly = target.Friendly;
		OwnObject = target.Object;

		prefab = p_prefab;

		TgtMarker.Instantiate(this, 1);
		SceneGlobals.physics_objects.Add(this);
		SceneGlobals.destroyables.Add(this);
	}

	public static DestroyableTarget Load (DataStructure data) {
		DSPrefab source = data.Get<DSPrefab>("pref");
		GameObject obj = Object.Instantiate(source.obj, data.Get<Vector3>("position"), data.Get<Quaternion>("orientation"));

		Target tgt = new Target(obj, 0, has_no_ship: true, id: data.Get<int>("id"));
		DestroyableTarget res = new DestroyableTarget(data.Get("hp", 1f), tgt, source);

		res.Velocity = data.Get<Vector3>("velocity");
		res.AngularVelocity = data.Get<Vector3>("angular velocity");

		return res;
	}
}

public struct PrecisePosition : IAimable
{
	public Vector3 Offset { get; set; }
	public IAimable Parent { get; set; }

	public Vector3 Position {
		get { return Parent.Position + Offset; }
	}

	public bool Exists {
		get { return Parent.Exists; }
	}

	public bool Friendly {
		get { return Parent.Friendly; }
	}

	public PrecisePosition (IAimable parent, Vector3 offset) {
		Parent = parent;
		Offset = offset;
	}
}

/// <summary>
///		A specific point relative to a physics object
/// </summary>
public struct PhysicsOffsetAim: IAimable
{
	public IPhysicsObject ParentPhysicsObject { get; set; }

	public Vector3 offset_position;

	public Vector3 Position {
		get { return ParentPhysicsObject.Orientation * -offset_position + ParentPhysicsObject.Position; }
	}

	public bool Exists {
		get { return ParentPhysicsObject.Exists; }
	}

	public bool Friendly {
		get { return ParentPhysicsObject.Friendly; }
	}

	public PhysicsOffsetAim (Vector3 p_position, IPhysicsObject p_part) {
		ParentPhysicsObject = p_part;
		offset_position = Quaternion.Inverse(p_part.Orientation) * (p_part.Position - p_position);
	}
}