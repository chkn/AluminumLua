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

namespace AluminumLua {
	
	public class InterpreterExecutor : IExecutor {
		
		public LuaContext CurrentScope { get; private set; }
		public Stack<LuaObject> Stack { get; private set; }
		
		public InterpreterExecutor (LuaContext ctx)
		{
			this.CurrentScope = ctx;
			this.Stack = new Stack<LuaObject> ();
		}
		
		public IExecutor PushScope ()
		{
			return new InterpreterExecutor (new LuaContext (CurrentScope));
		}
		
		public IExecutor PushFunctionScope (string identifier, string[] argNames)
		{
			var newCtx = new LuaContext (CurrentScope);
			
			// we need to define the function inside itself so it can call itself recursively
			newCtx.Define (identifier);
			
			return new CompilerExecutor (newCtx, argNames);
		}
		
		public void PopScope ()
		{
		}
		
		public void PopScopeAsFunction (string identifier)
		{
			throw new NotSupportedException ();
		}
		
		public bool IsDefined (string identifier)
		{
			return CurrentScope.Variables.ContainsKey (identifier);
		}
		
		public IExecutor CreateExpression ()
		{
			return this;
		}
		
		public IExecutor CreateArgumentsExpression (string functionName)
		{
			return this;
		}
		
		public void Constant (LuaObject value)
		{
			Stack.Push (value);
		}
		
		public void Variable (string identifier)
		{
			LuaObject val;
			if (!CurrentScope.Variables.TryGetValue (identifier, out val))
				throw new LuaException (string.Format ("'{0}' is not defined", identifier));
			
			Stack.Push (val);
		}
		
		public void Call (string identifier, int argCount)
		{
			LuaObject val;
			if (!CurrentScope.Variables.TryGetValue (identifier, out val))
				throw new LuaException (string.Format ("'{0}' is not defined", identifier));
			
			if (!val.IsFunction)
				throw new LuaException (string.Format ("cannot call non-function '{0}'", identifier));
			
			var args = new LuaObject [argCount];
			for (var i = 0; i < args.Length; i++)
				args [i] = Stack.Pop ();
			
			Stack.Push (val.AsFunction () (args));
		}
		
		public void PopStack ()
		{
			Stack.Pop ();
		}
		
		public void Assign (string identifier, bool localScope)
		{
			if (localScope)
				CurrentScope.SetLocal (identifier, Stack.Pop ());
			else
				CurrentScope.SetGlobal (identifier, Stack.Pop ());
		}
		

		
	}
}

