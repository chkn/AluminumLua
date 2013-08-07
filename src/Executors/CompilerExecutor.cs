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
	
	using LuaTable     = IDictionary<LuaObject,LuaObject>;
	using LuaTableItem = KeyValuePair<LuaObject,LuaObject>;
	
	public class CompilerExecutor : IExecutor {
		
		// all non-typesafe reflection here please
		private static readonly ConstructorInfo New_LuaContext  = typeof (LuaContext).GetConstructor (new Type [] { typeof (LuaContext) });
		private static readonly MethodInfo LuaContext_Get       = typeof (LuaContext).GetMethod ("Get");
		private static readonly MethodInfo LuaContext_SetLocal  = typeof (LuaContext).GetMethod ("SetLocal", new Type [] { typeof (string), typeof (LuaObject) });
		private static readonly MethodInfo LuaContext_SetGlobal = typeof (LuaContext).GetMethod ("SetGlobal", new Type [] { typeof (string), typeof (LuaObject) });
		
		private static readonly FieldInfo  LuaObject_Nil         = typeof (LuaObject).GetField  ("Nil", BindingFlags.Public | BindingFlags.Static);
		private static readonly MethodInfo LuaObject_FromString  = typeof (LuaObject).GetMethod ("FromString", BindingFlags.Public | BindingFlags.Static);
		private static readonly MethodInfo LuaObject_FromNumber  = typeof (LuaObject).GetMethod ("FromNumber", BindingFlags.Public | BindingFlags.Static);
		private static readonly MethodInfo LuaObject_FromBool    = typeof (LuaObject).GetMethod ("FromBool", BindingFlags.Public | BindingFlags.Static);
		
		private static readonly MethodInfo LuaObject_AsString    = typeof (LuaObject).GetMethod ("AsString");
		private static readonly MethodInfo LuaObject_AsBool      = typeof (LuaObject).GetMethod ("AsBool");
		private static readonly MethodInfo LuaObject_AsNumber    = typeof (LuaObject).GetMethod ("AsNumber");
		private static readonly MethodInfo LuaObject_AsFunction  = typeof (LuaObject).GetMethod ("AsFunction");

        private static readonly MethodInfo LuaObject_Equals = typeof(LuaObject).GetMethod("Equals", new Type[] {typeof(LuaObject)});

		private static readonly MethodInfo LuaFunction_Invoke    = typeof (LuaFunction).GetMethod ("Invoke");
		
		private static readonly MethodInfo LuaObject_this_get    = typeof (LuaObject).GetProperty ("Item").GetGetMethod ();
		private static readonly MethodInfo LuaObject_this_set    = typeof (LuaObject).GetProperty ("Item").GetSetMethod ();

		private static readonly MethodInfo LuaObject_NewTable    = typeof (LuaObject).GetMethod ("NewTable", BindingFlags.Public | BindingFlags.Static);
		private static readonly ConstructorInfo New_LuaTableItem = typeof (LuaTableItem).GetConstructor (new Type [] { typeof (LuaObject), typeof (LuaObject) });
		
		private static readonly MethodInfo String_Concat = typeof (string).GetMethod ("Concat", new Type [] { typeof (string), typeof (string) });
		
#if DEBUG_WRITE_IL
		private static AssemblyBuilder asm;
		private static TypeBuilder typ;
#else
		private DynamicMethod method;
#endif

		private LuaFunction compiled;		
		
		protected Stack<LuaContext>  scopes;
		protected Stack<Expression>  stack;
		protected ILGenerator        IL;
		
		protected ExpressionCompiler expression_compiler;
		protected ParameterExpression ctx, args;

		
		public LuaContext CurrentScope { get { return scopes.Peek (); } }
		
		public CompilerExecutor (LuaContext ctx)
		{
#if DEBUG_WRITE_IL
			if (asm == null) {
				asm = AppDomain.CurrentDomain.DefineDynamicAssembly (new AssemblyName ("DebugIL"), AssemblyBuilderAccess.RunAndSave);
				var mod = asm.DefineDynamicModule ("Debug_IL_Output.dll", "Debug_IL_Output.dll", true);
				typ = mod.DefineType ("DummyType");
			}
			var method = typ.DefineMethod ("dyn-" + Guid.NewGuid (), MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof (LuaObject), new Type [] { typeof (LuaContext), typeof (LuaObject[]) });
#else
			this.method = new DynamicMethod ("dyn-" + Guid.NewGuid (), typeof (LuaObject), new Type [] { typeof (LuaContext), typeof (LuaObject[]) });
#endif
			this.scopes = new Stack<LuaContext> ();
			this.scopes.Push (ctx);
			
			this.stack = new Stack<Expression> ();
			this.IL = method.GetILGenerator ();
			
			this.ctx  = Expression.Parameter (typeof (LuaContext), "ctx");
			this.args = Expression.Parameter (typeof (LuaObject[]), "args");
			this.expression_compiler = new ExpressionCompiler (IL, this.ctx, this.args);
		}
		
		public CompilerExecutor (LuaContext ctx, string [] argNames)
			: this (ctx)
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
		
		public virtual void PushBlockScope ()
		{
			throw new NotSupportedException ();
		}
		
		public virtual void PushFunctionScope (string[] argNames)
		{
			throw new NotSupportedException ();
		}
		
		public virtual void PopScope ()
		{
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
				
			case LuaType.function:
				// FIXME: we're actually creating a variable here..
				var fn = value.AsFunction ();
				var name = fn.Method.Name;
				CurrentScope.SetLocal (name, fn);
				Variable (name);
				break;
				
			default:
				throw new NotSupportedException (value.Type.ToString ());
			}
		}
		
		public virtual void Variable (string identifier)
		{
			stack.Push (Expression.Call (ctx, LuaContext_Get, Expression.Constant (identifier)));
		}
		
		public virtual void Call (int argCount)
		{
			var args = new Expression [argCount];
			
			// pop args
			for (int i = argCount - 1; i >= 0; i--)
				args [i] = stack.Pop ();
			
			// push function object
			stack.Push (Expression.Call (stack.Pop (), LuaObject_AsFunction));
			
			// call function
			stack.Push (Expression.Call (stack.Pop (), LuaFunction_Invoke, Expression.NewArrayInit (typeof (LuaObject), args)));
		}
		
		public virtual void TableCreate (int initCount)
		{
			var items = new Expression [initCount];
			
			// load up constructor items
			for (var i = 0; i < initCount; i++) {
				var value = stack.Pop ();
				var key = stack.Pop ();
				
				items [i] = Expression.New (New_LuaTableItem, key, value);
			}

			stack.Push (Expression.Call (LuaObject_NewTable, Expression.NewArrayInit (typeof (LuaTableItem), items)));
		}
		
		public virtual void TableGet ()
		{
			var key = stack.Pop ();
			stack.Push (Expression.Call (stack.Pop (), LuaObject_this_get, key));
		}
		
		public virtual void Concatenate ()
		{
			var val2 = Expression.Call (stack.Pop (), LuaObject_AsString);
			var val1 = Expression.Call (stack.Pop (), LuaObject_AsString);
			
			stack.Push (Expression.Call (LuaObject_FromString, Expression.Call (String_Concat, val1, val2)));
		}
		
		public virtual void Negate ()
		{
			stack.Push (Expression.Call (LuaObject_FromBool, Expression.Not (Expression.Call (stack.Pop (), LuaObject_AsBool))));
		}

        public virtual void Or()
        {
            var val2 = Expression.Call(stack.Pop(), LuaObject_AsBool);
            var val1 = Expression.Call(stack.Pop(), LuaObject_AsBool);
            stack.Push(Expression.Call(LuaObject_FromBool, Expression.OrElse(val1, val2)));
        }

        public virtual void And()
        {
            var val2 = Expression.Call(stack.Pop(), LuaObject_AsBool);
            var val1 = Expression.Call(stack.Pop(), LuaObject_AsBool);
            stack.Push(Expression.Call(LuaObject_FromBool, Expression.AndAlso(val1, val2)));
        }

        public virtual void Equal()
        {
            var val2 = stack.Pop();
            var val1 = stack.Pop();
            stack.Push(Expression.Call(LuaObject_FromBool, Expression.Call(val1, LuaObject_Equals, val2)));
        }

        public virtual void NotEqual()
        {
            var val2 = stack.Pop();
            var val1 = stack.Pop();
            stack.Push(Expression.Call(LuaObject_FromBool, Expression.Not(Expression.Call(val1, LuaObject_Equals, val2))));
        }
