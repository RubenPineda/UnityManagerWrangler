using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace ManagerWrangler
{
	[CustomEditor(typeof(ManagerWrangler))]
	public class ManagerWranglerEditor : Editor
	{
		private Object[] assets;
		
		private void OnEnable ()
		{
			assets = Resources.LoadAll ("Managers", typeof(Manager));
		}
		
		public override void OnInspectorGUI ()
		{
			foreach (var item in assets) {
				if (item as Manager) {
					Manager manager = (Manager)item;
					if (GUILayout.Button (manager.name)) {
						EditorGUIUtility.PingObject (manager);
					}
				}
			}
		}
	}
}