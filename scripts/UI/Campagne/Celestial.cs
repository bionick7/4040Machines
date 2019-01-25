using UnityEngine;
using System.Collections.Generic;
using FileManagement;

public class Celestial : MonoBehaviour {
	public Vector3 orbit_plane = Vector3.up;
	public bool is_static = false;

	public float mass;
	public float radius = 0;

	public Celestial parent_celestial = null;
	public float angular_velocity;
	public bool mouse_hover;
	public bool in_focus;
	public float click_distance = float.PositiveInfinity;

	private BoxCollider plane_collider;
	public CelestialData data;

	public List<Celestial> satellites = new List<Celestial>();

	public float Mass {
		get { return data.mass; }
	}

	public float Radius {
		get { return data.radius; }
	}

	public float OrbitalRadius { get; private set; }

	private Vector3 ParentPosition {
		get {
			if (parent_celestial == null)
				return Vector3.zero;
			return parent_celestial.transform.position;
		}
	}

	private void Start () {
		CampagneManager.celestials.Add(this);
		ChapterUpdate();
		if (!is_static) {
			parent_celestial = transform.parent.GetComponent<Celestial>();
			parent_celestial.satellites.Add(this);
			OrbitalRadius = Vector3.Distance(transform.position, ParentPosition);
			angular_velocity = Mathf.PI * 2 / Mathf.Sqrt(OrbitalRadius * OrbitalRadius * OrbitalRadius) * parent_celestial.mass;
			transform.RotateAround(ParentPosition, Vector3.Cross(Vector3.up, orbit_plane), Vector3.Angle(Vector3.up, orbit_plane));

			transform.RotateAround(ParentPosition, orbit_plane, Random.Range(0, 360));

			// Make Collider
			GameObject collider_object = new GameObject("Plane Collider");
			collider_object.transform.up = orbit_plane;
			collider_object.transform.SetParent(transform.parent);
			collider_object.transform.localPosition = Vector3.zero;

			plane_collider = collider_object.AddComponent<BoxCollider>();
			plane_collider.size = new Vector3(OrbitalRadius * 3, .01f, OrbitalRadius * 3);
			plane_collider.isTrigger = true;
		}
	}

	public void ChapterUpdate () {
		if (Globals.planet_information.ContainsChild(name)) {
			DataStructure celestial_ds = Globals.planet_information.GetChild(name);
			data = new CelestialData(
				name,
				celestial_ds.Get<float>("mass"),
				celestial_ds.Get("radius", transform.lossyScale.x, quiet: true),
				celestial_ds.Get<string>("description"),
				CampagneManager.battle_data.ContainsKey(name) ? CampagneManager.battle_data[name] : null
			);
		} else {
			data = CelestialData.None;
		}
	}

	private void Update () {
		if (!is_static) {
			transform.RotateAround(ParentPosition, orbit_plane, angular_velocity * Time.deltaTime);
			CampagneManager.drawer.Draw(new Polygon(OrbitalRadius, 128, ParentPosition, orbit_plane), 
				(in_focus | satellites.Exists(x => x.in_focus)) ? 0 : Radius * (mouse_hover ? .7f : .2f));
			CheckMousePos();
			if (CampagneManager.planet_view != data) in_focus = false;
		}
	}

	private void CheckMousePos () {
		RaycastHit hit;
		mouse_hover = false;
		if (plane_collider.Raycast(CampagneManager.cam.camera_inst.ScreenPointToRay(Input.mousePosition), out hit, 10000)) {
			click_distance = Mathf.Abs(Vector3.Distance(hit.point, ParentPosition) - OrbitalRadius);
			if (click_distance < OrbitalRadius * .1f) {
				mouse_hover = true;
			}
		}
	}
}

public struct CelestialData
{
	public string name;
	public float mass;
	public float radius;
	public string description;
	public ChapterBattle[] battles;

	public bool none;

	public CelestialData (string p_name, float p_mass, float p_radius, string p_description, List<ChapterBattle> p_engagable=null, bool is_none=false) {
		name = p_name;
		mass = p_mass;
		radius = p_radius;
		description = p_description;
		battles = p_engagable == null ? new ChapterBattle[0] : p_engagable.ToArray();
		none = is_none;
	}

	public static readonly CelestialData None = new CelestialData("NULL", 0, 0, "", is_none: true);

	public static bool operator == (CelestialData lhs, CelestialData rhs) {
		return lhs.name == rhs.name;
	}

	public static bool operator != (CelestialData lhs, CelestialData rhs) {
		return lhs.name != rhs.name;
	}
}