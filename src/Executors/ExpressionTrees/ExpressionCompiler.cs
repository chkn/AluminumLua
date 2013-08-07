/*
	ExpressionCompiler.cs: A really simple .NET 3.5 partial expression tree compiler
	
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
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq.Expressions;

namespace AluminumLua.Executors.ExpressionTrees {
	
	public class ExpressionCompiler : ExpressionVisitor {
		
		private static readonly MethodInfo Type_GetTypeFromHandle = typeof (Type).GetMethod ("GetTypeFromHandle", BindingFlags.Public | BindingFlags.Static);
		private static readonly MethodInfo MethodBase_GetMethodFromHandle = typeof (MethodBase).GetMethod ("GetMethodFromHandle", new Type [] { typeof (RuntimeMethodHandle) });
		
		protected ILGenerator                   IL;
		protected string []                     arg_names;
		protected Dictionary<Type,LocalBuilder> registers;
		
		public ExpressionCompiler (ILGenerator target, params ParameterExpression [] args)
		{
			this.IL = target;
			this.arg_names = args.Select (p => p.Name).ToArray ();
			this.registers = new Dictionary<Type, LocalBuilder> ();
		}
		
		public void Compile (Expression expression)
		{
			Visit (expression);
		}
		
		protected LocalBuilder LockRegister (Type type)
		{
			LocalBuilder reg;
			if (registers.TryGetValue (type, out reg))
				registers.Remove (type);
			else
				reg = IL.DeclareLocal (type);
			return reg;
		}
		
		protected void UnlockRegister (LocalBuilder reg)
		{
			if (registers.ContainsKey (reg.LocalType))
				return;
			
			registers.Add (reg.LocalType, reg);
		}
		
		protected override void VisitConstant (ConstantExpression c)
		{
			if (c.Value == null)
				IL.Emit (OpCodes.Ldnull);
			
			else if (c.Value is string)
				IL.Emit (OpCodes.Ldstr, (string)c.Value);
			
			else if (c.Value is int)
				IL.Emit (OpCodes.Ldc_I4, (int)c.Value);
			
			else if (c.Value is double)
				IL.Emit (OpCodes.Ldc_R8, (double)c.Value);
			
			else if (c.Value is Type) {
				IL.Emit (OpCodes.Ldtoken, (Type)c.Value);
				IL.Emit (OpCodes.Call, Type_GetTypeFromHandle);
			
			} else if (c.Value is MethodInfo) {
				IL.Emit (OpCodes.Ldtoken, (MethodInfo)c.Value);
				IL.Emit (OpCodes.Call, MethodBase_GetMethodFromHandle);
				IL.Emit (OpCodes.Castclass, typeof (MethodInfo));
				
			} else
				throw new NotSupportedException (c.Value.GetType ().FullName);
		}
		
		protected override void VisitParameter (ParameterExpression p)
		{
			IL.Emit (OpCodes.Ldarg, Array.IndexOf (arg_names, p.Name));
		}

        protected override void VisitUnary(UnaryExpression u)
        {
            Visit(u.Operand);
            switch (u.NodeType)
            {
                case ExpressionType.Not:
                    IL.Emit(OpCodes.Not);
                    break;
            }
        }

		protected override void VisitBinary (BinaryExpression b)
		{
			Visit (b.Left);
			
			switch (b.NodeType) {
			
			case ExpressionType.Add:
                Visit(b.Right);
				IL.Emit (OpCodes.Add);
				break;
			
			case ExpressionType.Subtract:
                Visit(b.Right);
				IL.Emit (OpCodes.Sub);
				break;
				
			case ExpressionType.Multiply:
                Visit(b.Right);
				IL.Emit (OpCodes.Mul);
				break;
				
			case ExpressionType.Divide:
                Visit(b.Right);
				IL.Emit (OpCodes.Div);
				break;
            case ExpressionType.GreaterThan:
                Visit(b.Right);
                IL.Emit(OpCodes.Cgt);
                break;
            case ExpressionType.GreaterThanOrEqual:
                Visit(b.Right);
                IL.Emit(OpCodes.Clt);
                IL.Emit(OpCodes.Not);
                break;
            case ExpressionType.LessThan:
                Visit(b.Right);
                IL.Emit(OpCodes.Clt);
                break;
            case ExpressionType.LessThanOrEqual:
                Visit(b.Right);
                IL.Emit(OpCodes.Cgt);
                IL.Emit(OpCodes.Not);
                break;
            case ExpressionType.AndAlso:
                var ItsFalse = IL.DefineLabel();
                var EndAnd = IL.DefineLabel();
                IL.Emit(OpCodes.Brfalse, ItsFalse);
                Visit(b.Right);
                IL.Emit(OpCodes.Br, EndAnd);
                IL.MarkLabel(ItsFalse);
                IL.Emit(OpCodes.Ldc_I4_0);
                IL.MarkLabel(EndAnd);
                break;
            case ExpressionType.OrElse:
                var ItsTrue = IL.DefineLabel();
                var EndOr = IL.DefineLabel();
                IL.Emit(OpCodes.Brtrue, ItsTrue);
                Visit(b.Right);
                IL.Emit(OpCodes.Br, EndOr);
                IL.MarkLabel(ItsTrue);
                IL.Emit(OpCodes.Ldc_I4_1);
                IL.MarkLabel(EndOr);
                break;
            case ExpressionType.Equal:
                Visit(b.Right);
                IL.Emit(OpCodes.Ceq);
                break;
            case ExpressionType.NotEqual:
                Visit(b.Right);
                IL.Emit(OpCodes.Ceq);
                IL.Emit(OpCodes.Not);
                break;
			default:
				throw new NotImplementedException (b.Type.ToString ());
			}
		}
		
		protected override void VisitMethodCall (MethodCallExpression m)
		{
			LocalBuilder reg = null;
			
			if (m.Object != null) {
				Visit (m.Object);
				
				if (m.Object.Type.IsValueType) {
					// for the record, I hate that this is necessary
					reg = LockRegister (m.Object.Type);
					IL.Emit (OpCodes.Stloc, reg);
					IL.Emit (OpCodes.Ldloca, reg);
				}
			}
			
			foreach (var arg in m.Arguments)
				Visit (arg);
			
			// FIXME: AlumninumLua never calls virtual methods while executing a script.. does it?
			IL.Emit (OpCodes.Call, m.Method);
			
			if (reg != null)
				UnlockRegister (reg);
		}
		
		protected override void VisitNew (NewExpression nex)
		{
			foreach (var arg in nex.Arguments)
				Visit (arg);
			
			IL.Emit (OpCodes.Newobj, nex.Constructor);
		}
		
		protected override void VisitNewArray (NewArrayExpression na)
		{
			IL.Emit (OpCodes.Ldc_I4, na.Expressions.Count);
			IL.Emit (OpCodes.Newarr, na.Type.GetElementType ());
			
			int i = 0;
			foreach (var item in na.Expressions) {
				IL.Emit (OpCodes.Dup); // keep the array on the stack
				IL.Emit (OpCodes.Ldc_I4, i++);
				Visit (item);
				IL.Emit (OpCodes.Stelem, item.Type);
			}
		}

        protected override void VisitConditional(ConditionalExpression c)
        {
            var End = IL.DefineLabel();
            var IfFalse = IL.DefineLabel();
            Visit(c.Test);

            IL.Emit(OpCodes.Brfalse, IfFalse);
            Visit(c.IfTrue);
            IL.Emit(OpCodes.Br, End);
            IL.MarkLabel(IfFalse);
            Visit(c.IfFalse);
            IL.MarkLabel(End);
        }
	}
}

