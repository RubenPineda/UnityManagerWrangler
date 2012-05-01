using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;


/// <summary>
/// Loads and executes Manager asset files.
/// </summary>
/// 
/// <remarks>
/// Attach ManagerWrangler to an object in the scene and set script execution order to run ManagerWrangler first.
/// On Awake, ManagerWrangler will load all <see cref="Manager"/> asset files stored within the <b>Resources/Managers</b> folder.
/// 
/// ManagerWrangler provides the necessary infrastructure for Manager scripts
/// to operate. ManagerWrangler reimplements Unity's coroutine scheduler and event invocation
/// (Start, Update, etc) which allows Manager scripts to work similarly to MonoBehaviours.
/// 
/// Unity methods which are forwarded by ManagerWrangler to Manager scripts:
///   - Start
///   - Update
///   - FixedUpdate
///   - LateUpdate
///   - OnLevelWasLoaded
///   - OnApplicationPause
///   - OnApplicationFocus
///   - OnApplicaitonQuit
///   - OnGUI
///   - OnPreCull
///   - OnPreRender
///   - OnPostRender
///   - OnRenderImage
///   - OnPlayerConnected
///   - OnPlayerDisconnected
///   - OnServerInitialized
///   - OnConnectedToServer
///   - OnDisconnectedFromServer
///   - OnFailedToConnect
///   - OnFailedToConnectToMasterServer
///   - OnMasterServerEvent
///   - OnNetworkInstantiate
///   - OnSerializeNetworkView
/// 
/// Methods which Unity calls directly on Manager scripts:
///   - Awake
///   - OnEnable
///   - OnDisable
///   - OnDestroy
/// 
/// See Unity's documentation on MonoBehavior for more information.
/// </remarks>
public class ManagerWrangler : MonoBehaviour {
	
	/// <summary>
	/// Singleton instance.
	/// </summary>
	public static ManagerWrangler Instance { get; private set; }
	
	private Manager[] managers;
	private List<CoroutineData> routines = new List<CoroutineData> ();
	
	private static bool initialized = false;


	void Awake ()
	{
		// Self-destruct if more than one ManagerWrangler exists
		if (initialized == true) {
			Destroy (this);
			Destroy (gameObject);
			return;
		}
		DontDestroyOnLoad (gameObject);
		gameObject.hideFlags = HideFlags.NotEditable;
		initialized = true;
		Instance = this;
		// Each object's Awake and OnEnable methods are automatically called when
		// the ScriptableObject is instantiated
		managers = Manager.GetManagers ();
		
		StartCoroutine (CoroutineUpdatePump ());
		StartCoroutine (CoroutineFixedUpdatePump ());
	}
	
	
	#region Unity Event Forwarders
	
	void OnGUI ()
	{
		if (!Application.isPlaying)
			return;
		InvokeInManagers ("OnGUI");
	}
	
	void Start () { InvokeInManagers ("Start"); }
	
	void Update () { InvokeInManagers ("Update"); }
	
	void FixedUpdate () { InvokeInManagers ("FixedUpdate"); }

	void LateUpdate () { InvokeInManagers ("LateUpdate"); }

	void OnLevelWasLoaded (int level) { InvokeInManagers ("OnLevelWasLoaded", level); }
	
	void OnApplicationPause (bool pause) { InvokeInManagers ("OnApplicationPause", pause); }
	
	void OnApplicationFocus (bool focus) { InvokeInManagers ("OnApplicationFocus", focus); }
	
	void OnApplicationQuit () { InvokeInManagers ("OnApplicationQuit"); }
	
	void OnPreCull () { InvokeInManagers ("OnPreCull"); }
	
	void OnPreRender () { InvokeInManagers ("OnPreRender"); }
	
	void OnPostRender () { InvokeInManagers ("OnPostRender"); }
	
	void OnRenderImage (RenderTexture source, RenderTexture destination) { InvokeInManagers ("OnRenderImage", source, destination); }
	
	void OnPlayerConnected (NetworkPlayer player) { InvokeInManagers ("OnPlayerConnected", player); }
	
