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

Then run ZeroBrane, set its project folder to the Assets/Resources/LuaScripts folder, run the "Project/Start Debugging Server" 
menu option if it's not already greyed out, and open "main.lua".  See below for more details on this.

Finally run the LuaDebug scene in Unity, and you should find it breaks into the debugger as expected and allows you to single-step 
through the code.  Note that you won't see any of the code's output until it has totally finished and returned control to Unity.

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

In order to debug your scripts, ZeroBrane needs to be able to see your lua scripts, and it needs the Lua interpreter to be able to tell 
it which script it's running at the moment.  So you need to be running your Lua scripts in a way which means that Lua knows their 
filenames - don't just stream code into the interpreter using loadstring, you need to use loadfile or similar instead.

Virtual filesystem
------------------

But under Unity, KopiLua doesn't really have filesystem access, at least not in a cross-platform way.  To fix that, you can use my 
UnityFOpen virtual filesystem, which lets KopiLua access files through Unity's Resource loading system.

    KopiLua.Lua.fopenDelegate = UnityFOpen.FOpen;
    UnityFOpen.ResourceRootPath = "LuaScripts";

This maps the virtual filesystem's current directory to the "LuaScripts" subfolder of the "Resources" folder, which should be in 
"Assets" somewhere.  (It doesn't matter where, and it's OK to have multiple Resources folders and even multiple LuaScripts folders...)

Lua files must have .txt extension
----------------------------------

Unfortunately due to quirks with Unity's Resource system we need the ".lua" files to have an extra ".txt" extension.  So, rename all 
your lua files to names ending with ".lua.txt" - these are the files that will be loaded at runtime.  You don't need to change 
anything in the Lua scripts, the extra ".txt" extension is hidden from them.

ZeroBrane still needs to see the non-.txt files
-----------------------------------------------

That brings up the next problem - ZeroBrane does not know about these ".txt" extensions, and it can only debug code it can see.  So, 
you need to also keep copies of the .lua files without the .txt extension.  It's up to you where you put them, you can put them in 
the Resources folder too and they won't do any harm there, or you can store them outside your project entirely.  You could set up a 
script, or an editor extension, or even an AssetPostProcessor (Pro only), to automatically sync the .lua files to the .lua.txt files.

Set up ZeroBrane
----------------

To debug, ZeroBrane needs to be running, with the debugging server active.  On the "Project" menu, execute the "Start Debugging 
Server" menu option.  If it's greyed out then that's fine, it means it's already running.

It also needs to have the Lua source files visible in its project directory, so press the "..." button in the Project pane and 
navigate to wherever you put the non-.txt-extension Lua files.

Finally, it needs to have a file open - I don't think it matters which.

Connect your script to the debugger
-----------------------------------

One of your scripts - usually the first one executed - needs to run the mobdebug script, to connect the Lua interpreter to the debugger.
Generally you just add this line somewhere:

    require("mobdebug").start()

This requires mobdebug.lua.txt to be in your LuaScripts directory too, of course.

With this line in place your script will not run if the debugger doesn't accept the connection, so you might want to put it in a 
conditional of some sort, so it's only enabled if you tick a box in the Inspector or something like that.

Save and run
------------

Now when you run your game it should pause (and hang the entire Unity UI) as soon as it hits the mobdebug initialization line.  
ZeroBrane should pop up and show your Lua code, with the instruction pointer just after the mobdebug line.  At this point you can use 
the debugger as normal - you can set breakpoints and run until they get hit, or single-step, inspect variables, and so on.

Again, Unity will be unresponsive while your Lua code is stopped in the debugger - this is as normal for Unity when a user script is 
taking a long time to complete.  It should be fine when you unbreak in the debugger.

Caveats
-------

The biggest problem that can occur at the moment is that if the mobdebug code gets confused and never continues execution then Unity 
can remain hung forever.  Sometimes closing ZeroBrane is enough to wake it up, sometimes it's not.  I think to some extent this is 
mobdebug not handling unexpected conditions well, but it could also be due to bugs in my luasocket implementation, especially 
handling of timeouts.  For now you just need to be careful, and make sure you save your scene (if it contains significant changes) 
before running.

There are also problems if ZeroBrane gets upset e.g. about not being able to find the lua file that the interpreter is running.  
Again it seems to lead to Unity being hung forever.  Try to avoid that.

