using UnityEngine;
using System.Collections;
using ManagerWrangler;

public sealed class SoundManager : ManagerSingleton<SoundManager>
{
	private bool _audible = true;
	/// <summary>
	/// Enable/disable sound effects.
	/// </summary>
	public bool Audible {
		get {
			return _audible;
		}
		set {
			_audible = value;
			// Our music AudioSources are set to ignore the global AudioListener volume,
			// so we can use this to universally control sound effects volume.
			// http://forum.unity3d.com/threads/9513-Ability-to-group-audio-sources-into-channels
			AudioListener.volume = value ? 1 : 0;
		}
	}
	
	/// <summary>
	/// Play a sound effect.
	/// </summary>
	/// <param name="resource">
	/// Path to AudioClip in Resources folder.
	/// </param>
	public void Play (string resource)
	{
		Play (resource, 1);
	}
	
	/// <summary>
	/// Play a sound effect at a given volume.
	/// </summary>
	/// <param name="resource">
	/// Path to AudioClip in Resources folder.
	/// </param>
	public void Play (string resource, float volume)
	{
		Play((AudioClip)Resources.Load ("Sounds/" + resource), volume);
	}
	
	/// <summary>
	/// Play a sound effect AudioClip.
	/// </summary>
	public void Play (AudioClip clip)
	{
		Play (clip, 1);
	}
	
	/// <summary>
	/// Play a sound effect AudioClip at a given volume.
	/// </summary>
	public void Play (AudioClip clip, float volume)
	{
		if (!Audible)
			return;
		AudioSource.PlayClipAtPoint (clip, Vector3.zero, volume);
	}
}
