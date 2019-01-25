using UnityEngine;
using System.Collections.Generic;

public class Navigation
{
	private List<FuzzyVector> translation = new List<FuzzyVector>();
	private List<FuzzyVector> rotation = new List<FuzzyVector>();
	private float throttle;

	public ShipControl control_script;

	public Navigation (ShipControl p_control_script) {
		control_script = p_control_script;
	}

	public void AddTranslation (Vector3 vec, float importance) {
		AddTranslation(new FuzzyVector(vec, importance));
	}

	public void AddTranslation (FuzzyVector fvec) {
		translation.Add(fvec);
	}

	public void AddRotation (Quaternion rot, float importance) {
		AddRotation(new FuzzyVector(rot.eulerAngles, importance));
	}

	public void AddRotation (Vector3 rot, float importance) {
		AddRotation(new FuzzyVector(rot, importance));
	}

	public void AddRotation (FuzzyVector fvec) {
		rotation.Add(fvec);
	}

	public void SetThrottle (float p_throttle) {
		throttle = p_throttle;
	}

	/// <summary> Applyes all navigation </summary>
	public void Navigate () {
		if (!control_script.rcs_thrust_player) {
			Vector3 final_translation = Vector3.zero;
			float final_importance = 0;
			foreach (FuzzyVector fvec in translation) {
				final_translation += fvec.vector * fvec.importance;
				final_importance += fvec.importance;
			}
			if (final_importance < .1f) {
				control_script.inp_thrust_vec = Vector3.zero;
			} else {
				control_script.inp_thrust_vec = CutVector(final_translation);
			}
		}

		if (!control_script.torque_player) {
			Vector3 final_rotation = Vector3.zero;
			float final_importance = 0;
			foreach (FuzzyVector fvec in rotation) {
				final_rotation += fvec.vector * fvec.importance;
				final_importance += fvec.importance;
			}
			if (final_importance < .1f) {
				control_script.inp_torque_vec = Vector3.zero;
			} else {
				control_script.inp_torque_vec = CutVector(final_rotation);
			} 
		}

		if (!control_script.engine_thrust_player) {
			control_script.myship.Throttle = throttle;
		}

		throttle = 0;

		translation.Clear();
		rotation.Clear();
	}

	public static Vector3 CutVector(Vector3 inp, float min=-1, float max=1) {
		return new Vector3(Mathf.Min(Mathf.Max(inp.x, min), max),
						   Mathf.Min(Mathf.Max(inp.y, min), max),
						   Mathf.Min(Mathf.Max(inp.z, min), max));
	}
}

/// <summary> Mix between Stack and Queue </summary>
/// <remarks> 
/// 
///							 OUT {= | 1 | 2 | 3 |
///	You can insert things here ... ^			  ^ ... or here
///	
/// </remarks>
public class Quack<T> : System.Collections.IEnumerable
{
	private T[] content;

	public uint Count {
		get { return (uint) content.Length; }
	}

	public Quack() {
		content = new T [0];
	}

	public T this[uint indx] {
		get { return content [indx]; }
		set { content [indx] = value; }
	}

	public T Get () {
		if (content.Length > 0)
			return content [0];
		throw new System.ArgumentOutOfRangeException("Quack size is zero");
	}

	public System.Collections.IEnumerator GetEnumerator () {
		return new QuackEnumerator<T>(content);
	}

	public void ClearTop () {
		if (content.Length == 0) return; 
		T[] new_content = new T [content.Length - 1];
		for (int i=1; i < content.Length; i++) {
			new_content [i - 1] = content [i];
		}
		content = new_content;
	}

	/// <summary> Like "push" </summary>
	public void PushFront (T value) {
		T[] new_content = new T[content.Length + 1];
		content.CopyTo(new_content, 1);
		new_content [0] = value;
		content = new_content;
	}
	
	/// <summary> Like "enqueue" </summary>
	public void PushBack (T value) {
		T[] new_content = new T[content.Length + 1];
		content.CopyTo(new_content, 0);
		new_content [content.Length] = value;
		content = new_content;
	}

	public void PushFront (T[] values) {
		T[] new_content = new T[content.Length + values.Length];
		values.CopyTo(new_content, 0);
		content.CopyTo(new_content, values.Length);
		content = new_content;
	}

	public void PushBack (T[] values) {
		T[] new_content = new T[content.Length + values.Length];
		content.CopyTo(new_content, 0);
		content.CopyTo(new_content, content.Length);
		content = new_content;
	}

