/* ==================================================================================
 * This is a kind of bridge between the buttons and the UI script.
 * Because the player object wis not known at first, functions have to go here first.
 * They then trigger the UI script, which is known in this script.
 * Currently stored in "Database"
 * ================================================================================== */

public class ButtonListener : UnityEngine.MonoBehaviour {

	private GUIScript script;
	private AddGroupMenu add_menu;

	void Start () {
		script = SceneData.ui_script;
		add_menu = script.add_group_menu;
	}

	public void GroupSwitch () {
		script.GroupSwitch = !script.GroupSwitch;
		script.Click();
	}

	public void TGAimTarget () {
		script.TurretGroupAimTarget();
		script.Click();
	}

	public void TGAimMarker () {
		script.TurretGroupAimDirection();
		script.Click();
	}

	public void TGAimCursor () {
		script.TurretGroupAimCursor();
		script.Click();
	}

	public void TGEdit () {
		script.CallGroupMenu(true);
		script.Click();
	}

	public void TGDelete () {
		script.DeleteTurretGroup();
		script.Click();
	}

	public void ToGroup () {
		script.CallGroupMenu(false);
		script.Click();
	}

	public void EnDisable (bool single_turret) {
		if (single_turret) {
			script.selected_turret.Enabled = !script.selected_turret.Enabled;
		} else {
			script.selected_group.Enabled = !script.selected_group.Enabled;
		}
		script.Click();
	}

	/// <summary> Moves the console </summary>
	/// <param name="pos">
	///		If 0, the console is moved to the next state (hidden > lower > shown > hidden)
	///		If 1, the console is hidden
	///		If 2, the console is lowered
	///		If 3, the console is shown
	/// </param>
	public void ToggleConsole (int pos=0) {
		script.Click();
		switch (pos) {
		case 0:
			switch (script.ConsolePos) {
			case ConsolePosition.hidden: goto LOWER;
			case ConsolePosition.lower: goto SHOWN;
			default: goto HIDDEN;
			}
		case 1: goto HIDDEN;
		case 2: goto LOWER;
		case 3: goto SHOWN;
		}
		HIDDEN: script.ConsolePos = ConsolePosition.hidden;
		return;
		LOWER: script.ConsolePos = ConsolePosition.lower;
		return;
		SHOWN: script.ConsolePos = ConsolePosition.shown;
		return;
	}

	public void ToggleMenu () {
		script.ToggleMenu();
		script.Click();
	}
}