using System.IO;
using System.Text;
using UnityEngine;

using KopiLua;

public class LuaDebugTest : MonoBehaviour
{
    private Lua.lua_State _lua;

    public void Start()
    {
	    KopiLua.Lua.fopenDelegate = UnityFOpen.FOpen;
	    UnityFOpen.ResourceRootPath = "LuaScripts";

	    _lua = Lua.luaL_newstate();
	    Lua.luaL_openlibs(_lua);
		LuaSocketLibrary.Init(_lua);

		Lua.lua_pushcfunction(_lua, Trace);
		Lua.lua_setglobal(_lua, "TRACE");

		// KopiLua doesn't implement os.exit properly - it seems to just hang - so override it with something that doesn't.
		Check(Lua.luaL_loadstring(_lua, "os.exit = function() end"));
		Check(Lua.lua_pcall(_lua, 0, 0, 0));

		// Activate mobdebug - causes the debugger to pause on the next statement
		Check(Lua.luaL_loadstring(_lua, "require('mobdebug').start()"));
		Check(Lua.lua_pcall(_lua, 0, 0, 0));

		// Test debugging a loadstring
		// Doesn't seem to work :(
		//Check(Lua.luaL_loadstring(_lua, "xxx = 888"));
		//Check(Lua.lua_pcall(_lua, 0, 0, 0));

		Check(Lua.luaL_loadfile(_lua, "main.lua"));
	    Check(Lua.lua_pcall(_lua, 0, 0, 0));
	}

	public void OnApplicationQuit()
	{
		if (_lua != null)
		{
			// We need to shut down mobdebug when the application quits, otherwise the debugger won't detach
			Check(Lua.luaL_loadstring(_lua, "require('mobdebug').done()"));
			Check(Lua.lua_pcall(_lua, 0, 0, 0));
		}
	}

	public static int Trace(Lua.lua_State lua)
	{
		Lua.CharPtr cp = Lua.lua_tostring(lua, 1);
		using (var sw = File.AppendText("log.txt"))
			sw.Write(cp.chars);
		return 0;
	}

    void Check(int result)
    {
        if (result == 0) return;
        
        if (Lua.lua_gettop(_lua) == 0)
            Debug.LogError("no error message (empty stack)");
        else if (Lua.lua_isstring(_lua, -1) == 0)
            Debug.LogError("no error message (top of stack is not string)");
        else
            Debug.LogError(Lua.lua_tostring(_lua, -1));
    }

    private readonly Encoding _encoding = new UTF8Encoding();

    public void OnGUI()
    {
        var ms = (MemoryStream)Lua.stdout;
        var bytes = ms.ToArray();
        var chars = new char[_encoding.GetCharCount(bytes)];
        _encoding.GetDecoder().GetChars(bytes, 0, bytes.Length, chars, 0);

        var s = new string(chars);
        var lines = s.Split('\n');
        for (var i = lines.Length - 10; i < lines.Length; ++i)
            if (i >= 0)
                GUILayout.Label(lines[i]);

		if (GUILayout.Button("Do Lua stuff"))
		{
			Lua.lua_getglobal(_lua, "do_lua_stuff");
			Check (Lua.lua_pcall(_lua, 0, 0, 0));
		}
    }
}
