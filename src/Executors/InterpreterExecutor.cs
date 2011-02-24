/*
	InterpreterExecutor.cs: Immediately executes the code by interpreting it
	
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
using System.Collections.Generic;

namespace AluminumLua.Executors {
	
	public class InterpreterExecutor : IExecutor {
		
		protected Stack<LuaContext> scopes;
		protected Stack<LuaObject>  stack;
		
		public LuaContext CurrentScope { get { return scopes.Peek (); } }
		
		public InterpreterExecutor (LuaContext ctx)
		{
			this.scopes = new Stack<LuaContext> ();
			this.scopes.Push (ctx);
			
			this.stack = new Stack<LuaObject> ();
		}
		
		public void PushScope ()
		{
			scopes.Push (new LuaContext (CurrentScope));
		}
		
		// FIXME: We should be able to define functions in the interpreter w/o having to compile DynamicMethods
		public void PushFunctionScope (string identifier, string [] argNames)
		{
			throw new NotSupportedException ();
		}
		
		public void PopScope ()
		{
			scopes.Pop ();
		}
		
		public bool IsDefined (string identifier)
		{
			return CurrentScope.Variables.ContainsKey (identifier);
		}
		
		public void Constant (LuaObject value)
		{
			stack.Push (value);
		}
		
		public void Variable (string identifier)
		{
			stack.Push (CurrentScope.Get (identifier));
		}
		
		public void Call (string identifier, int argCount)
		{
			var val = CurrentScope.Get (identifier);
			
			if (!val.IsFunction)
				throw new LuaException (string.Format ("cannot call non-function '{0}'", identifier));
			
			var args = new LuaObject [argCount];
			
			for (var i = argCount - 1; i >= 0; i--)
				args [i] = stack.Pop ();
			
			stack.Push (val.AsFunction () (args));
		}
		
		public void PopStack ()
		{
			stack.Pop ();
		}
		
		public void Assign (string identifier, bool localScope)
		{
			if (localScope)
				CurrentScope.SetLocal (identifier, stack.Pop ());
			else
				CurrentScope.SetGlobal (identifier, stack.Pop ());
		}
		
		public LuaObject Result ()
		{
			if (stack.Count > 0)
				return stack.Pop ();
			
			return LuaObject.Nil;
		}
		
	}
}

