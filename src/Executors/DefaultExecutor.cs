/*
	DefaultExecutor.cs: Default execution policy
	
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
	
	public class DefaultExecutor : IExecutor {
                                             
		protected Stack<IExecutor> executors;
		public IExecutor Target  { get { return executors.Peek (); } }
		public LuaContext CurrentScope { get { return Target.CurrentScope; } }
		
		public DefaultExecutor (LuaContext ctx)
		{
			var defaultExecutor = new InterpreterExecutor (ctx);
			
			this.executors = new Stack<IExecutor> ();
			this.executors.Push (defaultExecutor);
		}
		
		protected DefaultExecutor ()
		{
		}
		
		// ----
		
		// scoping:
		public virtual void PushScope ()
		{
			var current = Target;
			current.PushScope ();
			executors.Push (current);
		}
		
		public virtual void PushFunctionScope (string identifier, string [] argNames)
		{
			var newCtx = new LuaContext (CurrentScope);
			
			// make sure function's name is defined inside itself!
			newCtx.Define (identifier);
			
			executors.Push (new CompilerExecutor (newCtx, identifier, argNames));
		}
		
		public virtual void PopScope ()
		{
			executors.Pop ().PopScope ();
		}
		
		// expressions:
		public virtual void Constant (LuaObject value)
		{
			Target.Constant (value);
		}
		
		public virtual void Variable (string identifier)
		{
			Target.Variable (identifier);
		}
		
		public virtual void Call (string identifier, int argCount)
		{
			Target.Call (identifier, argCount);
		}
		
		public virtual void PopStack ()
		{
			Target.PopStack ();
		}
		
		// statements:
		public virtual void Assign (string identifier, bool localScope)
		{
			Target.Assign (identifier, localScope);
		}
		
		public virtual void Return ()
		{
			Target.Return ();
		}
		
		public virtual LuaObject Result ()
		{
			return Target.Result ();
		}
	}
}