#if NET35
        public virtual void IfThenElse()
        {
            throw new NotImplementedException();
        }
#else
        public virtual void IfThenElse()
        {
            var Else = Expression.Call(stack.Pop(), LuaObject_AsFunction);
            var Then = Expression.Call(stack.Pop(), LuaObject_AsFunction);
            var Cond = Expression.Call(stack.Pop(), LuaObject_AsBool);
            stack.Push(Expression.IfThenElse(
                Cond, 
                Expression.Call(Then, LuaFunction_Invoke, Expression.NewArrayInit(typeof(LuaObject), new Expression[]{})), 
                Expression.Call(Else, LuaFunction_Invoke, Expression.NewArrayInit(typeof(LuaObject), new Expression[]{}))
            ));
        }
#endif


        public virtual void Greater()
        {
            var val2 = Expression.Call(stack.Pop(), LuaObject_AsNumber);
            var val1 = Expression.Call(stack.Pop(), LuaObject_AsNumber);

            stack.Push(Expression.Call (LuaObject_FromBool, Expression.GreaterThan(val1, val2)));
        }
        public virtual void Smaller()
        {
            var val2 = Expression.Call(stack.Pop(), LuaObject_AsNumber);
            var val1 = Expression.Call(stack.Pop(), LuaObject_AsNumber);

            stack.Push(Expression.Call(LuaObject_FromBool, Expression.LessThan(val1, val2)));
        }
        public virtual void GreaterOrEqual()
        {
            var val2 = Expression.Call(stack.Pop(), LuaObject_AsNumber);
            var val1 = Expression.Call(stack.Pop(), LuaObject_AsNumber);

            stack.Push(Expression.Call(LuaObject_FromBool, Expression.GreaterThanOrEqual(val1, val2)));
        }
        public virtual void SmallerOrEqual()
        {
            var val2 = Expression.Call(stack.Pop(), LuaObject_AsNumber);
            var val1 = Expression.Call(stack.Pop(), LuaObject_AsNumber);

            stack.Push(Expression.Call(LuaObject_FromBool, Expression.LessThanOrEqual(val1, val2)));
        }

		public virtual void Add ()
		{
			var val2 = Expression.Call (stack.Pop (), LuaObject_AsNumber);
			var val1 = Expression.Call (stack.Pop (), LuaObject_AsNumber);
			
			stack.Push (Expression.Call (LuaObject_FromNumber, Expression.Add (val1, val2)));
		}
		
		public virtual void Subtract ()
		{
			var val2 = Expression.Call (stack.Pop (), LuaObject_AsNumber);
			var val1 = Expression.Call (stack.Pop (), LuaObject_AsNumber);
			
			stack.Push (Expression.Call (LuaObject_FromNumber, Expression.Subtract (val1, val2)));			
		}
		
		public virtual void Multiply ()
		{
			var val2 = Expression.Call (stack.Pop (), LuaObject_AsNumber);
			var val1 = Expression.Call (stack.Pop (), LuaObject_AsNumber);
			
			stack.Push (Expression.Call (LuaObject_FromNumber, Expression.Multiply (val1, val2)));
		}
		
		public virtual void Divide ()
		{
			var val2 = Expression.Call (stack.Pop (), LuaObject_AsNumber);
			var val1 = Expression.Call (stack.Pop (), LuaObject_AsNumber);
			
			stack.Push (Expression.Call (LuaObject_FromNumber, Expression.Divide (val1, val2)));
		}
		
		public virtual void PopStack ()
		{
			// turn last expression into a statement
			expression_compiler.Compile (stack.Pop ());
			IL.Emit (OpCodes.Pop);
		}
		
		public virtual void Assign (string identifier, bool localScope)
		{
			CurrentScope.Define (identifier);
			
			IL.Emit (OpCodes.Ldarg_0);
			IL.Emit (OpCodes.Ldstr, identifier);
			expression_compiler.Compile (stack.Pop ());
			
			if (localScope) 
				IL.Emit (OpCodes.Call, LuaContext_SetLocal);
				
			else
				IL.Emit (OpCodes.Call, LuaContext_SetGlobal);
		}
		
		public virtual void TableSet ()
		{
			var val = stack.Pop ();
			var key = stack.Pop ();
			
			expression_compiler.Compile (Expression.Call (stack.Pop (), LuaObject_this_set, key, val));
		}
		
		public virtual void Return ()
		{
			if (stack.Count == 0) {
				IL.Emit (OpCodes.Ldsfld, LuaObject_Nil);
				
			} else if (stack.Count == 1) {
				expression_compiler.Compile (stack.Pop ());
				
			} else {
				
				throw new Exception ("stack height is greater than one!");
			}
			IL.Emit (OpCodes.Ret);
		}
		
		public virtual LuaObject Result ()
		{
			return Compile () (new LuaObject [0]);
		}

        public void ColonOperator()
        {
            var key = stack.Pop();
            var table = stack.Pop();
            stack.Push(Expression.Call(table, LuaObject_this_get, key));
            stack.Push(table);
        }

		public LuaFunction Compile ()
		{
			if (compiled != null)
				return compiled;
			
			Return ();
			
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