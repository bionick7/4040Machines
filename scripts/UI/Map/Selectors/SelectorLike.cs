using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class SelectorLike : MonoBehaviour, IDrawable,
IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
	private const ushort thickness = 2;

	public string[] options_str;
	public Sprite[] options_icons = null;
	public int[] options_flags;
	public int[] option_functions;

	public Image head;
	public Image body;
	public Image tail;

	public GameObject optionsButton;
	public bool draggable = true;

	/// <summary> The time to open the selector in seconds </summary>
	public float opening_time = 5f;
	private float OpeningFrames {
		get {
			if (opening_time == 0) return 1;
			return opening_time / Time.deltaTime; }
	}
	/// <summary> The time to close the selector in seconds </summary>
	public float closing_time = 5f;
	private float ClosingFrames {
		get { return closing_time / Time.deltaTime; }
	}

	private Camera map_cam;
	private RectTransform main_rect_trans;
	private RectTransform body_rect_trans;
	public float width;
	public float head_height;
	public float tail_height;
	protected SelectorLike child;
	public IMarkerParentObject[] targets = null;

	protected bool dragging;
	private bool mousehover;
	private Vector2 mousepos_delta;
	protected float button_height;

	protected SelectorState currentstate;
	public SelectorIdentifyer identifyer = SelectorIdentifyer.none;

	/// <summary> The time, the Selector is already in a specific state, in seconds </summary>
	private uint time_frames;
	protected bool initialized = false;

	public ushort min_height;
	public ushort max_height;
	private ushort delta_height;

	private List<Behaviour> _graphicscomponents = new List<Behaviour>();
	public Behaviour [] GraphicsComponents {
		get { return _graphicscomponents.ToArray(); }
	}

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
		set {
			main_rect_trans.sizeDelta = new Vector2(width, value + head_height + tail_height);
			// Check upper bounders
			if (main_rect_trans.rect.yMax + main_rect_trans.position.y >= Screen.height) {
				main_rect_trans.position = new Vector3(main_rect_trans.position.x, Screen.height - Height / 2 - head_height);
			}
			// Check lower bounders
			if (main_rect_trans.rect.xMax + main_rect_trans.position.y <= 0) {
				main_rect_trans.position = new Vector3(main_rect_trans.position.x, Height / 2 + tail_height);
			}
		}
	}

	public void Init () {
		if (options_icons == null || options_icons.Length != options_str.Length)
			options_icons = new Sprite [options_str.Length];
		for (int i = 0; i < options_str.Length; i++) {
			if (options_icons [i] == null) options_icons [i] = Globals.selector_data.default_sprite;
		}

		initialized = true;
		map_cam = SceneGlobals.map_camera;
		main_rect_trans = GetComponent<RectTransform>();
		body_rect_trans = body.GetComponent<RectTransform>();

		mousepos_delta = Vector2.zero;

		float txt_width = GetMaxWidth() + 30;

		head_height = head.GetComponent<RectTransform>().rect.height;
		tail_height = tail.GetComponent<RectTransform>().rect.height;

		width = txt_width + 20;

		_graphicscomponents.Add(head);
		_graphicscomponents.Add(body);
		_graphicscomponents.Add(tail);

		Position.Set((int) transform.position.x, (int) transform.position.y);
		button_height = optionsButton.GetComponent<RectTransform>().rect.height;
		for (byte i=0; i < options_str.Length; i++) {
			string possibility = options_str[i];
			GameObject new_button_obj = Instantiate(optionsButton.gameObject);

			RectTransform button_rect = new_button_obj.GetComponent<RectTransform>();
			button_rect.position = new Vector3(0, - (i + .5f) * button_height);
			button_rect.sizeDelta = new Vector3(txt_width + 6, button_height);

			Text button_text = new_button_obj.GetComponentInChildren<Text>();
			button_text.text = possibility;
			button_text.rectTransform.sizeDelta = new Vector3(txt_width, button_height);

			new_button_obj.transform.SetParent(body.transform, false);
			new_button_obj.transform.SetAsLastSibling();
			new_button_obj.GetComponent<SelectorButton>().Init(this, i, options_icons[i], options_flags[i], option_functions[i]);
		}

		min_height = 0;
		max_height = (ushort) (button_height * options_str.Length);
		delta_height = (ushort) (max_height - min_height);

		Height = 0;
		currentstate = SelectorState.closed;

		Open();
	}

	private void Start () {
		if (!initialized) Init();
	}

	/// <summary> Returns the width of the longest string in the options in pixels (float) </summary>
	private float GetMaxWidth() {
		float max_txt_width = 0;
		var font = optionsButton.GetComponentInChildren<Text>().font;
		CharacterInfo chr_info;
		foreach (string str_opt in options_str) {
			float txt_width = 0;
			foreach (char c in str_opt) {
				font.GetCharacterInfo(c, out chr_info);
				txt_width += chr_info.advance * 1.1666f;
			}
			if (txt_width > max_txt_width) max_txt_width = txt_width;
		}
		//DeveloppmentTools.Log("selector text width: " + max_txt_width);
		return max_txt_width;
	}

	protected void Update () {
		Draw(map_cam);
		time_frames++;

		if (mousehover) {
			if (Input.GetMouseButtonDown(0) && MouseOverHead && draggable) {
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
			Vector2 InputPos = ((Vector2) Input.mousePosition - mousepos_delta);
			Position = new Vector2(Mathf.Max(Mathf.Min(InputPos.x, Screen.width + main_rect_trans.rect.xMin), main_rect_trans.rect.xMax), 
								   Mathf.Max(Mathf.Min(InputPos.y, Screen.height - Height / 2 - head_height), Height / 2 + tail_height));
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
			// Called during opening
			if (Height >= max_height) {
				Height = max_height;
				FinishOpening();
			} else {
				Vector2 d_vec = Vector2.zero;
				try {
					d_vec = new Vector2(0, delta_height / OpeningFrames);
				} catch (System.DivideByZeroException) {; }

				Height = min_height + d_vec.y * time_frames;
				Position -= d_vec * .5f;
			}
			break;

		case SelectorState.closing:
			if (Height <= min_height) {
				Height = min_height;
				FinishClosing();
			} else {
				Vector2 d_vec = Vector2.zero;
				try {
					d_vec = new Vector2(0, delta_height / OpeningFrames);
				} catch (System.DivideByZeroException) {; }

				Height = max_height - d_vec.y * time_frames;
				Position += d_vec * .5f;
			}
			break;
		}
	}

	/// <summary> Beginns the opening of the selector </summary>
	private void Open () {
		currentstate = SelectorState.opening;
		time_frames = 1;
	}

	/// <summary> Beginns the closing of the selector </summary>
	private void Close () {
		currentstate = SelectorState.closing;
		time_frames = 1;
	}

	/// <summary> Finishes the opening of the selector </summary>
	private void FinishOpening () {
		currentstate = SelectorState.opened;
		time_frames = 1;
	}

	/// <summary> Finishes the closing of the selector </summary>
	private void FinishClosing () {
		currentstate = SelectorState.closed;
		time_frames = 1;
	}

	/// <summary> Should trigger, when the exit button is pressed </summary>
	public void ExitButtonPressed () {
		Destroy(gameObject);
	}

	/// <summary> Spawns a child to the Selector </summary>
	/// <param name="button"> Where the child comes from </param>
	/// <param name="child_options"> The new options for the child </param>
	/// <returns> The Selectorlike of the child, or null if the button is not valid </returns>
	public SelectorLike SpawnChild (string button, string[] child_options, object[] obj_arr=null, Sprite[] icons=null, int[] flags=null, int[] functions=null) {
		if (child != null) {
			try {
				Destroy(child.gameObject);
			} catch { }
		}
		sbyte pos = -1;
		for (int i=0; i < options_str.Length; i++) {
			if (options_str [i] == button) pos = (sbyte) i;
		}
		if (pos == -1) return null; // "What are you even doing?"

		GameObject selectorobj = Instantiate(GameObject.Find("map_selector_simple"));
		selectorobj.transform.SetParent(SceneGlobals.map_canvas.transform);

		// Setting foelds of new_child
		SelectorLike new_child = Loader.EnsureComponent<SelectorLike>(selectorobj);
		new_child.options_str = child_options;
		new_child.options_icons = icons;
		new_child.options_flags = flags;
		new_child.option_functions = functions;
		new_child.targets = targets;
		new_child.Init();
		
		new_child.Position = UpperLeft + new Vector2(width + new_child.width / 2, -new_child.head_height);
		new_child.draggable = false;
		new_child.transform.SetParent(transform, true);
		child = new_child;

		return new_child;
	}

	public void OnPointerEnter (PointerEventData pdata) {
		transform.SetAsLastSibling();
		mousehover = true;
	}

	public void OnPointerClick (PointerEventData pdata){
		transform.SetAsLastSibling();
		mousehover = true;
	}

	public void OnPointerExit (PointerEventData pdata) {
		mousehover = false;
	}
}

public enum SelectorState
{
	opened,
	closed,
	opening,
	closing
}

public enum SelectorParent
{
	selector,
	point,
	target,
	ship
}

public enum SelectorIdentifyer
{
	none,
	target_parts,
	attack,
	turret_attack,
	match_velocity,
	match_velocity_closest,
}