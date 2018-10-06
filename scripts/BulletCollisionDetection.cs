using UnityEngine;

// Redundant
/*
 *This script goes on every seperate component of the ship, that should be able to be shot seperatly,
 * be it Engine, fuel tanks, cockpit, docking mechanism, antennae, and stuff.
 * Here, a float, beeing Hitpoints (supposed to start as an integer), a string beeing the function and another float beeing the Explosion force are demanded
 * the function must be one of the following: "main", "weapon" "docking port", "fuel tank", "engine", "rcs tank",
 * "power reciever", "weapon cooling", "life support", "structure", latter dooing nothing. What happens if the component is dammeged/destroyed is
 * decided in "controil_ship2.cs". The exploision force determins the size of the explosion created by the destrucion of the compüonent.
 * This should be big for hypergolic fuel tanks and weaponnery, but 0 for structure or pure hydrogen fuel tanks.
 * The boolean "main_component" determins, if the the GameObject is destroyed as soon as the component's hitpoints go to 0.
 * This is practical, if your Object contains more components.
 */

public class BulletCollisionDetection : MonoBehaviour {

	public float health;
	public float mass;
	public PartsOptions parttype;
	public bool is_main = true;

	public bool is_part = true;

	public ShipPart Part = null;
	public DestroyableObject DestObj;

	public void Initialize () {
		if (is_part) {
			if (Part == null) {
				Part = ShipPart.Get(health, gameObject, parttype, mass);
			} else {
				// Debugging purpuses, can be removed befre compiling
				health = Part.HP;
				mass = Part.Mass;
				parttype = Part.parttype;
			}
			Part.main_component = is_main;
			DestObj = Part;
		}
	}

	public void Collide (Bullet bullet) {
		DestObj.Hit(bullet);
	}
}
