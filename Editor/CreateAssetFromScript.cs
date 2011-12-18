using System;
using UnityEngine;
using UnityEditor;
using System.Collections;

public class CreateAssetFromScript : Editor {
	
	[MenuItem("Assets/Create Asset From Manager Script", false, 10000)]
	public static void CreateManager ()
	{
		ScriptableObject asset = ScriptableObject.CreateInstance (Selection.activeObject.name);
		AssetDatabase.CreateAsset (asset, String.Format ("Assets/{0}.asset", Selection.activeObject.name));
		EditorUtility.FocusProjectWindow ();
		Selection.activeObject = asset;
	}

	[MenuItem("Assets/Create Asset From Manager Script", true, 10000)]
	public static bool CreateManagerValidate ()
	{
		if (Selection.activeObject.GetType () != typeof(MonoScript))
			return false;
		MonoScript script = (MonoScript)Selection.activeObject;
		var scriptClass = script.GetClass ();
		if (scriptClass == null)
			return false;
		return typeof(Manager).IsAssignableFrom (scriptClass.BaseType);
	}
}
