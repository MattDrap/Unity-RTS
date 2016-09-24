using UnityEngine;
using System.Collections.Generic;
using RTS;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class UIManager : MonoBehaviour
{
	
	public GameObject ChangePlayerPanel;
	public GameObject GOtext;
	Text text;
	void Start(){
		Cursor.visible = true;
		text = GOtext.GetComponent<Text> ();
		PlayerManager.Load ();
		if (PlayerManager.GetPlayerName () == "") {
			//no player yet selected so enable SetPlayerMenu
			this.gameObject.SetActive(false);
			ChangePlayerPanel.SetActive (true);
		}
	}
	void OnGUI(){
		text.text = "Welcome " + PlayerManager.GetPlayerName ();
	}

	public void NewGameClicked(){
		ResourceManager.MenuOpen = false;
		SceneManager.LoadScene("Map");
		//makes sure that the loaded level runs at normal speed
		Time.timeScale = 1.0f;
	}
	public void ExitApplication(){
		#if UNITY_EDITOR
		EditorApplication.Exit(0);
		#else
		Application.Quit ();
		#endif
	}
}

