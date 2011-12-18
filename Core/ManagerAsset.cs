using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// Singleton manager class.
/// </summary>
/// 
/// <remarks>
/// When subclassing ManagerSingleton\<T\>, use the new subclass type as the
/// base type's generic parameter. This will allow the base class's <see cref="Instance"/> property
/// to use the new subclass type as its return type.
/// 
/// Example:
/// 
/// <code>
/// public class ScoreManager : ManagerSingleton<ScoreManager>
/// </code>
/// 
/// Access a subclass's public members through <see cref="Instance"/>.
/// 
/// Example:
/// 
/// <code>
/// ScoreManager.Instance.AddPoints (3);
/// </code>
/// 
/// If your subclass is fully self-contained and does have any public non-static members,
/// it is best to inherit from <see cref="Manager"/> instead.
/// 
/// ManagerSingleton\<T\> objects must be packaged into a <tt>.asset</tt> file and placed
/// into the folder <b>Resources/Managers</b> within the current Unity project folder.
/// These files will then be loaded and executed by <see cref="ManagerWrangler"/>.
/// 
/// <seealso cref="Manager"/>
/// </remarks>
public abstract class ManagerSingleton<T> : Manager
	where T : ManagerSingleton<T>
{
	private static T _instance;
	public static T Instance {
		get {
			if (_instance != null)
				return _instance;
			foreach (Manager manager in GetManagers ()) {
				if (manager is T) {
					_instance = (T)manager;
					DontDestroyOnLoad (_instance);
					return _instance;
				}
			}
			Debug.LogError ("Missing Manager: " + typeof(T).ToString () + " asset must exist in the folder Resources/Managers/");
			return null;
		}
	}
}


/// <summary>
/// Non-singleton manager class.
/// </summary>
/// <remarks>
/// Scripts that derive from Manager must be instantiated into <tt>.asset</tt> files and stored in
/// the <b>Resources/Managers</b> folder.
/// 
/// All Managers within the Managers resource folder will be loaded and executed by <see cref="ManagerWrangler"/>.
/// </remarks>
public abstract class Manager : ScriptableObject
{
	private static Manager[] managers;
	
	
	#region Coroutines
	
	/// <summary>
	/// Reimplementation of MonoBehaviour's StartCoroutine method.
	/// </summary>
	protected void StartCoroutine (IEnumerator routine)
	{
		if (ManagerWrangler.Instance != null)
			ManagerWrangler.Instance.StartPseudoCoroutine (this, routine);
	}
	
	/// <summary>
	/// Reimplementation of MonoBehaviour's StartCoroutine method.
	/// </summary>
	protected void StartCoroutine (string methodName)
	{
		if (ManagerWrangler.Instance != null)
			ManagerWrangler.Instance.StartPseudoCoroutine (this, methodName);
	}
	
	/// <summary>
	/// Reimplementation of MonoBehaviour's StartCoroutine method.
	/// </summary>
	protected void StartCoroutine (string methodName, object argument)
	{
		if (ManagerWrangler.Instance != null)
			ManagerWrangler.Instance.StartPseudoCoroutine (this, methodName, argument);
	}
	
	/// <summary>
	/// Reimplementation of MonoBehaviour's StopCoroutine method.
	/// </summary>
	protected void StopCoroutine (string methodName)
	{
		if (ManagerWrangler.Instance != null)
			ManagerWrangler.Instance.StopPseudoCoroutine (this, methodName);
	}
	
	#endregion
	
	
	#region Static Methods
	
	/// <summary>
	/// Return all Manager asset files stored within <b>Resources/Managers</b>
	/// </summary>
	/// <remarks>
	/// GetManagers will use <see cref="managerResourceFolder"/> as its search location.
	/// </remarks>
	public static Manager[] GetManagers ()
	{
		if (Manager.managers != null)
			return Manager.managers;
		// Return all managers inside of "Resources/Managers"
		UnityEngine.Object[] managers = Resources.LoadAll ("Managers", typeof(Manager));
		Manager.managers = (Manager[])System.Array.ConvertAll (managers, item => (Manager)item);
		return Manager.managers;
	}
	
	/// <summary>
	/// Find a loaded Manager script by type.
	/// </summary>
	public static T FindManager<T> ()
		where T : Manager
	{
		foreach (Manager m in GetManagers()) {
			if (m as T)
				return (T)m;
		}
		return null;
	}
	
	/// <summary>
	/// Find a loaded Manager script by type.
	/// </summary>
	public static Manager FindManager (System.Type type)
	{
		foreach (Manager m in GetManagers ()) {
			if (m.GetType () == type)
				return m;
		}
		return null;
	}
	
	#endregion
}
