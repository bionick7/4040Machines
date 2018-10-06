public class NetworkHost : UnityEngine.MonoBehaviour {

	public uint fleet_size;
	public bool side;

	public Network Net = null;

	void Start () {
		if (Net == null) {
			Net = new Network(fleet_size, side);
		} else {
			fleet_size = Net.fleetsize;
			side = Net.side;
		}
	}
}