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

	    //		DoCode(Code);
	}

    private float GetValueFromLua(string name)
    {
        Lua.lua_getfield(_lua, Lua.LUA_GLOBALSINDEX, name);
        return (float)Lua.lua_tonumber(_lua, -1);
    }

    private bool GetBoolFromLua(string name)
    {
        Lua.lua_getfield(_lua, Lua.LUA_GLOBALSINDEX, name);
        return Lua.lua_toboolean(_lua, -1) != 0;
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

    public void FixedUpdate()
	{
		return;

	    Lua.lua_getfield(_lua, Lua.LUA_GLOBALSINDEX, "update");
        Check(Lua.lua_pcall(_lua, 0, -1, 0));
        
        if (Lua.lua_isstring(_lua, -1) != 0)
            Debug.Log(Lua.lua_tostring(_lua, -1));
        else
            Lua.lua_pop(_lua, 1);

        var heading = GetValueFromLua("heading");
        var posX = GetValueFromLua("pos_x");
        var posY = GetValueFromLua("pos_y");
        var pen = GetBoolFromLua("pen");
	}

    public static void DumpStack(Lua.lua_State L)
    {
        for (var i = -Lua.lua_gettop(L); i < 0; ++i)
        {
            var s = "?";
            var t = Lua.lua_type(L, i);
            switch (t)
            {
                case Lua.LUA_TSTRING:
                    s = Lua.lua_tostring(L, i).ToString();
                    break;
                case Lua.LUA_TBOOLEAN:
                    s = Lua.lua_toboolean(L, i) != 0 ? "true" : "false";
                    break;
                case Lua.LUA_TNUMBER:
                    s = Lua.lua_tonumber(L, i).ToString();
                    break;
            }
            Debug.Log(string.Format("{0}: {1} {2}", i, Lua.lua_typename(L, t), s));
        }
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
