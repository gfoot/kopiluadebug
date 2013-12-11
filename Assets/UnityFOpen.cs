using UnityEngine;
using System.Collections.Generic;
using KopiLua;
using System;
using System.IO;

public static class UnityFOpen
{
	public static string ResourceRootPath = "";
	
	private static Dictionary<string, byte[]> _fileSystem = new Dictionary<string, byte[]>();
	
	// MemoryStream-like stream object for writing to files - the main
	// distinction is that when the stream is disposed it bakes the
	// data into the File object.
	private class FileMemoryStream : MemoryStream
	{
		public FileMemoryStream(string filename, bool truncate, bool append)
		{
			_filename = filename;
			//_stream = new MemoryStream();
			
			if (!truncate && _fileSystem.ContainsKey(filename))
			{
				var data = _fileSystem[filename];
				Write(data, 0, data.Length);
				if (!append)
					Seek(0, SeekOrigin.Begin);
			}
		}
		
		protected override void Dispose(bool disposing)
		{
			if (!disposing || _disposed)
				return;
			
			_disposed = true;
			
			_fileSystem[_filename] = this.ToArray();
		}
		
		private string _filename;
		private bool _disposed;
	}
	
	public static Stream FOpen(Lua.CharPtr cFilename, Lua.CharPtr cMode)
	{
		string filename = "/" + cFilename.ToString();
		filename = filename.Replace("\\", "/").Replace("/./", "/");
		filename = filename.Substring(1); // remove the leading slash again

		bool read = true;
		bool write = false;
		bool truncate = false;
		bool startAtEnd = false;
		
		if (cMode.chars.Length != 0)
		{
			int pos = 0;
			switch (cMode.chars[pos])
			{
			case 'r':
				break;
			case 'w':
				read = false;
				write = true;
				truncate = true;
				break;
			case 'a':
				read = false;
				write = true;
				startAtEnd = true;
				break;
			default:
				throw new ArgumentException(string.Format("Bad mode character '{0}' at position {1} in mode '{2}'", cMode.chars[pos], pos, cMode.ToString()));
			}
			++pos;
			
			if (cMode.chars[pos] == '+')
			{
				read = true;
				write = true;
				++pos;
			}
			
			if (cMode.chars[pos] == 'b')
			{
				// ignore
				++pos;
			}

			if (cMode.chars[pos] != '\0')
			{
				throw new ArgumentException(string.Format("Bad mode character '{0}' at position {1} in mode '{2}'", cMode.chars[pos], pos, cMode.ToString()));
			}
		}
		
		// Try to populate from Unity Resources, but don't bother if we're truncating
		if (!truncate && !_fileSystem.ContainsKey(filename))
			PopulateFromResource(filename);
		
		// If we're not writing, we just use a read-only MemoryStream to access the data, allowing simultaneous reads
		if (!write)
		{
			if (!_fileSystem.ContainsKey(filename))
			{
				throw new FileNotFoundException(string.Format("File not found - {0}", filename));
			}
			
			return new MemoryStream(_fileSystem[filename], false);
		}
		
		// Since we're writing we need to use a fancy derivative of MemoryStream that knows how to bake the data into 
		// the filesystem when it is disposed
		var stream = new FileMemoryStream(filename, truncate, startAtEnd);
		return stream;
	}
		
	private static bool PopulateFromResource(string filename)
	{
		string path = ResourceRootPath + (ResourceRootPath != "" ? "/" : "") + filename;
		var r = Resources.Load(path);
		//Debug.Log(string.Format("Load '{0}': result {1}", path, r));
		
		if (!(r is TextAsset))
			return false;
		
		var bytes = ((TextAsset)r).bytes;
		Resources.UnloadAsset(r);
		
		_fileSystem[filename] = bytes;
		return true;
	}
}
