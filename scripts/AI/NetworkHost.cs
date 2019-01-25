/* =============================================================================
 * Use this to attach a network to a GameObject
 * If the Object is destroyable and gets destroyed, the Network will get deleted
 * ============================================================================= */

public class NetworkHost : UnityEngine.MonoBehaviour {

	public uint fleet_size;
	public bool is_friendly;
	public string name;

	public Network Net = null;

	private void Start () {
		if (Net == null) {
			Net = new Network(fleet_size, is_friendly, name);
		} else {
			fleet_size = Net.fleetsize;
			is_friendly = Net.Friendly;
			name = Net.Name;
		}
	}
}