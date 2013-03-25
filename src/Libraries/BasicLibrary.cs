/*
	BasicLibrary.cs: Lua Basic Library
	http://www.lua.org/manual/5.1/manual.html#5.1
	
	Copyright (c) 2011 Alexander Corrado
  
	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:
	
	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.
	
	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
 */

using System;
using System.Linq;
using System.Text;

using AluminumLua.Executors;

namespace AluminumLua {
	
	public partial class LuaContext {
		
		public void AddBasicLibrary ()
		{
			SetGlobal ("print", print);
			SetGlobal ("dofile", dofile);
			SetGlobal ("type", type);
		}
		
		private LuaObject print (LuaObject [] args)
		{
			var first = true;
			var buf = new StringBuilder ();
			
			foreach (var arg in args) {
				
				if (!first)
					buf.Append ('\t');
				
				buf.Append (arg.ToString ());
				first = false;
			}
			
			Console.WriteLine (buf.ToString ());
			return true;
		}
		
		private LuaObject dofile (LuaObject [] args)
		{
			LuaParser parser;
			
			var exec = LuaSettings.Executor (this);
			var file = args.FirstOrDefault ();
			
			if (file.IsNil)
				parser = new LuaParser (exec); // read from stdin
			else
				parser = new LuaParser (exec, file.AsString ());
			
			parser.Parse ();
			
			return exec.Result ();
		}
		
		private LuaObject type (LuaObject [] args)
		{
			return Enum.GetName (typeof (LuaType), args.First ().Type);
		}
	}
}

