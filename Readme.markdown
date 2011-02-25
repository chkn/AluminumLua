AluminumLua
===========

What?
-----

A fast, lightweight Lua scripting engine written entirely in C#. It's called 'AluminumLua' (or 'alua' for short) as a poke at all those "iron" languages...

Architecture
------------

AluminumLua does not use the DLR. Instead of generating an AST, the parser calls directly into an interface (IExecutor), that represents abstracted stack-based execution actions, such as Assignment or function Call.

This design enables different pluggable execution backends. Currently there is an InterpreterExecutor that directly executes the actions while the script is being parsed, and a CompilerExecutor that compiles a DynamicMethod.

The default execution policy uses the interpreter for the main script body and generates DynamicMethods for any defined function bodies. 

Dependencies
------------

.NET 3.5+ is required on Windows, or Mono 2.6+ anywhere else. There are no other dependencies.

Embed it
--------

	var context = new LuaContext ();
	context.AddBasicLibrary ();
	context.AddIoLibrary ();

	context.SetGlobal ("some_func_in_lua", calls_this_func_in_csharp);
	context.SetGlobal ("random_string", "hello");
	// ...

	var parser = new LuaParser (context, file_name); // < or leave file_name out to read stdin
	parser.Parse ();



Building
--------

Use Premake to generate a makefile or Visual Studio solution.
Get it here: http://industriousone.com/premake/download

### GNU Make ###

	premake4 gmake
	make

### Visual Studio / MonoDevelop / SharpDevelop ###

	premake4 vs2008
	(build the solution using your favorite tool)

The build produces the following products:

+ **AluminumLua.dll** is the script engine that can be referenced in other projects.
+ **alua.exe** is the command line interpreter. Pass a script file name or no arguments for a REPL.

Both products are self-contained and do not depend on the other.

After the Build
---------------

### Run tests ###

	premake4 test

### Clean ###

	premake4 clean

License
-------
MIT/X11
