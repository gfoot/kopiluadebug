using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using KopiLua;
using UnityEngine;

public class LuaSocketLibrary : KopiLua.Lua
{
	static luaL_Reg[] funcs =
	{
		new luaL_Reg("connect", Connect),
		new luaL_Reg(null, null)
	};

	static luaL_Reg[] SocketFuncs =
	{
		new luaL_Reg("receive", SocketReceive),
		new luaL_Reg("send", SocketSend),
		new luaL_Reg("settimeout", SocketSetTimeout),
	};

	private static void NewClass(lua_State L, string classname, luaL_Reg[] methods)
	{
		luaL_newmetatable(L, classname); /* mt */
		/* create __index table to place methods */
		lua_pushstring(L, "__index");    /* mt,"__index" */
		lua_newtable(L);                 /* mt,"__index",it */ 
		/* put class name into class metatable */
		lua_pushstring(L, "class");      /* mt,"__index",it,"class" */
		lua_pushstring(L, classname);     /* mt,"__index",it,"class",classname */
		lua_rawset(L, -3);               /* mt,"__index",it */
		
		/* pass all methods that start with _ to the metatable, and all others
	     * to the index table */
		foreach (var func in methods)
		{
			lua_pushstring(L, func.name);
			lua_pushcfunction(L, func.func);
			lua_rawset(L, func.name[0] == '_' ? -5: -3);
		}
		lua_rawset(L, -3);               /* mt */
		lua_pop(L, 1);
	}

	public static void Init(lua_State L)
	{
		luaI_openlib(L, "socket", funcs, 0);
	
		NewClass(L, "Socket", SocketFuncs);

		_sockets = new Dictionary<int, Connection>();
	}

	private class Connection
	{
		public Socket Socket;
		public int Timeout;
	}

	private static Dictionary<int, Connection> _sockets;
	private static int _nextId;

	private static int Connect(Lua.lua_State L)
	{
		var host = luaL_checkstring(L, 1).ToString();
		var port = luaL_checkint(L, 2);

		Debug.Log(string.Format("Connect(\"{0}\", {1})", host, port));

		Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		socket.Connect(host, port);
		var id = _nextId++;
		_sockets[id] = new Connection { Socket = socket, Timeout = -1 };

		lua_newtable(L);
		luaL_getmetatable(L, "Socket");
		lua_setmetatable(L, -2);

		lua_pushstring(L, "clientId");
		lua_pushinteger(L, id);
		lua_rawset(L, -3);

		return 1;
	}

	private static Encoding TextEncoding = Encoding.GetEncoding(28591);

	private static int SocketSend(Lua.lua_State L)
	{
		var message = luaL_checkstring(L, 2).ToString();

		lua_pushstring(L, "clientId");
		lua_gettable(L, 1);
		var id = luaL_checkinteger(L, -1);

		var socket = _sockets[id];

		var bytes = TextEncoding.GetBytes(message);
		int bytesSent = 0;
		while (bytesSent < bytes.Length)
		{
			bytesSent += socket.Socket.Send(bytes, bytesSent, bytes.Length - bytesSent, SocketFlags.None);
		}

		return 0;
	}

	static byte[] buffer = new byte[1024];

	private static int SocketReceive(Lua.lua_State L)
	{
		lua_settop(L, 3);

		lua_pushstring(L, "clientId");
		lua_gettable(L, 1);
		var id = luaL_checkinteger(L, -1);
		
		var socket = _sockets[id];

		if (!socket.Socket.Connected)
		{
			Debug.Log("disconnected");
			lua_pushnil(L);
			lua_pushstring(L, "closed");
			return 2;
		}
		
		if (lua_isnumber(L, 2) != 0)
		{
			return SocketReceiveBytes(L, socket, luaL_checkinteger(L, 2));
		}
		else if (lua_isnil(L, 2))
		{
			return SocketReceiveLine(L, socket);
		}
		else if (lua_isstring(L, 2) != 0)
		{
			if (lua_tostring(L, 2).ToString() == "*1")
				return SocketReceiveLine(L, socket);
			if (lua_tostring(L, 2).ToString() == "*a")
				return SocketReceiveBytes(L, socket, -1);
		}

		lua_pushnil(L);
		lua_pushstring(L, "Bad parameter to socket:receive");
		return 2;
	}
	
	private static int SocketReceiveLine(lua_State L, Connection socket)
	{
		string s = "";

		while (true)
		{
			int bytesRead;

			try
			{
				bytesRead = socket.Socket.Receive(buffer, 0, 1, SocketFlags.None);
			}
			catch (SocketException e)
			{
				Debug.Log(string.Format("timeout ({0})", e.SocketErrorCode));
				lua_pushnil(L);
				lua_pushstring(L, "timeout");
				return 2;
			}
			
			if (bytesRead == 0)
				break;

			if (bytesRead == 1 && buffer[0] == 0xa)
				break;

			if (bytesRead == 1)
				s += TextEncoding.GetString(buffer, 0, 1);
		}
		
		lua_pushstring(L, s);
		return 1;
	}

	private static int SocketReceiveBytes(lua_State L, Connection socket, int bytesToReceive)
	{
		int byteCount;

		string s = "";

		if (socket.Timeout == 0)
		{
			socket.Socket.Blocking = false;
		}

		do
		{
			try
			{
				int max = bytesToReceive;
				if (max < 0 || max > buffer.Length)
					max = buffer.Length;
				byteCount = socket.Socket.Receive(buffer, 0, max, SocketFlags.None);
			}
			catch (SocketException e)
			{
				Debug.Log(string.Format("timeout ({0})", e.SocketErrorCode));

				socket.Socket.Blocking = true;
				lua_pushnil(L);
				lua_pushstring(L, "timeout");
				return 2;
			}
	
			if (byteCount == 0)
			{
				Debug.Log("disconnected (byteCount == 0)");

				socket.Socket.Blocking = true;
				lua_pushnil(L);
				lua_pushstring(L, "closed");
				return 2;
			}

			if (bytesToReceive >= 0)
				bytesToReceive -= byteCount;

		 	s += TextEncoding.GetString(buffer, 0, byteCount);
		}
		while (socket.Socket.Available > 0 && bytesToReceive != 0);

		socket.Socket.Blocking = true;
		
		lua_pushstring(L, s);
		return 1;
	}

	private static int SocketSetTimeout(Lua.lua_State L)
	{
		lua_pushstring(L, "clientId");
		lua_gettable(L, 1);
		var id = luaL_checkinteger(L, -1);
		
		var socket = _sockets[id];

		int timeout = -1;
		if (lua_isnumber(L, 2) != 0)
			timeout = (int)(1000 * luaL_checknumber(L, 2));

		socket.Timeout = timeout;

		return 0;
	}
}
