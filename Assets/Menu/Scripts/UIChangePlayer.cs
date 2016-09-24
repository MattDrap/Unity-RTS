using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using RTS;

public class UIChangePlayer : MonoBehaviour
{
	public GameObject Dropdown;
	public GameObject InputF;
	public GameObject MainMenu;
	Dropdown dropdown;
	InputField inputField;
	protected void Awake(){
		inputField = InputF.GetComponent<InputField> ();
		inputField.onEndEdit.AddListener (delegate {
			OnEndEdit(inputField);
		});

		dropdown = Dropdown.GetComponent<Dropdown> ();
		dropdown.onValueChanged.AddListener( delegate {
			OnValueChangedHandler(dropdown);
		});


		populate ();
	}
	protected void OnEnable(){
		populate ();
	}
	void Update(){
		if (Input.GetKey (KeyCode.Escape)) {
			MainMenu.SetActive (true);
			this.gameObject.SetActive (false);
		}
	}
	void OnValueChangedHandler(Dropdown dropdown){
		PlayerManager.SelectPlayer (PlayerManager.GetPlayerNames () [dropdown.value]);
	}
	void populate(){
		dropdown.options.Clear ();
		List<string> opts = new List<string> (PlayerManager.GetPlayerNames());
		dropdown.AddOptions(opts);
		string curr = PlayerManager.GetPlayerName ();
		int num = opts.FindIndex (a => a == curr);
		dropdown.value = num;
	}

	void OnEndEdit(InputField inputf){
		if (string.IsNullOrEmpty (inputf.text))
			return;
		PlayerManager.SelectPlayer(inputf.text);
		populate ();
	}
	void Destroy(){
		dropdown.onValueChanged.RemoveAllListeners ();
		inputField.onEndEdit.RemoveAllListeners ();
	}
}

