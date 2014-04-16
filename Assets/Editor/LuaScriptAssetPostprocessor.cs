using UnityEngine;
using UnityEditor;
using System.Collections;

public class LuaScriptAssetPostprocessor : AssetPostprocessor
{
	const string SourcePrefix = "Assets/LuaScripts/";
	const string DestPrefix = "Assets/Resources/LuaScripts/";

	static void OnPostprocessAllAssets(
		string[] importedAssets,
		string[] deletedAssets,
		string[] movedAssets,
		string[] movedFromAssetPaths
	)
	{
		foreach (var path in importedAssets)
		{
			if (path.StartsWith(SourcePrefix) && path.EndsWith(".lua"))
			{
				var newPath = DestPrefix + path.Substring(SourcePrefix.Length) + ".txt";
				Debug.Log(string.Format("{0} => {1}", path, newPath));
				var result = AssetDatabase.CopyAsset(path, newPath);
				Debug.Log(result);
				AssetDatabase.ImportAsset(newPath);
			}
		}
	}
}
