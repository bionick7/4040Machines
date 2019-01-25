using UnityEngine;
using UnityEngine.UI;

/* ==================================================================================
 * This is a kind of bridge between the buttons and the UI script.
 * Because the player object wis not known at first, functions have to go here first.
 * They then trigger the UI script, which is known in this script.
 * Currently stored in "Database"
 * ================================================================================== */

public class ButtonListener : MonoBehaviour {

	private GUIScript script;
	private AddGroupMenu add_menu;
	private ShipControl player_control;

	public Slider arrow_size_slider;

	private void Start () {
		script = SceneGlobals.ui_script;
		add_menu = script.add_group_menu;
		player_control = SceneGlobals.Player.Object.GetComponent<ShipControl>();

		arrow_size_slider = GameObject.Find("arrow_size").GetComponent<Slider>();
	}

	public void GroupSwitch () {
		script.GroupSwitch = !script.GroupSwitch;
		Globals.audio.UIPlay(UISound.soft_click);
	}

	public void TGAimTarget () {
		script.TurretGroupAimTarget();
		Globals.audio.UIPlay(UISound.soft_click);
	}

	public void TGAimMarker () {
		script.TurretGroupAimDirection();
		Globals.audio.UIPlay(UISound.soft_click);
	}

	public void TGAimCursor () {
		script.TurretGroupAimCursor();
		Globals.audio.UIPlay(UISound.soft_click);
	}

	public void TGEdit () {
		script.CallGroupMenu(true);
		Globals.audio.UIPlay(UISound.soft_click);
	}

	public void TGDelete () {
		script.DeleteTurretGroup();
		Globals.audio.UIPlay(UISound.soft_click);
	}

	public void ToGroup () {
		script.CallGroupMenu(false);
		Globals.audio.UIPlay(UISound.soft_click);
	}

	public void SelectAll () {
		SceneGlobals.map_core.selection_viewer.SelectAll();
	}

	public void SelectEnemies () {
		SceneGlobals.map_core.selection_viewer.Selectenemies();

	}

	public void SelectFriends () {
		SceneGlobals.map_core.selection_viewer.SelectFriends();

	}

	public void Command2All () {
		SceneGlobals.map_core.CommandToSelected();
	}

	/// <summary> Changes the purpose of the UI arrows </summary>
	/// <param name="code">
	///		0 -> None
	///		1 -> Velocity
	///		2 -> Acceleration
	/// </param>
	public void ArrowPurpose (int code) {
		switch (code) {
		case 0:
			ArrowIndicator.arrow_usage = ArrowIndicator.ArrowUsage.none;
			break;
		case 1:
			ArrowIndicator.arrow_usage = ArrowIndicator.ArrowUsage.velocity;
			arrow_size_slider.value = Mathf.Sqrt(SceneGlobals.velocity_multiplyer);
			break;
		case 2:
			ArrowIndicator.arrow_usage = ArrowIndicator.ArrowUsage.acceleration;
			arrow_size_slider.value = Mathf.Sqrt(SceneGlobals.acceleration_multiplyer);
			break;
		default:
			break;
		}
	}

	/// <summary> Changes the size of the UI arrows </summary>
	public void UpdateArrowSize () {
		switch (ArrowIndicator.arrow_usage) {
		case ArrowIndicator.ArrowUsage.velocity:
			SceneGlobals.velocity_multiplyer = arrow_size_slider.value * arrow_size_slider.value;
			break;
		case ArrowIndicator.ArrowUsage.acceleration:
			SceneGlobals.acceleration_multiplyer = arrow_size_slider.value * arrow_size_slider.value;
			break;
		default:
		case ArrowIndicator.ArrowUsage.none:
			break;
		}
	}

	/// <summary> Points the ship to a specific direction </summary>
	/// <param name="code">
	///		1 -> velocity +
	///	   -1 -> velocity -
	///		2 ->   target +
	///	   -2 ->   target -
	///	    3 -> target veloxity +
	///	   -3 -> target velocity -
	///		4 -> cancel navigation
	/// </param>
	public void PointTo (int code) {
		player_control.ai_low.Point2Command(code);
	}

	public void EnDisable (bool single_turret) {
		if (single_turret) {
			script.selected_turret.Enabled = !script.selected_turret.Enabled;
		} else {
			script.selected_group.Enabled = !script.selected_group.Enabled;
		}
		Globals.audio.UIPlay(UISound.soft_click);
	}

	/// <summary> Moves the console </summary>
	/// <param name="pos">
	///		If 0, the console is moved to the next state (hidden > lower > shown > hidden)
	///		If 1, the console is hidden
	///		If 2, the console is lowered
	///		If 3, the console is shown
	/// </param>
	public void ToggleConsole (int pos=0) {
		Globals.audio.UIPlay(UISound.soft_click);
		var console = script.console;
		switch (pos) {
		case 0:
			switch (console.ConsolePos) {
			case ConsolePosition.hidden: goto LOWER;
			case ConsolePosition.lower: goto SHOWN;
			default: goto HIDDEN;
			}
		case 1: goto HIDDEN;
		case 2: goto LOWER;
		case 3: goto SHOWN;
		}
		HIDDEN: console.ConsolePos = ConsolePosition.hidden;
		return;
		LOWER: console.ConsolePos = ConsolePosition.lower;
		return;
		SHOWN: console.ConsolePos = ConsolePosition.shown;
		return;
	}

	public void ToggleMenu () {
		script.ToggleMenu();
		Globals.audio.UIPlay(UISound.soft_click);
	}
}