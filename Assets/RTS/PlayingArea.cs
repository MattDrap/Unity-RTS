using UnityEngine;
using System.Collections;

public class PlayingArea
{
	public PlayingArea(){
		area = new Rect(0.0f,0.0f,0.0f,0.0f);
		not_playable_top = new Rect (0.0f, 0.0f, 0.0f, 0.0f);
		not_playable_bottom = new Rect (0.0f, 0.0f, 0.0f, 0.0f);
	}
	public Rect area;
	public Rect not_playable_top;
	public Rect not_playable_bottom;
}

