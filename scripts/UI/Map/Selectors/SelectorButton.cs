using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SelectorButton : MonoBehaviour, 
IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{
	public string label;
	public SelectorLike parent;
	public byte num;

	public int flags;
	public int function;

	private Image img;
	private Image icon_img;
	private Text label_text;
	private AudioManager audio_;

	private static readonly Color idle = new Color(.71f, .72f, .62f, 0);
	private static readonly Color hover = new Color(.71f, .72f, .62f, .5f);
	private static readonly Color pressed = new Color(.71f, .72f, .62f, 1);

	private const UISound hover_enter = UISound.soft_click;
	private const UISound click = UISound.dump_click;

	public void Init (SelectorLike p_parent, byte p_num, Sprite icon, int pflags, int pfunction) {
		parent = p_parent;
		num = p_num;
		audio_ = Globals.audio;
		img = GetComponent<Image>();
		icon_img = transform.GetChild(1).GetComponent<Image>();
		icon_img.sprite = icon;

		label_text = GetComponentInChildren<Text>();
		label = label_text.text;
		img.color = idle;
		flags = pflags;
		function = pfunction;
	}

	public void OnPointerEnter(PointerEventData pdata) {
		img.color = hover;
		//MapCore.Active.event_system.SelectorLikeHovered(this, true);
		audio_.UIPlay(hover_enter, .2f);
	}

	public void OnPointerExit(PointerEventData pdata) {
		img.color = idle;
		//MapCore.Active.event_system.SelectorLikeHovered(this, false);
	}

	public void OnPointerDown(PointerEventData pdata) {
		img.color = pressed;
		audio_.UIPlay(click);
		if (pdata.button == PointerEventData.InputButton.Left && flags % 2 == 1) {
			PinLabel.Dragging = true;
			PinLabel.Active.Context = PinLabel.PinContext.selector;
			MapCore.Active.selector_event_system.SelectorClicked(this);
		}
	}

	public void OnPointerUp(PointerEventData pdata) {
		img.color = hover;
		if (pdata.button == PointerEventData.InputButton.Left && flags % 2 == 0) {
			if (pdata.pointerPress == gameObject) {
				var parent_sel = parent as Selector;
				var parent_subsel = parent as SubSelector;
				if (parent_sel != null)
					parent_sel.SpawnSubSelector((SelectionClass) new List<string>(System.Enum.GetNames(typeof(SelectionClass))).IndexOf(label));
				else if (parent_subsel != null)
					MapCore.Active.selector_event_system.SelectorClicked(this);
			}
		}
	}
 }