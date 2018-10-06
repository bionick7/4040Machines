using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConsoleBehaviour : MonoBehaviour {

	private InputField input;
	private Text text;
	private List<string> _lines = new List<string>();
	private List<string> Lines {
		get {
			return _lines;
		}
		set {
			_lines = value;
		}
	}
	private Stack<string> InputLines = new Stack<string>();

	public bool HasInput {
		get {
			return InputLines.Count > 0;
		}
	}

	public const int full_lines = 28;
	public int lines_shown = full_lines;

	public void Start_ () {
		input = GetComponentInChildren<InputField>();
		text = transform.GetChild(0).GetComponent<Text>();
		Lines.Add(input.text);
		Write();
	}

	public void WriteLine (string text) {
		Lines.AddRange(text.Split('\n'));
		Write();
	}

	public string ReadLine () {
		if (InputLines.Count == 0) { return string.Empty; }
		string answer = InputLines.Pop();
		return answer;
	}

	public void Clear () {
		Lines.Clear();
	}

	public void Write () {
		string txt = string.Empty;
		for ( int i=Mathf.Max(Lines.Count - lines_shown + 1, 0); i < Lines.Count; i++) {
			if (Lines[i].Replace("\n",string.Empty) != string.Empty) {
				txt += Lines [i] + "\n";
			}
		}
		text.text = txt;
	}
	
	public void Enter () {
		InputLines.Push(input.text);
		input.text = string.Empty;
		Write();
	}

	private void Update () {
		SceneData.in_console = input.isFocused;
	}
}
