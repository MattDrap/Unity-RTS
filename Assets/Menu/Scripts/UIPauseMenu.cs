using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using RTS;
using UnityEngine.UI;
public class UIPauseMenu : MonoBehaviour
{

	private Player player;
	public GameObject LoadPanel;
	public GameObject SavePanel;

	public Button loadButton;
	public Button saveButton;
	public Button resumeButton;
	public Button exitButton;
	public Button mainMenuButton;

	protected void Start () {
		player = transform.root.GetComponent< Player >();
		loadButton.onClick.AddListener (delegate {
			LoadGame();
		});
		saveButton.onClick.AddListener (delegate {
			SaveGame();
		});
		resumeButton.onClick.AddListener (delegate {
			Resume();
		});
		exitButton.onClick.AddListener (delegate {
			ExitGame();
		});
		mainMenuButton.onClick.AddListener (delegate {
			ReturnToMainMenu();	
		});
	}
	void OnEnable(){
		Cursor.visible = true;
	}
	void Destroy(){
		loadButton.onClick.RemoveAllListeners ();
		saveButton.onClick.RemoveAllListeners ();
		resumeButton.onClick.RemoveAllListeners ();
		exitButton.onClick.RemoveAllListeners ();
	}

	void Update () {
		if(Input.GetKeyDown(KeyCode.Escape)) Resume();
	}

	private void Resume() {
		Time.timeScale = 1.0f;
		HideCurrentMenu ();
		if (player) {
			player.GetComponent< PlayerUserInput > ().enabled = true;
			player.hud.gameObject.SetActive(true);
		}
		Cursor.visible = false;
		ResourceManager.MenuOpen = false;
	}
	private void ReturnToMainMenu(){
		ResourceManager.LevelName = "";
		SceneManager.LoadScene ("MainMenu");
		Cursor.visible = true;
	}
	private void SaveGame() {
		HideCurrentMenu ();
		SavePanel.SetActive (true);
	}
	protected void HideCurrentMenu () {
		this.gameObject.SetActive (false);
	}
	protected void ExitGame() {
		Application.Quit();
	}
	protected void LoadGame() {
		HideCurrentMenu();
		LoadPanel.SetActive (true);
	}
}

