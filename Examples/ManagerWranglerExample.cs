using UnityEngine;
using System.Collections;
using ManagerWrangler;

public class ManagerWranglerExample : MonoBehaviour
{
	void Start ()
	{
		// Print all available managers
		var allManagers = Manager.GetAllManagers ();
		foreach (var manager in allManagers) {
			Debug.Log (manager.GetType ());
		}

		// Set the score
		var scoreManager = Manager.FindManager<ScoreManager> ();
		scoreManager.Score = 3;
	}
}
