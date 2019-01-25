using UnityEngine;
using NMS;

public class ConsoleInterpreter : MonoBehaviour {

	public ConsoleBehaviour console;
	public Interpreter interpreter;

	void Start () {
		foreach (GameObject obj in FindObjectsOfType<GameObject>()) {
			if (obj.GetComponent<ConsoleBehaviour>() != null) {
				console = obj.GetComponent<ConsoleBehaviour>();
			}
		}
		interpreter = new Interpreter();
	}
	

	void Update () {
		if (console.HasInput) {
			string input = console.ReadLine();
			byte [] res;
			string answer = interpreter.Process(input, out res);
			console.WriteLine(answer);
		}
	}
}
