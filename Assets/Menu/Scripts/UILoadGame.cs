using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using RTS;

public class UILoadGame: MonoBehaviour
{
	public Button load_button;
	public GameObject Dropdown;
	public GameObject MainMenu;
	Dropdown dropdown;
	protected void Awake(){
		dropdown = Dropdown.GetComponent<Dropdown> ();
		load_button.onClick.AddListener( delegate {
			OnClick();	
		});
		populate ();
	}

	void Update(){
		if (Input.GetKey (KeyCode.Escape)) {
			MainMenu.SetActive (true);
			this.gameObject.SetActive (false);
		}
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
		load_button.onClick.RemoveAllListeners ();
	}

	void OnClick(){
		List<string> opts = new List<string> (PlayerManager.GetSavedGames());
		if (opts.Count == 0)
			return;
		string newLevel = opts[dropdown.value];
		if(newLevel!="") {
			ResourceManager.LevelName = newLevel;
			if(SceneManager.GetActiveScene().name != "BlankMap1") SceneManager.LoadScene("BlankMap1");
			else if(SceneManager.GetActiveScene().name != "BlankMap2") SceneManager.LoadScene("BlankMap2");
			//makes sure that the loaded level runs at normal speed
			Time.timeScale = 1.0f;
		}
	}
}

