using UnityEngine;
using System.Collections.Generic;
using RTS;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UISaveMenu : MonoBehaviour
{
	private string saveName = "NewGame";

	public Button save_button;
	public GameObject Dropdown;
	public GameObject MainMenu;
	public InputField InputF;
	Dropdown dropdown;
	protected void Awake(){
		dropdown = Dropdown.GetComponent<Dropdown> ();
		save_button.onClick.AddListener( delegate {
			OnClick();	
		});
		dropdown.onValueChanged.AddListener (delegate {
			OnDropDown();	
		});
		populate ();
	}

	protected void OnEnable(){
		populate ();
	}

	void populate(){
		dropdown.options.Clear ();
		List<string> opts = new List<string> (PlayerManager.GetSavedGames());
		dropdown.AddOptions(opts);
	}

	void Destroy(){
		dropdown.onValueChanged.RemoveAllListeners ();
		save_button.onClick.RemoveAllListeners ();
	}

	void OnClick(){
		saveName = InputF.text;
		StartSave ();
	}
	void OnDropDown(){
		populate ();
		List<string> opts = new List<string> (PlayerManager.GetSavedGames());
		if (opts.Count == 0) {
			return;
		}
		saveName = opts[dropdown.value];
		InputF.text = saveName;
	}
	
	void Update () {
		//handle escape key 
		if(Input.GetKeyDown(KeyCode.Escape)) {
			CancelSave();
		}
		//handle enter key in confirmation dialog
		if(Input.GetKeyDown(KeyCode.Return)) {
			SaveGame();
		}
	}

	private void StartSave() {
		//prompt for override of name if necessary
		 SaveGame();
	}

	private void CancelSave() {
		MainMenu.SetActive (true);
		this.gameObject.SetActive (false);
	}

	private void SaveGame() {
		SaveManager.SaveGame (saveName);
		ResourceManager.LevelName = saveName;
		this.gameObject.SetActive (false);
		MainMenu.SetActive (true);
	}
}