	public void Clear () {
		content = new T [0];
	}

	private class QuackEnumerator<T> : IEnumerator<T>
	{
		private int position = -1;

		private T[] array;

		public T Current {
			get {
				if (position < array.Length) {
					return array [position];
				} else {
					throw new System.ArgumentOutOfRangeException();
				}
			}
		}

		object System.Collections.IEnumerator.Current {
			get { return Current; }
		}

		public QuackEnumerator(T[] parray) {
			array = parray;
		}

		public void Dispose () {

		}

		public void Reset () {
			position = -1;
		}

		public bool MoveNext () {
			position++;
			return position < array.Length;
		}
	}
}

public interface IAICommand
{
	bool IsAccomplished (Ship current_ship);
	void Execute (ref Navigation nav, Ship current_ship);
}

public struct AIMouvementCommand :IAICommand
{
	private ShipState desired_state;
	public CommandType command_type;
	private bool first;

	private AIMouvementCommand(CommandType p_command_type, ShipState p_desired) {
		command_type = p_command_type;
		desired_state = p_desired;
		first = true;
	}

	public bool IsAccomplished (Ship current_ship) {
		switch (command_type) {
		case CommandType.turn:
			return Vector3.Angle(current_ship.Transform.forward, desired_state.Orientation * Vector3.forward) < .1f && current_ship.AngularVelocity.sqrMagnitude < .1f;
		case CommandType.main_dv:
			if (Vector3.Dot(desired_state.Velocity - current_ship.Velocity, current_ship.Transform.forward) <= 0) {
				//current_ship.Throttle = 0;
				return true;
			}
			return false;
		case CommandType.rcs_dv:
			return Vector3.Distance(current_ship.Velocity, desired_state.Velocity) < .02f;
		case CommandType.hold_orientation:
		case CommandType.stay_locked:
		case CommandType.wait:
			return desired_state.Time <= GeneralExecution.TotalTime;
		default: return true;
		}
	}

	public void Execute (ref Navigation nav, Ship current_ship) {
		switch (command_type) {
		case CommandType.turn:
			nav.AddRotation(new FuzzyVector(current_ship.low_ai.CalculateTurn(desired_state.Orientation * Vector3.forward), .5f));
			return;
		case CommandType.main_dv:
			nav.SetThrottle(1);
			return;
		case CommandType.rcs_dv:
			nav.AddTranslation(new FuzzyVector((desired_state.Velocity - current_ship.Velocity).normalized, .5f));
			return;
		case CommandType.hold_orientation:
			if (first) {
				desired_state.Orientation = current_ship.Orientation;
				first = false;
			}
			nav.AddRotation(new FuzzyVector(current_ship.low_ai.CalculateTurn(desired_state.Orientation * Vector3.forward), .5f));
			return;
		case CommandType.stay_locked:
			nav.AddRotation(new FuzzyVector(current_ship.low_ai.CalculateTurn(desired_state.Target.Position - current_ship.Position), .5f));
			return;
		case CommandType.wait:
		default: return;
		}
	}

	public static AIMouvementCommand Turn(Quaternion p_orientation, Ship ship, bool directly=false) {
		return new AIMouvementCommand(CommandType.turn, new ShipState() {
			Orientation = directly ? p_orientation : ship.Orientation * p_orientation
		});
	}

	public static AIMouvementCommand RCSAccelerate(Vector3 p_velocity, Ship ship, bool directly=false) {
		return new AIMouvementCommand(CommandType.rcs_dv, new ShipState() {
			Velocity = directly ? p_velocity : p_velocity + ship.Velocity
		});
	}

	public static AIMouvementCommand EngineAccelerate(Vector3 velocity, Ship ship, bool directly=false) {
		return new AIMouvementCommand(CommandType.main_dv, new ShipState() {
			Velocity = directly ? velocity : ship.Velocity + velocity
		});
	}

	public static AIMouvementCommand Wait(double p_time, bool directly=false) {
		return new AIMouvementCommand(CommandType.wait, new ShipState() {
			Time = directly ? p_time : GeneralExecution.TotalTime + p_time
		});
	}

	public static AIMouvementCommand HoldOrientation(double p_time, Ship ship, bool directly=false) {
		return new AIMouvementCommand(CommandType.hold_orientation, new ShipState() {
			Time = directly ? p_time : GeneralExecution.TotalTime + p_time
		});
	}

