using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using RTS;
public class UIResultMenu : MonoBehaviour
{
	public AudioClip clickSound;
	public float clickVolume = 1.0f;
	public Text text;
	public Button button;

	private Player winner;
	private VictoryCondition metVictoryCondition;

	void Start () {
		List< AudioClip > sounds = new List< AudioClip >();
		List< float > volumes = new List< float >();
		sounds.Add(clickSound);
		volumes.Add (clickVolume);
		button.onClick.AddListener (delegate {
			OnMenuClick();
		});
		Cursor.visible = true;
		//audioElement = new AudioElement(sounds, volumes, "ResultsScreen", null);
	}

	void OnMenuClick(){
		ResourceManager.LevelName = "";
		SceneManager.LoadScene ("MainMenu");
		Cursor.visible = true;
	}

	void Destroy(){
		button.onClick.RemoveAllListeners ();
	}

	private void PlayClick() {
		//if(audioElement != null) audioElement.Play(clickSound);
	}

	public void OnEnable(){
		string message = "Game Over";
		if(winner) message = "Congratulations " + winner.username + "! You have won by " + metVictoryCondition.GetDescription();
		text.text = message;
	}

	public void SetMetVictoryCondition(VictoryCondition victoryCondition) {
		if(!victoryCondition) return;
		metVictoryCondition = victoryCondition;
		winner = metVictoryCondition.GetWinner();
	}
}


