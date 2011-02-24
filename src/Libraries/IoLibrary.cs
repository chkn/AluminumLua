/*
	IoLibrary.cs: Lua I/O Library
	http://www.lua.org/manual/5.1/manual.html#5.7
	
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

namespace AluminumLua {
	
	public partial class LuaContext {
		
		public void AddIoLibrary ()
		{
			// FIXME: This doesn't work.. we need a way to handle dot notation for tables anyway
			SetGlobal ("io.write", io_write);
		}
		
		private LuaObject io_write (LuaObject [] args)
		{
			foreach (var arg in args) {
			
				if (arg.IsString)
					Console.Write (arg.AsString ());
				
				else if (arg.IsNumber)
					Console.Write (arg.AsNumber ());
				
				else
					throw new LuaException ("bad argument to write");
				
			}
			
			return true;
		}
		
	}
}