	public static AIMouvementCommand StayLocked(double p_time, Ship ship, IAimable tgt, bool directly=false) {
		return new AIMouvementCommand(CommandType.stay_locked, new ShipState() {
			Target = tgt,
			Time = directly ? p_time : GeneralExecution.TotalTime + p_time
		});
	}

	public override string ToString () {
		switch (command_type) {
		case CommandType.turn:
			return string.Format("Turn: {0}", desired_state.Orientation.eulerAngles);
		case CommandType.main_dv:
			return string.Format("Main: {0}m/s", desired_state.Velocity);
		case CommandType.rcs_dv:
			return string.Format("RCS : {0}", desired_state.Velocity);
		case CommandType.wait:
			return string.Format("Wait: {0}", desired_state.Time);
		case CommandType.hold_orientation:
			return string.Format("Hold: {0} for {1}", desired_state.Orientation.eulerAngles, desired_state.Time);
		case CommandType.stay_locked:
			return string.Format("Lock: {0} for {1}", desired_state.Orientation.eulerAngles, desired_state.Time);
		default: return string.Empty;
		}
	}

	struct ShipState
	{
		public Vector3 Position;
		public Vector3 Velocity;
		public Quaternion Orientation;
		public Vector3 AngularVelocity;
		public IAimable Target;
		public double Time;

		public ShipState (Ship ship) {
			Position = ship.Position;
			Velocity = ship.Velocity;
			Orientation = ship.Orientation;
			AngularVelocity = ship.AngularVelocity;
			Target = ship.TurretAim;
			Time = GeneralExecution.TotalTime;
		}
	}

	public ulong ToCode () {
		ulong machine_command = 0;
		switch (command_type) {
		case CommandType.turn:
			Vector3 rot = desired_state.Orientation.eulerAngles;
			//					 CCi&AAAABBBBCCCC
			machine_command =  0x8000000000000000;
			machine_command += 0x0001000000000000;
			machine_command += (ulong) (Mathf.Min(rot.x * 100, ushort.MaxValue)) << 32;
			machine_command += (ulong) (Mathf.Min(rot.y * 100, ushort.MaxValue)) << 16;
			machine_command += (ulong) (Mathf.Min(rot.z * 100, ushort.MaxValue));
			return machine_command;
			
		case CommandType.main_dv:
			break;
		case CommandType.rcs_dv:
			break;
		case CommandType.wait:
			break;
		case CommandType.hold_orientation:
			break;
		case CommandType.stay_locked:
			break;
		default:
			return 0;
		}
		return 0;
	}


	public enum CommandType
	{
		turn,
		main_dv,
		rcs_dv,
		wait,
		hold_orientation,
		stay_locked,
	}
}

public struct AIActionCommand : IAICommand
{
	private CommandType command_type;
	private IAimable target;
	private bool is_first;
	
	private AIActionCommand (CommandType p_command_type) {
		command_type = p_command_type;
		target = Target.None;
		is_first = true;
	}

	public bool IsAccomplished (Ship current_ship) {
		switch (command_type) {
		case CommandType.shoot_tgs:
			return !is_first;
		case CommandType.fire_main:
			return false;
		case CommandType.shoot_missile:
			return !is_first;
		default: return true;
		}
	}

	public void Execute (ref Navigation _, Ship current_ship) {
		switch (command_type) {
		case CommandType.shoot_tgs:
			return;
		case CommandType.fire_main:
			if (Vector3.Angle(current_ship.Transform.forward, target.Position - current_ship.Position) < .01) {
				current_ship.control_script.Trigger_Shooting(true);
			} else {
				current_ship.control_script.Trigger_Shooting(false);
			}
			return;
		case CommandType.shoot_missile:
			if (is_first) {
				current_ship.control_script.FireMissile();
				is_first = false;
			}
			return;
		default: return;
		}
	}

	public static AIActionCommand ShootTG (TurretGroup group, IAimable target) {
		return new AIActionCommand(CommandType.shoot_tgs) {
			target = target
		};
	}

	public static AIActionCommand FireMain (IAimable target) {
		return new AIActionCommand(CommandType.fire_main) {
			target = target
		};
	}

	public static AIActionCommand ShootMissile (uint missile) {
		return new AIActionCommand(CommandType.shoot_missile);
	}

	enum CommandType
	{
		shoot_tgs,
		fire_main,
		shoot_missile,
	}
}