	void OnPlayerDisconnected (NetworkPlayer player) { InvokeInManagers ("OnPlayerDisconnected", player); }
	
	void OnServerInitialized () { InvokeInManagers ("OnServerInitialized"); }
	
	void OnConnectedToServer () { InvokeInManagers ("OnConnectedToServer"); }
	
	void OnDisconnectedFromServer (NetworkDisconnection mode) { InvokeInManagers ("OnDisconnectedFromServer", mode); }
	
	void OnFailedToConnect (NetworkConnectionError error) { InvokeInManagers ("OnFailedToConnect", error); }
	
	void OnFailedToConnectToMasterServer (NetworkConnectionError error) { InvokeInManagers ("OnFailedToConnectToMasterServer", error); }

	void OnMasterServerEvent (MasterServerEvent msEvent) { InvokeInManagers ("OnMasterServerEvent", msEvent); }

	void OnNetworkInstantiate (NetworkMessageInfo info) { InvokeInManagers ("OnNetworkInstantiate", info); }
	
	void OnSerializeNetworkView (BitStream stream, NetworkMessageInfo info) { InvokeInManagers ("OnSerializeNetworkView", stream, info); }
	
	#endregion
	
	
	#region Coroutines
	
	// FIXME better coroutine implementation here: http://entitycrisis.blogspot.com/2010/10/custom-coroutines-in-unity3d.html
	
	private class CoroutineData
	{
		public CoroutineData (IEnumerator routine) : this(routine, null)
		{
		}
		
		public CoroutineData (IEnumerator routine, object args)
		{
			this.enumerator = routine;
			this.args = args;
			
			Match match = Regex.Match (routine.GetType().Name, "<(.+)>");
			this.name = match.Groups[1].Value;
		}
		
