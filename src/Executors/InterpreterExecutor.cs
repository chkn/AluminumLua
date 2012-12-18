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
		
		public virtual void PushScope ()
		{
			scopes.Push (new LuaContext (CurrentScope));
		}
		
		public virtual void PushBlockScope ()
		{
			throw new NotSupportedException ();
		}
		
		public virtual void PushFunctionScope (string [] argNames)
		{
			throw new NotSupportedException ();
		}
		
		public virtual void PopScope ()
		{
			scopes.Pop ();
		}
		
		public virtual void Constant (LuaObject value)
		{
			stack.Push (value);
		}
		
		public virtual void Variable (string identifier)
		{
			stack.Push (CurrentScope.Get (identifier));
		}
		
		public virtual void Call (int argCount)
		{
			var args = new LuaObject [argCount];
			
			for (var i = argCount - 1; i >= 0; i--)
				args [i] = stack.Pop ();

			var function = stack.Pop();
			stack.Push(function.AsFunction()(args));
		}
		
		public virtual void TableCreate (int initCount)
		{
			var table = LuaObject.NewTable ();
			
			for (var i = 0; i < initCount; i++) {
				var value = stack.Pop ();
				var key = stack.Pop ();
				
				table [key] = value;
			}
			
			stack.Push (table);
		}
		
		public virtual void TableGet ()
		{
			var key = stack.Pop ();
			var table = stack.Pop ();
			stack.Push (table [key]);
		}
		
		public virtual void Concatenate ()
		{
			var val2 = stack.Pop ().AsString ();
			var val1 = stack.Pop ().AsString ();
			
			stack.Push (LuaObject.FromString (string.Concat (val1, val2)));
		}
		
		public virtual void Negate ()
		{
			var val = stack.Pop ().AsBool ();
			stack.Push (LuaObject.FromBool (!val));
		}
		
		public virtual void Add ()
		{
			var val2 = stack.Pop ().AsNumber ();
			var val1 = stack.Pop ().AsNumber ();
			
			stack.Push (LuaObject.FromNumber (val1 + val2));
		}
		
		public virtual void Subtract ()
		{
			var val2 = stack.Pop ().AsNumber ();
			var val1 = stack.Pop ().AsNumber ();
			
			stack.Push (LuaObject.FromNumber (val1 - val2));
		}
		
		public virtual void Multiply ()
		{
			var val2 = stack.Pop ().AsNumber ();
			var val1 = stack.Pop ().AsNumber ();
			
			stack.Push (LuaObject.FromNumber (val1 * val2));
		}
		
		public virtual void Divide ()
		{
			var val2 = stack.Pop ().AsNumber ();
			var val1 = stack.Pop ().AsNumber ();
			
			stack.Push (LuaObject.FromNumber (val1 / val2));
		}
		
		public virtual void PopStack ()
		{
			stack.Pop ();
		}
		
		public virtual void Assign (string identifier, bool localScope)
		{
			if (localScope)
				CurrentScope.SetLocal (identifier, stack.Pop ());
			else
				CurrentScope.SetGlobal (identifier, stack.Pop ());
		}
		
		public virtual void TableSet ()
		{
			var value = stack.Pop ();
			var key = stack.Pop ();
			var table = stack.Pop ();
			
			table [key] = value;
		}
		
		public virtual void Return ()
		{
			// FIXME: This will do something once the interpreter can support uncompiled functions /:
		}
		
		public virtual LuaObject Result ()
		{
			if (stack.Count > 0)
				return stack.Pop ();
			
			return LuaObject.Nil;
		}

		public void ColonOperator()
		{
			var key = stack.Pop();
			var table = stack.Pop();
			stack.Push(table[key]);
			stack.Push(table);
		}
	}
}

