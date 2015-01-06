using UnityEngine;
using System.Collections;
using ManagerWrangler;

public sealed class MusicManager : ManagerSingleton<MusicManager>
{
	#region Fields & Properties
	
	/// <summary>
	/// Master music volume, 0 to 1.
	/// </summary>
	public float masterVolume = 1;

	/// <summary>
	/// Filename of the default music clip, excluding file extension.
	/// </summary>
	public string defaultTrack = "Default Music";

	
	/// <summary>
	/// Properties for music fade coroutine.
	/// </summary>
	private struct FadeProperties
	{
		public AudioSource source;
		public float length;
		public float targetVolume;
		
		public FadeProperties (AudioSource source, float length, float targetVolume)
		{
			this.source = source;
			this.length = length;
			this.targetVolume = targetVolume;
		}
	}
	
	private bool _audible = true;
	/// <summary>
	/// Enable/disable music.
	/// </summary>
	public bool Audible {
		get {
			return _audible;
		}
		set {
			_audible = value;
			currentTrack.volume = masterVolume * SilenceMultiplier;
			previousTrack.volume = masterVolume * SilenceMultiplier;
		}
	}
	
	/// <summary>
	/// 0 mutes music, 1 uses masterVolume
	/// </summary>
	private int SilenceMultiplier {
		get { return Audible ? 1 : 0; }
	}
	
	/// <summary>
	/// Is music currently playing?
	/// </summary>
	public bool IsPlaying {
		get { return (!currentTrack.isPlaying && !previousTrack.isPlaying) ? false : true; }
	}
	
	/// <summary>
	/// The name of the currently playing AudioClip.
	/// </summary>
	public string CurrentClipName {
		get {
			if (currentTrack.clip == null)
				return null;
			else
				return currentTrack.clip.name;
		}
	}
	

	// Music AudioSources are buffered so that we can fade between two different tracks.
	private AudioSource currentTrack = null;
	private AudioSource previousTrack = null;
	
	#endregion
	
	
	#region Unity Events
	
	void Awake ()
	{
		// Create a music player game object with two audiosources
		GameObject go = new GameObject ("Music");
		go.hideFlags = HideFlags.HideAndDontSave;
		currentTrack = go.AddComponent<AudioSource> ();
		previousTrack = go.AddComponent<AudioSource> ();
		// ignoreListenerVolume allows the AudioListener to globally control
		// sound effects volume while music controls its own volume independently
		currentTrack.ignoreListenerVolume = true;
		previousTrack.ignoreListenerVolume = true;
	}
	
	#endregion
	
	
	#region Methods
	
	/// <summary>
	/// Play the default track defined in the MusicManager inspector.
	/// </summary>
	public void PlayDefault ()
	{
		Play (defaultTrack, 1, 0);
	}
	
	/// <summary>
	/// Play a music clip by name.
	/// </summary>
	/// <param name="resource">
	/// Path to AudioClip in Resources folder.
	/// </param>
	public void Play (string resource)
	{
		Play (resource, 1, 0);
	}
	
	/// <summary>
	/// Play a music clip by name at a given volume.
	/// </summary>
	/// <param name="resource">
	/// Path to AudioClip in Resources folder.
	/// </param>
	public void Play (string resource, float volume)
	{
		Play (resource, volume, 0);
	}
	
	public void Play (string resource, float volume, float crossFade)
	{
		Play (resource, volume, 0, true);
	}
	
	/// <summary>
	/// Play a music clip by name at a given volume, and cross fade if another track is currently playing.
	/// </summary>
	/// <param name="resource">
	/// Path to AudioClip in Resources folder.
	/// </param>
	public void Play (string resource, float volume, float crossFade, bool loop)
	{
		AudioClip clip = (AudioClip)Resources.Load ("Music/" + resource);
		Play (clip, volume, crossFade, loop);
	}
	
	/// <summary>
	/// Play a given music AudioClip.
	/// </summary>
	public void Play (AudioClip clip)
	{
		Play (clip, 1, 0, true);
	}
	
