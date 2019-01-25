using UnityEngine;

public class Retractable : MonoBehaviour
{
	protected MovingState moving_state;

	private float time = 1;
	private Vector3
		min = Vector3.zero,
		max = new Vector3(500, 0),
		speed = Vector3.zero;

	private float beginning_time = 0;
	private bool _shown;
	public bool Shown {
		get { return _shown; }
		set {
			if (_shown != value) {
				moving_state = value ? MovingState.opening : MovingState.closing;
				beginning_time = Time.time;
			}
			_shown = value;
		}
	}

	protected void Update () {
		switch (moving_state) {
		case MovingState.opening:
			if (Time.time - beginning_time >= time) {
				transform.position = max;
				moving_state = MovingState.opened;
			}
			transform.Translate(speed * Time.deltaTime);
			break;
		case MovingState.closing:
			if (Time.time - beginning_time >= time) {
				transform.position = min;
				moving_state = MovingState.closed;
			}
			transform.Translate(-speed * Time.deltaTime);
			break;
		case MovingState.opened:
		case MovingState.closed:
		default: break;
		}
	}

	protected void SetVariables (float ptime, Vector3 pmin, Vector3 pmax) {
		time = ptime;
		min = pmin;
		max = pmax;
		speed.x = Mathf.Abs(pmax.x - pmin.x) / ptime;
		speed.y = Mathf.Abs(pmax.y - pmin.y) / ptime;
	}


	protected enum MovingState
	{
		opened,
		opening,
		closed,
		closing
	}
}
