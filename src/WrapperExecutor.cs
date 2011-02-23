/*
	WrapperExecutor.cs: Apply simple aspects to an IExecutor
	
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

namespace AluminumLua {
	
	public class WrapperExecutor : IExecutor {
		
		public IExecutor Target  { get; private set; }
		
		public Action BeforeExpression { get; set; }
		public Action AfterExpression  { get; set; }
		
		public WrapperExecutor (IExecutor target)
		{
			this.Target = target;
		}
		
		// ----
		
		// scoping:
		public IExecutor PushScope ()
		{
			return Target.PushScope ();
		}
		
		public IExecutor PushFunctionScope (string identifier, string [] argNames)
		{
			return Target.PushFunctionScope (identifier, argNames);
		}
		
		public void PopScope () {
			Target.PopScope ();
		}
		
		public void PopScopeAsFunction (string identifier)
		{
			Target.PopScopeAsFunction (identifier);
		}
		
		public bool IsDefined (string identifier)
		{
			return Target.IsDefined (identifier);
		}
		
		// expressions:
		public IExecutor CreateExpression ()
		{
			return Target.CreateExpression ();
		}
		
		public IExecutor CreateArgumentsExpression (string functionName)
		{
			return Target.CreateArgumentsExpression (functionName);
		}
		
		public void Constant (LuaObject value)
		{
			if (BeforeExpression != null) BeforeExpression ();
			Target.Constant (value);
			if (AfterExpression != null) AfterExpression ();
		}
		
		public void Variable (string identifier)
		{
			if (BeforeExpression != null) BeforeExpression ();
			Target.Variable (identifier);
			if (AfterExpression != null) AfterExpression ();
		}
		
		public void Call (string identifier, int argCount)
		{
			if (BeforeExpression != null) BeforeExpression ();
			Target.Call (identifier, argCount);
			if (AfterExpression != null) AfterExpression ();
		}
		
		public void PopStack ()
		{
			Target.PopStack ();
		}
		
		// statements:
		public void Assign (string identifier, bool localScope)
		{
			Target.Assign (identifier, localScope);
		}
	}
}

