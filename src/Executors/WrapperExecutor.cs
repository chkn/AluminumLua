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

namespace AluminumLua.Executors {
	
	public class WrapperExecutor : DefaultExecutor {
		
		public Action BeforeExpression { get; set; }
		public Action AfterExpression  { get; set; }
		
		public WrapperExecutor (IExecutor wrapped) : base ()
		{
			this.executors.Push (wrapped);
		}
		
		public override void Constant (LuaObject value)
		{
			DoBeforeExpression ();
			base.Constant (value);
			DoAfterExpression ();
		}
		
		public override void Variable (string identifier)
		{
			DoBeforeExpression ();
			base.Variable (identifier);
			DoAfterExpression ();
		}
		
		public override void Call (string identifier, int argCount)
		{
			DoBeforeExpression ();
			base.Call (identifier, argCount);
			DoAfterExpression ();
		}
		
		private void DoBeforeExpression ()
		{
			if (executors.Count == 1 && BeforeExpression != null)
				BeforeExpression ();
		}
		
		private void DoAfterExpression ()
		{
			if (executors.Count == 1 && AfterExpression != null)
				AfterExpression ();
		}
	}
}

