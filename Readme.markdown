AluminumLua
===========

What?
-----

A fast, lightweight Lua scripting engine written entirely in C#. It's called 'AluminumLua' (or 'alua' for short) as a poke at all those "iron" languages...

Architecture
------------

AluminumLua does not use the dlr and runs on .net/mono 3.5 profile. It uses a handwritten parser that directly calls into a pluggable execution backend.

The current backend uses an interpreter for the main script body and generates DynamicMethods for any functions bodies (the assumption being they will probably be executed more than once). However, it would be trivial to change this around...

How?
----

	var context = new LuaContext ();
	context.AddBasicLibrary ();
	context.AddIoLibrary ();

	context.SetGlobal ("some_func_in_lua", calls_this_func_in_csharp);
	context.SetGlobal ("random_string", "hello");
	// ...

	var parser = new LuaParser (context, file_name); // < or leave file_name out for a "repl"
	parser.Parse ();


License
-------
MIT/X11
