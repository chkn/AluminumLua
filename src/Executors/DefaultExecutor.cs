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
		
		public virtual void PushBlockScope ()
		{
			executors.Push (new CompilerExecutor (new LuaContext (CurrentScope)));
		}
		
		public virtual void PushFunctionScope (string [] argNames)
		{
			executors.Push (new CompilerExecutor (new LuaContext (CurrentScope), argNames));
		}
		
		public virtual void PopScope ()
		{
			var old = executors.Pop ();
			var fn  = old as CompilerExecutor;
			
			if (fn != null)
				Target.Constant (LuaObject.FromFunction (fn.Compile ()));
			
			old.PopScope ();
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
		
		public virtual void Call (int argCount)
		{
			Target.Call (argCount);
		}
		
		public virtual void TableCreate (int initCount)
		{
			Target.TableCreate (initCount);
		}
		
		public virtual void TableGet ()
		{
			Target.TableGet ();
		}
		
		public virtual void Concatenate ()
		{
			Target.Concatenate ();
		}
		
		public virtual void Negate ()
		{
			Target.Negate ();
		}

        public virtual void And() 
        {
            Target.And();
        }
        public virtual void Or() 
        {
            Target.Or();
        }
        public virtual void Equal() 
        {
            Target.Equal();
        }
        public virtual void NotEqual() 
        {
            Target.NotEqual();
        }

        public virtual void IfThenElse()
        {
            Target.IfThenElse();
        }

        public virtual void Greater()
        {
            Target.Greater();
        }
        public virtual void Smaller()
        {
            Target.Smaller();
        }
        public virtual void GreaterOrEqual()
        {
            Target.GreaterOrEqual();
        }
        public virtual void SmallerOrEqual()
        {
            Target.SmallerOrEqual();
        }

		public virtual void Add ()
		{
			Target.Add ();
		}
		
		public virtual void Subtract ()
		{
			Target.Subtract ();
		}
		
		public virtual void Multiply ()
		{
			Target.Multiply ();
		}
		
		public virtual void Divide ()
		{
			Target.Divide ();
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
		
		public virtual void TableSet ()
		{
			Target.TableSet ();
		}
		
		public virtual void Return ()
		{
			Target.Return ();
		}

        public void ColonOperator()
        {
            Target.ColonOperator();
        }

		public virtual LuaObject Result ()
		{
			return Target.Result ();
		}


	}
}

