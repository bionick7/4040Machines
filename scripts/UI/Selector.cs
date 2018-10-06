using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

class Selector : MonoBehaviour, IDrawable, IPointerEnterHandler, IPointerExitHandler
{
	public string[] possibilities;
	public Image head;
	public Image body;
	public Image tail;
	public Button optionsButton;

	public Material line_material;

	/// <summary> The time to open the selector in seconds </summary>
	public float opening_time = .5f;
	private ushort OpeningFrames {
		get { return (ushort) (opening_time / Time.deltaTime); }
	}
	/// <summary> The time to close the selector in seconds </summary>
	public float closing_time = .5f;
	private ushort ClosingFrames {
		get { return (ushort) (closing_time / Time.deltaTime); }
	}

	private Camera map_cam;
	private RectTransform main_rect_trans;
	private RectTransform body_rect_trans;
	private float width;
	private float head_height;
	private float tail_height;

	private uint lineindex;

	private bool dragging;
	private bool mousehover;
	private Vector2 mousepos_delta;

	private SelectorState currentstate;
	private SceneObject target = null;
	/// <summary> The time, the Selector is already in a specific state, in seconds </summary>
	private uint time_frames;

	private ushort min_height;
	private ushort max_height;
	private ushort delta_height;

	private List<Behaviour> _graphicscomponents = new List<Behaviour>();
	public Behaviour [] GraphicsComponents {
		get { return _graphicscomponents.ToArray(); }
	}

	public Vector2 AnchorPoint { get; set; }
	public Vector2 Position {
		get { return main_rect_trans.position; }
		set { main_rect_trans.position = value; }
	}

	private bool _open;
	/// <summary> True, if the selecter is opened </summary>
	public bool IsOpen {
		get { return _open; }
		set {
			bool closing = currentstate == SelectorState.closed || currentstate == SelectorState.closing;
			if (value) {
				if (closing) Open();
			} else {
				if (!closing) Close();
			}
			_open = value;
		}
	}

	public Vector2 UpperLeft {
		get { return Position - new Vector2(width / 2, - Height/2 - head_height); }
	}

	private bool MouseOverHead {
		get { return head.rectTransform.rect.Contains(Input.mousePosition - head.rectTransform.position); }
	}

	private bool MouseOverTail {
		get { return tail.rectTransform.rect.Contains(Input.mousePosition - tail.rectTransform.position); }
	}

	/// <summary> The height of the body </summary>
	public float Height {
		get { return body_rect_trans.rect.size.y; }
		set { main_rect_trans.sizeDelta = new Vector2(width, value + head_height + tail_height); }
	}

	private void Start () {
		map_cam = SceneData.map_camera;
		main_rect_trans = GetComponent<RectTransform>();
		body_rect_trans = body.GetComponent<RectTransform>();

		if (target == null) target = Target.None;
		mousepos_delta = Vector2.zero;

		head_height = head.GetComponent<RectTransform>().rect.height;
		tail_height = tail.GetComponent<RectTransform>().rect.height;
		width = main_rect_trans.rect.width;

		_graphicscomponents.Add(head);
		_graphicscomponents.Add(body);
		_graphicscomponents.Add(tail);

		Position.Set((int) transform.position.x, (int) transform.position.y);

		float button_height = optionsButton.GetComponent<RectTransform>().rect.height;
		for (int i=0; i < possibilities.Length; i++) {
			string possibility = possibilities[i];
			GameObject new_button_obj = Instantiate(optionsButton.gameObject);
			new_button_obj.GetComponent<RectTransform>().position = new Vector3(0, - (i + .5f) * button_height);
			new_button_obj.GetComponentInChildren<Text>().text = possibility;
			new_button_obj.transform.SetParent(body.transform, false);
			new_button_obj.transform.SetAsLastSibling();
			_graphicscomponents.Add(new_button_obj.GetComponent<Button>());
		}

		min_height = 0;
		max_height = (ushort) (button_height * possibilities.Length);
		delta_height = (ushort) (max_height - min_height);

		Height = 0;
		currentstate = SelectorState.closed;

		Open();

		lineindex = CameraDrawing.AddLine(new Line2D(AnchorPoint, UpperLeft + new Vector2(5, -5), 5));
	}

	private void Update () {
		//if (target.Exists) SpacePosition = target.Position;
		AnchorPoint = Vector3.zero;
		Draw(map_cam);
		time_frames++;

		if (mousehover) {
			if (Input.GetMouseButtonDown(0) && MouseOverHead) {
				dragging = true;
				mousepos_delta = (Vector2) Input.mousePosition - Position;
			}
			if (Input.GetMouseButtonDown(0) && MouseOverTail) {
				if (currentstate == SelectorState.closed || currentstate == SelectorState.closing) {
					Open();
				} else {
					Close();
				}
			}

			if (dragging && !Input.GetMouseButton(0)) {
				dragging = false;
			}
		}

		if (dragging) {
			Position = ((Vector2) Input.mousePosition - mousepos_delta);
			CameraDrawing.UpdateLine(lineindex, new Line2D(AnchorPoint, UpperLeft + new Vector2(5, -5), 5));
		}
	}

	public void Draw (Camera cam) {
		switch (currentstate) {
		case SelectorState.opened:
			// Do nothing here
			break;

		case SelectorState.closed:
			// Do nothing here
			break;

		case SelectorState.opening:
			if (Height >= max_height) {
				Height = max_height;
				FinishOpening();
			} else {
				Height = min_height + delta_height / OpeningFrames * time_frames;
			}
			break;

		case SelectorState.closing:
			if (Height <= min_height) {
				Height = min_height;
				FinishClosing();
			} else {
				Height = max_height - delta_height / ClosingFrames * time_frames;
			}
			break;
		}
	}

	/// <summary> Beginns the opening of the selector </summary>
	private void Open () {
		currentstate = SelectorState.opening;
		time_frames = 0;
	}

	/// <summary> Beginns the closing of the selector </summary>
	private void Close () {
		currentstate = SelectorState.closing;
		time_frames = 0;
	}

	/// <summary> Finishes the opening of the selector </summary>
	private void FinishOpening () {
		currentstate = SelectorState.opened;
		time_frames = 0;
	}

	/// <summary> Finishes the closing of the selector </summary>
	private void FinishClosing () {
		currentstate = SelectorState.closed;
		time_frames = 0;
	}

	/// <summary> Should trigger, when the exit button is pressed </summary>
	public void ExitButtonPressed () {
		Destroy(gameObject);
		CameraDrawing.DeleteLine(lineindex);
	}

	/// <summary> Should trigger, when any of the options buttons is pressed </summary>
	/// <param name="button_id"> The number of the pressed button </param>
	public void OptionButtonPressed (byte button_id) {

	}

	public void OnPointerEnter(PointerEventData pdata) {
		transform.SetAsLastSibling();
		mousehover = true;
	}

	public void OnPointerExit(PointerEventData pdata) {
		mousehover = false;
	}

	private enum SelectorState
	{
		opened,
		closed,
		opening,
		closing
	}
}

