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

		Check(Lua.luaL_loadfile(_lua, "main.lua"));
	    Check(Lua.lua_pcall(_lua, 0, -1, 0));
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
    }
}