		public readonly string name;
		public readonly IEnumerator enumerator;
		public readonly object args;
		public float? timeRemaining = null;
		public bool reachedEnd = false;
		public object lastYielded = null;
	}
	
	
	/// <summary>
	/// Custom coroutine scheduler
	/// </summary>
	private IEnumerator CoroutineUpdatePump ()
	{
		while (true) {
			for (int i = 0; i < routines.Count; i++) {
				DoUpdateCoroutine (routines[i]);
			}
			yield return new WaitForEndOfFrame ();
		}
	}
	
	
	private void DoUpdateCoroutine (CoroutineData r)
	{
		bool ended;
		
		// FIXME too much copy/pasting between blocks
		
		// WaitForSeconds
		if (r.enumerator.Current is WaitForSeconds) {
			// Wait
			if (r.timeRemaining == null) {
				WaitForSeconds s = (WaitForSeconds)r.enumerator.Current;
				FieldInfo f = s.GetType ().GetField ("m_Seconds", BindingFlags.NonPublic | BindingFlags.Instance);
				// FIXME accumulates error over time, slightly slower than a normal coroutine
				r.timeRemaining = (float)f.GetValue (s) - Time.deltaTime;
				ended = false;
				return;
			}
			else if (r.timeRemaining <= 0) {
				r.timeRemaining = null;
				ended = r.enumerator.MoveNext ();
				if (r.reachedEnd == true && r.lastYielded == r.enumerator.Current) {
					routines.Remove (r);
					return;
				}
			}
			else {
				r.timeRemaining -= Time.deltaTime;
				return;
			}
		}
		// WaitForEndOfFrame
		else if (r.enumerator.Current is WaitForEndOfFrame) {
			ended = r.enumerator.MoveNext ();
			if (r.reachedEnd == true && r.lastYielded == r.enumerator.Current) {
				routines.Remove (r);
				return;
			}
		}
		// Coroutine
		else if (r.enumerator.Current is Coroutine) {
			// FIXME implement this! How do we figure out if a coroutine has finished? Bypass for now.
			Debug.LogWarning("Yielding on a Coroutine is not yet implemented in ManagerWrangler");
			ended = r.enumerator.MoveNext ();
			if (r.reachedEnd == true && r.lastYielded == r.enumerator.Current) {
				routines.Remove (r);
				return;
			}
		}
		else {
			ended = r.enumerator.MoveNext ();
			if (r.reachedEnd == true && r.lastYielded == r.enumerator.Current) {
				routines.Remove (r);
				return;
			}
		}
				
		if (ended == true) {
			r.reachedEnd = true;
			r.lastYielded = r.enumerator.Current;
		}
	}
	
	
	private IEnumerator CoroutineFixedUpdatePump ()
	{
		while (true) {
			for (int i = 0; i < routines.Count; i++) {
				DoFixedUpdateCoroutine (routines[i]);
			}
			yield return new WaitForFixedUpdate ();
		}
	}
	
	
	private void DoFixedUpdateCoroutine (CoroutineData r)
	{
		if (r.enumerator.Current is WaitForFixedUpdate) {
			
			// FIXME
			Debug.Log ("FixedUpdate coroutines don't yet work correctly");
			
			bool ended = r.enumerator.MoveNext ();
			if (r.reachedEnd == true && r.lastYielded == r.enumerator.Current) {
				routines.Remove (r);
				return;
			}
			
			if (ended == true) {
				r.reachedEnd = true;
				r.lastYielded = r.enumerator.Current;
			}
		}
	}
	

	
	internal void StartPseudoCoroutine (object sender, string methodName)
	{
		StartPseudoCoroutine (sender, methodName, null);
	}
	
	
	internal void StartPseudoCoroutine (object sender, string methodName, object argument)
	{
		MethodInfo m = GetCoroutineMethod (sender, methodName);
		if (m == null) {
			Debug.LogError ("Can't start coroutine: IEnumerator named " + methodName + " doesn't exist in " + sender.GetType ().Name);
			return;
		}
		
		IEnumerator routine = (IEnumerator)InvokeMember (sender, methodName, argument == null ? null : new object[] { argument });
		StartPseudoCoroutine (sender, routine);
	}
	
	
	internal void StartPseudoCoroutine (object sender, IEnumerator routine)
	{
		CoroutineData r = new CoroutineData (routine);
		routines.Add (r);
	}
	
	
	internal void StopPseudoCoroutine (object sender, string methodName)
	{
		MethodInfo m = GetCoroutineMethod (sender, methodName);
		if (m == null) {
			Debug.LogError ("Can't stop coroutine: " + methodName + " doesn't exist in " + sender.GetType ().Name);
			return;
		}
		
		for (int i = 0; i < routines.Count; i++) {
			if (routines[i].name == methodName) {
				routines.Remove (routines[i]);
			}
		}
	}
	
	
	/// <summary>
	/// Invoke a given method by name in all available manager scripts.
	/// </summary>
	/// <remarks>
	/// This is used for manually invoking standard Unity events, such as Start and Update,
	/// in scripts that do not inherit from MonoBehaviour.
	/// </remarks>
	void InvokeInManagers (string methodName)
	{
		InvokeInManagers (methodName, null);
	}
	
	/// <summary>
	/// Invoke a given method by name in all available manager scripts.
	/// </summary>
	void InvokeInManagers (string methodName, params object[] parameters)
	{
		if (managers == null)
			return;
		foreach (Manager m in managers)
			InvokeMember (m, methodName, parameters);
	}
	
	
	private object InvokeMember (object script, string methodName)
	{
		return InvokeMember (script, methodName, null);
	}
		
	/// <summary>
	/// Invoke a method by name in a given script.
	/// </summary>
	private object InvokeMember (object script, string methodName, params object[] parameters)
	{
		MethodInfo m = script.GetType ().GetMethod (methodName, BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
		if (m != null) {
			// FIXME allow invocation of methods without parameters. Just ignore the params.
			return m.Invoke (script, parameters);
		}
		return null;
	}
	
	
	/// <summary>
	/// Retrieve a method in a given script.
	/// </summary>
	private MethodInfo GetCoroutineMethod (object script, string methodName)
	{
		MethodInfo m = script.GetType ().GetMethod (methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		if (m == null)
			return null;
		if (m.ReturnType != typeof(IEnumerator))
			return null;
		return m;
	}
	
	#endregion
	
}