	/// <summary>
	/// Play a given music AudioClip at a specific volume.
	/// </summary>
	public void Play (AudioClip clip, float volume)
	{
		Play (clip, volume, 0, true);
	}
	
	/// <summary>
	/// Play a given music AudioClip at a specific volume with crossfade.
	/// </summary>
	public void Play (AudioClip clip, float volume, float crossFade)
	{
		Play (clip, volume, 0, true);
	}
	
	/// <summary>
	/// Play a given AudioClip at a specific volume, and cross fade if another track is currently playing.
	/// </summary>
	/// <param name="clip">
	/// AudioClip to play.
	/// </param>
	/// <param name="volume">
	/// Target volume.
	/// </param>
	/// <param name="crossFade">
	/// Crossfade length in seconds.
	/// </param>
	public void Play (AudioClip clip, float volume, float crossFade, bool loop)
	{
		// If no fading needs to be done, kill all tracks and begin playing new clip
		if (crossFade <= 0) {
			previousTrack.Stop ();
			currentTrack.Stop ();
			currentTrack.clip = clip;
			currentTrack.volume = volume * masterVolume * SilenceMultiplier;
			currentTrack.loop = loop;
			currentTrack.Play ();
			return;
		}
		
		// If the audio is already in transition, ignore the play request
		if (previousTrack.isPlaying) {
			return;
		}
		// If there is something already playing
		else if (currentTrack.isPlaying) {
			SwapSources();
			// Fade old audio out
			Fade (previousTrack, crossFade, 0);
			// Fade new audio in
			currentTrack.Stop();
			currentTrack.clip = clip;
			currentTrack.volume = 0;
			currentTrack.loop = loop;
			currentTrack.Play();
			Fade (currentTrack, crossFade, volume);
		}
	}
	
	/// <summary>
	/// Stop all music.
	/// </summary>
	/// <param name="fadeOut">
	/// Length of fade out in seconds. 0 will stop instantly.
	/// </param>
	public void Stop (float fadeOut)
	{
		StopCoroutine ("DoStop");
		StartCoroutine ("DoStop", new FadeProperties (currentTrack, fadeOut, 0));
	}
	
	private IEnumerator DoStop (FadeProperties settings)
	{
		// Wait for the audio to finish fading, then stop the audio
		yield return DoFade (settings);
		settings.source.Stop ();
	}
	
	/// <summary>
	/// Fade the volume of the currently playing track.
	/// </summary>
	/// <param name="length">
	/// The duration of the fade, in seconds.
	/// </param>
	/// <param name="targetVolume">
	/// Fade from the current volume to the target volume.
	/// </param>
	public void Fade (float length, float targetVolume)
	{
		Fade (currentTrack, length, targetVolume);
	}
	
	private void Fade (AudioSource source, float length, float targetVolume)
	{
		StopCoroutine ("DoFade");
		if (length <= 0) {
			source.volume = targetVolume * masterVolume * SilenceMultiplier;
			return;
		}
		StartCoroutine ("DoFade", new FadeProperties (source, length, targetVolume));		
	}
	
	private IEnumerator DoFade (FadeProperties settings)
	{
		yield return 0;
		float startTime = Time.time;
		float startVolume = settings.source.volume;
		float fadePosition = 0;
		while (Time.time < startTime + settings.length) {
			fadePosition += Time.deltaTime;
			settings.source.volume = Mathf.SmoothStep (startVolume, settings.targetVolume * masterVolume * SilenceMultiplier, fadePosition/settings.length);
			yield return 0;
		}
		// Stop playback if the audio has faded completely out
		if (settings.targetVolume <= 0) {
			settings.source.Stop ();
		}
	}
	
	#endregion
	
	
	/// <summary>
	/// Swaps the previousTrack and currentTrack AudioSources.
	/// </summary>
	private void SwapSources ()
	{
		AudioSource temp;
		temp = previousTrack;
		previousTrack = currentTrack;
		currentTrack = temp;
	}
	
}
