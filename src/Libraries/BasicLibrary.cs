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

namespace AluminumLua {
	
	public partial class LuaContext {
		
		public void AddBasicLibrary ()
		{
			// ok, these seems almost TOO basic to go here..
			SetGlobal ("true", true);
			SetGlobal ("false", false);
			SetGlobal ("nil", null);
			
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
				
				if (arg == null)
					continue;
				
				// we do this ternary for the added verbosity mentioned in the manual (ex. nil -> "nil")
				buf.Append (arg.IsString? arg.AsString () : arg.ToString ());
				first = false;
			}
			
			Console.WriteLine (buf.ToString ());
			return true;
		}
		
		private LuaObject dofile (LuaObject [] args)
		{
			LuaParser parser;
			
			var exec = new InterpreterExecutor (this);
			var file = args.SingleOrDefault ();
			
			if (file != null)
				parser = new LuaParser (exec, file.AsString ());
			else
				parser = new LuaParser (exec);
			
			parser.Parse ();
			
			if (exec.Stack.Count > 0)
				return exec.Stack.Pop ();
			
			return true;
		}
		
		private LuaObject type (LuaObject [] args)
		{
			return Enum.GetName (typeof (LuaType), args.Single ().Type);
		}
	}
}

