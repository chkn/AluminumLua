/*
	IExecutor.cs: Decouples execution from parsing
	
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

namespace AluminumLua.Executors {
	
	// All IExecutor implementations should have a constructor that takes a LuaContext
	public interface IExecutor {
		
		// scoping:
		void PushScope                      ();
		void PushFunctionScope              (string identifier, string [] argNames);
		LuaContext CurrentScope             { get; }
		void PopScope                       ();

		
		// expressions:
		void Constant                       (LuaObject value);
		void Variable                       (string identifier);
		void Call                           (string identifier, int argCount);
		void PopStack                       (); // <- turn above into a statement
		
		// statements:
		void Assign (string identifier, bool localScope);
		void Return ();
		
		// to execute: (some IExecutors - like InterpreterExecutor - may have executed instructions as they came in)
		LuaObject Result ();
	}
}

