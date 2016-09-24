using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour {

	const float MIN_PITCH = 0.75f;
	const float MAX_PITCH = 1.25f;
	// Use this for initialization
	void Start () {
		
	}
	public void PlayAudio(AudioClip clip, AudioSource source){
		source.Stop ();
		source.clip = clip;
		float randPitch = Random.Range (MIN_PITCH, MAX_PITCH);
		source.pitch = randPitch;
		source.Play ();
	}
	public void PlayLoopAudio(AudioClip clip, AudioSource source){
		source.Stop ();
		source.clip = clip;
		float randPitch = Random.Range (MIN_PITCH, MAX_PITCH);
		source.pitch = randPitch;
		source.loop = true;
		source.Play ();
	}
}
