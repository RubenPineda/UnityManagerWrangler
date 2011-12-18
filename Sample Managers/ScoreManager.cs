using UnityEngine;
using System.Collections;

public class ScoreManager : ManagerSingleton<ScoreManager>
{
	public int Score {
		get { return PlayerPrefs.GetInt ("Score"); }
		set { PlayerPrefs.SetInt ("Score", value); }
	}
	
	public void Reset ()
	{
		Score = 0;
	}
}
