using UnityEngine;
using UnityEditor;
using System.Collections;

public class LuaScriptAssetPostprocessor : AssetPostprocessor
{
	const string SourcePrefix = "Assets/LuaScripts/";
	const string DestPrefix = "Assets/Resources/LuaScripts/";

	public static void OnPostprocessAllAssets(
		string[] importedAssets,
		string[] deletedAssets,
		string[] movedAssets,
		string[] movedFromAssetPaths
	)
	{
		foreach (var path in deletedAssets)
		{
			Unimport(path);
		}
		
		foreach (var path in movedFromAssetPaths)
		{
			Unimport(path);
		}

		foreach (var path in importedAssets)
		{
			Import(path);
		}
		foreach (var path in movedAssets)
		{
			Import(path);
		}
	}

	static string GetTargetPath(string path)
	{
		if (path.StartsWith (SourcePrefix) && path.EndsWith (".lua")) {
			return DestPrefix + path.Substring (SourcePrefix.Length) + ".txt";
		}
		return null;
	}

	static void Import(string path)
	{
		var newPath = GetTargetPath(path);
		if (newPath == null)
			return;

		AssetDatabase.DeleteAsset(newPath);
		AssetDatabase.CopyAsset(path, newPath);
		AssetDatabase.ImportAsset(newPath);
	}

	static void Unimport(string path)
	{
		var newPath = GetTargetPath(path);
		if (newPath == null)
			return;

		AssetDatabase.DeleteAsset(newPath);
	}
}
