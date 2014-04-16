KopiLuaDebug
============

This is a sample Unity project prototyping some glue to let users debug Lua scripts running through KopiLua using ZeroBrane Studio.

The main requirement here was a .NET-native luasocket implementation, in order to allow mobdebug.lua to function.  I only implemented
the bare minimum of features required to get it to work, and I'm not a luasocket expert so my understanding of the API is driven 
entirely by observing how it's used in mobdebug.

About these notes
-----------------

I don't have a lot of time to write up how to use this, but I wanted to capture what I could - so these notes are rather brief and 
disorganized.  Hopefully they are still useful to somebody.

Contact me
----------

I'd love to hear any experiences using this, especially if you find better ways around some of the problems.  Please do contact me 
with feedback, questions, requests, etc.

george.foot@gmail.com

Using the KopiLuaDebug example project
======================================

To use the project you need to supply a KopiLua.dll, e.g. take the one from http://gfootweb.webspace.virginmedia.com/KLI-bin/ 
and put it somewhere in the Assets folder.

Then run ZeroBrane Studio, set its project folder to the Assets/LuaScripts folder, run the "Project/Start Debugging Server" 
menu option if it's not already greyed out, and open "main.lua".  See below for more details on this.

Finally run the LuaDebug scene in Unity, and you should find it breaks into the debugger as expected and allows you to single-step 
through the code.  Note that you won't see any of the code's output until it has totally finished and returned control to Unity.
You can also inspect variables and use the remote console pane to get the Lua VM in your Unity app to evaluate expressions and 
generally run whatever code you want.

Setting up your project for debugging
=====================================

Using LuaSocketLibrary
----------------------

You need to preload the LuaSocketLibrary package, so that when mobdebug does a require("socket") it doesn't try to load a .lua file 
or anything like that.  To do this at the KopiLua level, call LuaSocketLibrary.Init, i.e.:

    _lua = Lua.luaL_newstate();
    Lua.luaL_openlibs(_lua);
    LuaSocketLibrary.Init(_lua);

Use loadfile, not loadstring
----------------------------

For best results, ZeroBrane Studio needs to be able to see your lua scripts, and it needs the Lua interpreter to be able to tell 
it which script it's running at the moment.  So you need to be running your Lua scripts in a way which means that Lua knows their 
filenames - don't just stream code into the interpreter using loadstring, you need to use loadfile or similar instead.

The author of ZeroBrane Studio told me that there is an alternative if you want loadstring to work - you can set the editor.autoactivate
option (see http://studio.zerobrane.com/doc-editor-preferences.html).  This will make ZBS auto-open scripts that aren't already open, and
it will also open a new tab for debugging loadstring code.  So you could try that - I still recommend using real .lua files if you can
though.

Virtual filesystem
------------------

But under Unity, KopiLua doesn't really have filesystem access, at least not in
a cross-platform way, so it can't load .lua files.  To fix that, you can use my 
UnityFOpen virtual filesystem, which lets KopiLua access files through Unity's
Resource loading system.

    KopiLua.Lua.fopenDelegate = UnityFOpen.FOpen;
    UnityFOpen.ResourceRootPath = "LuaScripts";

This maps the virtual filesystem's current directory to the "LuaScripts"
subfolder of the "Resources" folder, which should be in "Assets" somewhere.
(It doesn't matter where, and it's OK to have multiple Resources folders and
even multiple LuaScripts folders...)

Resources-hosted Lua files must have .txt extension
---------------------------------------------------

Unfortunately due to quirks with Unity's Resource system we need the ".lua"
files there to have an extra ".txt" extension.  This is invisible to Lua, so you don't need to edit your package paths etc.

A good way to arrange this is to store source ".lua" files outside the Resources folder, e.g. in Assets/LuaScripts, and
use an AssetPostprocessor to maintain ".txt" copies of these files in the Resources folder.  I've provided such an
AssetPostprocessor in the Editor folder, which is configured to copy from Assets/LuaScripts to Resources/LuaScripts.

So, edit the lua files in Assets/LuaScripts, and let ZeroBrane Studio open them from there; but be vaguely aware that your
runtime code is actually reading copies of the files instead, from the Resources folder.  I think the AssetPostprocessor
works around all the problems here but if you find anything that still doesn't work well as a result of this then I'd be
interested to know about it.

Set up ZeroBrane Studio
-----------------------

To debug, ZeroBrane Studio needs to be running, with the debugging server active.  On the "Project" menu, execute the "Start Debugging 
Server" menu option.  If it's greyed out then that's fine, it means it's already running.

It also needs to have the Lua source files visible in its project directory, so press the "..." button in the Project pane and 
navigate to wherever you put the non-.txt-extension Lua files.  Remember that ZeroBrane Studio doesn't know anything about the 
ones with the .txt extension, it needs to see the originals.

Finally, by default it needs to have the file you're going to debug open.  If you set the "editor.autoactivate" option, however,
it will automatically open files, so long as they're in the active project tree.  This is a lot more convenient.

Connect your script to the debugger
-----------------------------------

One of your scripts - usually the first one executed - needs to run the mobdebug script, to connect the Lua interpreter to the debugger.
Generally you just add this line somewhere:

    require("mobdebug").start()

This requires mobdebug.lua to be in your LuaScripts directory too, of course.  In the example project, I actually do this from C# via
a loadstring call.

With this line in place your script will not run very well if the debugger
doesn't accept the connection (at best it will stall for a bit and report an
error before carrying on), so you might want to put it in a conditional of some
sort, so that it's only enabled if you tick a box in the Inspector or something
like that.

Save and run
------------

Now when you run your game it should pause (and hang the entire Unity UI) after
executing the mobdebug start line.  ZeroBrane Studio should pop up and show
your Lua code, pointing at whatever Lua instruction is next to execute.  At
this point you can use the debugger as normal - you can set breakpoints and run
until they get hit, or single-step, inspect variables, and so on.

Again, Unity will be unresponsive while your Lua code is stopped in the
debugger - this is as normal for Unity when a user script is taking a long time
to complete.  It should be fine when you unbreak in the debugger.

Caveats
-------

I have had problems in the past where something would go wrong between the
debugger and the debuggee, and Unity would remain hung forever.  I think I have
resolved this now, but in case it happens to you, try closing ZeroBrane Studio
completely - it might be enough to break the deadlock.  Please do contact me if
you see this happen.

In some circumstances the debugger will try to kill your app, e.g. if you press
the "stop" button.  This is not supported in KopiLua - in fact it causes a
Unity deadlock if you call os.exit.  Oops.  I will have to fix that sometime
but for now you can make os.exit do nothing at all - see the test script for
example, it does this using another loadstring call.

This will make your app carry on regardless, with the debugger detached.  It's
not what "stop" should do, but it's better than a deadlock :)

