/*
	CompilerExecutor.cs: Does not execute code directly.. generates a LuaFunction 
	
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
using System.Reflection;
using System.Reflection.Emit;

namespace AluminumLua {
	
	public class CompilerExecutor : IExecutor {
		
		// all non-typesafe reflection here please
		private static readonly MethodInfo LuaContext_Get = typeof (LuaContext).GetMethod ("Get");
		private static readonly MethodInfo LuaContext_SetLocal  = typeof (LuaContext).GetMethod ("SetLocal", new Type [] { typeof (string), typeof (LuaObject) });
		private static readonly MethodInfo LuaContext_SetGlobal = typeof (LuaContext).GetMethod ("SetGlobal", new Type [] { typeof (string), typeof (LuaObject) });
		
		private static readonly MethodInfo ListLuaObject_Add = typeof (List<LuaObject>).GetMethod ("Add");
		private static readonly MethodInfo ListLuaObject_ToArray = typeof (List<LuaObject>).GetMethod ("ToArray");
		
		private static readonly MethodInfo LuaObject_AsFunction = typeof (LuaObject).GetMethod ("AsFunction");
		
		private static readonly MethodInfo LuaFunction_Invoke = typeof (LuaFunction).GetMethod ("Invoke");
		
		private DynamicMethod method;
		//public static AssemblyBuilder asm;
		//private static TypeBuilder typ;
		
		public LuaContext CurrentScope { get; private set; }
		
		protected ILGenerator IL { get; private set; }
		protected int StackHeight { get; set; }
		
		public CompilerExecutor (LuaContext ctx)
		{
			this.method = new DynamicMethod ("dyn-" + Guid.NewGuid (), typeof (LuaObject), new Type [] { typeof (LuaContext), typeof (LuaObject[]) });
			/*
			if (asm == null) {
				asm = AppDomain.CurrentDomain.DefineDynamicAssembly (new AssemblyName ("foo"), AssemblyBuilderAccess.RunAndSave);
				var mod = asm.DefineDynamicModule ("Test.dll", "Test.dll", true);
				typ = mod.DefineType ("typ");
			}
			var method = typ.DefineMethod ("dyn" + Guid.NewGuid (), MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Any, typeof (LuaObject), new Type [] { typeof (LuaContext), typeof (LuaObject[]) });
			*/
			
			this.CurrentScope = ctx;
			this.IL = method.GetILGenerator ();
		}
		
		public CompilerExecutor (LuaContext ctx, string [] argNames) : this (ctx)
		{
			var done = IL.DefineLabel ();
			var argCount = IL.DeclareLocal (typeof (int));
			
			IL.Emit (OpCodes.Ldarg_1); // load args array
			IL.Emit (OpCodes.Ldlen);
			IL.Emit (OpCodes.Stloc, argCount);
			
			for (int i = 0; i < argNames.Length; i++) {
				// since we don't know how many args were actually passed in, we have to check on each one
				IL.Emit (OpCodes.Ldc_I4, i);
				IL.Emit (OpCodes.Ldloc, argCount);
				IL.Emit (OpCodes.Bge, done);
				
				IL.Emit (OpCodes.Ldarg_0);
				IL.Emit (OpCodes.Ldstr, argNames [i]);
				IL.Emit (OpCodes.Ldarg_1);
				IL.Emit (OpCodes.Ldc_I4, i);
				IL.Emit (OpCodes.Ldelem_Ref);
				IL.Emit (OpCodes.Castclass, typeof (LuaObject));
				IL.Emit (OpCodes.Call, LuaContext_SetLocal);
				
				// also have to define args in scope manually here so prebinding works
				CurrentScope.Define (argNames [i]);
			}
			
			IL.MarkLabel (done);
		}
		
		public IExecutor PushScope ()
		{
			IL.BeginScope ();
			return this;
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
			IL.EndScope ();
		}
		
		public void PopScopeAsFunction (string identifier)
		{
			if (StackHeight == 0) {
				IL.Emit (OpCodes.Ldnull);
				
			} else {
				
				//FIXME: Technically, this isn't quite correct /:
				while (StackHeight > 1) {
					IL.Emit (OpCodes.Pop);
					StackHeight--;
				}
			}
			IL.Emit (OpCodes.Ret);
			
			var impl = (LuaFunction)method.CreateDelegate(typeof (LuaFunction), CurrentScope);
			CurrentScope.SetLocalAndParent (identifier, impl);
			//typ.CreateType ();
			//CompilerExecutor.asm.Save ("Test.dll");
			//Environment.Exit (0);
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
			Variable (functionName);
			IL.Emit (OpCodes.Call, LuaObject_AsFunction);
			
			//FIXME: Really, we should use an array here..
			IL.Emit (OpCodes.Newobj, typeof (List<LuaObject>).GetConstructor (Type.EmptyTypes));
			IL.Emit (OpCodes.Dup);
			
			return new WrapperExecutor (this)
			{
				AfterExpression  = () => { IL.Emit (OpCodes.Call, ListLuaObject_Add); IL.Emit (OpCodes.Dup); }
			};
		}
		
		public void Constant (LuaObject value)
		{
			if (value.IsString) {
				IL.Emit (OpCodes.Ldstr, value.AsString ());
				IL.Emit (OpCodes.Newobj, typeof (LuaObject).GetConstructor (new Type [] { typeof (string) }));
			
			} else if (value.IsNumber) {
				IL.Emit (OpCodes.Ldc_R8, value.AsNumber ());
				IL.Emit (OpCodes.Newobj, typeof (LuaObject).GetConstructor (new Type [] { typeof (double) }));
			
			} else if (value.IsTable) {
				EmitTable (value.AsTable ());
				IL.Emit (OpCodes.Newobj, typeof (LuaObject).GetConstructor (new Type [] { typeof (IDictionary<string,LuaObject>) }));
			
			} else
				throw new NotSupportedException (value.ToString ());
			
			StackHeight++;
		}
				         
		protected void EmitTable (IDictionary<string,LuaObject> table)
		{
			throw new NotImplementedException ();
		}
		
		public void Variable (string identifier)
		{
			IL.Emit (OpCodes.Ldarg_0); // context
			IL.Emit (OpCodes.Ldstr, identifier);
			IL.Emit (OpCodes.Call, LuaContext_Get);
			
			StackHeight++;
		}
		
		public void Call (string identifier, int argCount)
		{
			IL.Emit (OpCodes.Pop);
			IL.Emit (OpCodes.Call, ListLuaObject_ToArray);
			
			IL.Emit (OpCodes.Call, LuaFunction_Invoke);
			
			StackHeight -= argCount;
		}
		
		public void PopStack ()
		{
			IL.Emit (OpCodes.Pop);
			
			StackHeight--;
		}
		
		public void Assign (string identifier, bool localScope)
		{
			CurrentScope.Define (identifier);
			IL.Emit (OpCodes.Ldarg_0);
			IL.Emit (OpCodes.Ldstr, identifier);
			
			if (localScope) 
				IL.Emit (OpCodes.Call, LuaContext_SetLocal);
				
			else
				IL.Emit (OpCodes.Call, LuaContext_SetGlobal);
		}

	}
}