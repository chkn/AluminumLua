/*
	CompilerExecutor.cs: Compiles the code into a LuaFunction
	
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

#if DEBUG_WRITE_IL
#warning DEBUG_WRITE_IL is defined. This will quit after writing the first function to Debug_IL_Output.dll.
#endif

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq.Expressions;

using AluminumLua.Executors.ExpressionTrees;

namespace AluminumLua.Executors {
	
	public class CompilerExecutor : IExecutor {
		
		// all non-typesafe reflection here please
		private static readonly MethodInfo LuaContext_Get       = typeof (LuaContext).GetMethod ("Get");
		private static readonly MethodInfo LuaContext_SetLocal  = typeof (LuaContext).GetMethod ("SetLocal", new Type [] { typeof (string), typeof (LuaObject) });
		private static readonly MethodInfo LuaContext_SetGlobal = typeof (LuaContext).GetMethod ("SetGlobal", new Type [] { typeof (string), typeof (LuaObject) });
		
		private static readonly FieldInfo  LuaObject_Nil        = typeof (LuaObject).GetField  ("Nil", BindingFlags.Public | BindingFlags.Static);
		private static readonly MethodInfo LuaObject_FromString = typeof (LuaObject).GetMethod ("FromString", BindingFlags.Public | BindingFlags.Static);
		private static readonly MethodInfo LuaObject_FromNumber = typeof (LuaObject).GetMethod ("FromNumber", BindingFlags.Public | BindingFlags.Static);
		private static readonly MethodInfo LuaObject_FromTable  = typeof (LuaObject).GetMethod ("FromTable" , BindingFlags.Public | BindingFlags.Static);
		private static readonly MethodInfo LuaObject_AsFunction = typeof (LuaObject).GetMethod ("AsFunction");
		
		private static readonly MethodInfo LuaFunction_Invoke = typeof (LuaFunction).GetMethod ("Invoke");
		
#if DEBUG_WRITE_IL
		private static AssemblyBuilder asm;
		private static TypeBuilder typ;
#else
		private DynamicMethod method;
#endif
		private string functionName;
		private LuaFunction compiled;
		
		protected Stack<LuaContext>  scopes;
		protected Stack<Expression>  stack;
		protected ILGenerator        IL;
		
		protected ExpressionCompiler expressionCompiler;
		protected ParameterExpression ctx, args;
		
		public LuaContext CurrentScope { get { return scopes.Peek (); } }
		
		public CompilerExecutor (LuaContext ctx)
			: this (ctx, "dyn-" + Guid.NewGuid ())
		{
			this.functionName = null;
		}
		
		public CompilerExecutor (LuaContext ctx, string functionName)
		{
#if DEBUG_WRITE_IL
			if (asm == null) {
				asm = AppDomain.CurrentDomain.DefineDynamicAssembly (new AssemblyName ("DebugIL"), AssemblyBuilderAccess.RunAndSave);
				var mod = asm.DefineDynamicModule ("Debug_IL_Output.dll", "Debug_IL_Output.dll", true);
				typ = mod.DefineType ("DummyType");
			}
			var method = typ.DefineMethod (functionName, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof (LuaObject), new Type [] { typeof (LuaContext), typeof (LuaObject[]) });
#else
			this.method = new DynamicMethod (functionName, typeof (LuaObject), new Type [] { typeof (LuaContext), typeof (LuaObject[]) });
#endif
			this.functionName = functionName;
			this.scopes = new Stack<LuaContext> ();
			this.scopes.Push (ctx);
			
			this.stack = new Stack<Expression> ();
			this.IL = method.GetILGenerator ();
			
			this.ctx  = Expression.Parameter (typeof (LuaContext), "ctx");
			this.args = Expression.Parameter (typeof (LuaObject[]), "args");
			this.expressionCompiler = new ExpressionCompiler (IL, this.ctx, this.args);
		}
		
		public CompilerExecutor (LuaContext ctx, string functionName, string [] argNames)
			: this (ctx, functionName)
		{
			if (argNames.Length == 0)
				return;
			
			var done = IL.DefineLabel ();
			IL.DeclareLocal (typeof (int));
			
			IL.Emit (OpCodes.Ldarg_1); // load args array
			IL.Emit (OpCodes.Ldlen);
			IL.Emit (OpCodes.Stloc_0);
			
			for (int i = 0; i < argNames.Length; i++) {
				// since we don't know how many args were actually passed in, we have to check on each one
				IL.Emit (OpCodes.Ldc_I4, i);
				IL.Emit (OpCodes.Ldloc_0);
				IL.Emit (OpCodes.Bge, done);
				
				IL.Emit (OpCodes.Ldarg_0);
				IL.Emit (OpCodes.Ldstr, argNames [i]);
				IL.Emit (OpCodes.Ldarg_1);
				IL.Emit (OpCodes.Ldc_I4, i);
				IL.Emit (OpCodes.Ldelem, typeof (LuaObject));
				IL.Emit (OpCodes.Call, LuaContext_SetLocal);
				
				// also have to define args in scope manually here so prebinding works
				CurrentScope.Define (argNames [i]);
			}
			
			IL.MarkLabel (done);
		}
		
		public virtual void PushScope ()
		{
			scopes.Push (new LuaContext (CurrentScope));
		}
		
		public virtual void PushFunctionScope (string identifier, string[] argNames)
		{
			throw new NotSupportedException ();
		}
		
		public virtual void PopScope ()
		{
			if (scopes.Count == 1 && functionName != null) // we are ending this function
				CurrentScope.SetLocalAndParent (functionName, Compile ());
			
			scopes.Pop ();
		}
		
		public virtual void Constant (LuaObject value)
		{
			switch (value.Type) {
			
			case LuaType.@string:
				stack.Push (Expression.Call (LuaObject_FromString, Expression.Constant (value.AsString ())));
				break;
				
			case LuaType.number:
				stack.Push (Expression.Call (LuaObject_FromNumber, Expression.Constant (value.AsNumber ())));
				break;
				
			case LuaType.table:
				stack.Push (ConstantTableExpression (value.AsTable ()));
				break;
				
			default:
				throw new NotSupportedException (value.Type.ToString ());
			}
		}
				         
		private Expression ConstantTableExpression (IDictionary<string,LuaObject> table)
		{
			throw new NotImplementedException ();
		}
		
		public virtual void Variable (string identifier)
		{
			stack.Push (Expression.Call (ctx, LuaContext_Get, Expression.Constant (identifier)));
		}
		
		public virtual void Call (string identifier, int argCount)
		{
			var args = new Expression [argCount];
			
			// pop args
			for (int i = argCount - 1; i >= 0; i--)
				args [i] = stack.Pop ();
			
			// push function object
			Variable (identifier);
			stack.Push (Expression.Call (stack.Pop (), LuaObject_AsFunction));
			
			// call function
			stack.Push (Expression.Call (stack.Pop (), LuaFunction_Invoke, Expression.NewArrayInit (typeof (LuaObject), args)));
		}
		
		public virtual void PopStack ()
		{
			// turn last expression into a statement
			expressionCompiler.Compile (stack.Pop ());
			IL.Emit (OpCodes.Pop);
		}
		
		public virtual void Assign (string identifier, bool localScope)
		{
			CurrentScope.Define (identifier);
			
			IL.Emit (OpCodes.Ldarg_0);
			IL.Emit (OpCodes.Ldstr, identifier);
			expressionCompiler.Compile (stack.Pop ());
			
			if (localScope) 
				IL.Emit (OpCodes.Call, LuaContext_SetLocal);
				
			else
				IL.Emit (OpCodes.Call, LuaContext_SetGlobal);
		}
	
		public virtual LuaObject Result ()
		{
			return Compile () (new LuaObject [0]);
		}
		
		public LuaFunction Compile ()
		{
			if (compiled != null)
				return compiled;
			
			if (stack.Count == 0) {
				IL.Emit (OpCodes.Ldsfld, LuaObject_Nil);
				
			} else if (stack.Count == 1) {
				expressionCompiler.Compile (stack.Pop ());
				
			} else {
				
				throw new Exception ("stack height is greater than one!");
			}
			IL.Emit (OpCodes.Ret);
			
#if DEBUG_WRITE_IL
			typ.CreateType ();
			asm.Save ("Debug_IL_Output.dll");
			Environment.Exit (0);
			return null;
#else
			compiled = (LuaFunction)method.CreateDelegate (typeof (LuaFunction), CurrentScope);
			return compiled;
#endif
		}
	}
}