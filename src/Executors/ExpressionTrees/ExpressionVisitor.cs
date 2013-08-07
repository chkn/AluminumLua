/*
 * ExpressionVisitor.cs
 * Based on: http://msdn.microsoft.com/en-us/library/bb882521%28v=VS.90%29.aspx
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace AluminumLua.Executors.ExpressionTrees {
	
	public abstract class ExpressionVisitor {
		
	    protected ExpressionVisitor()
	    {
	    }
	
	    protected virtual void Visit(Expression exp)
	    {
	        if (exp == null)
	             return;
	        switch (exp.NodeType)
	        {
	            case ExpressionType.Negate:
	            case ExpressionType.NegateChecked:
	            case ExpressionType.Not:
	            case ExpressionType.Convert:
	            case ExpressionType.ConvertChecked:
	            case ExpressionType.ArrayLength:
	            case ExpressionType.Quote:
	            case ExpressionType.TypeAs:
	                 this.VisitUnary((UnaryExpression)exp);
			         break; 
	            case ExpressionType.Add:
	            case ExpressionType.AddChecked:
	            case ExpressionType.Subtract:
	            case ExpressionType.SubtractChecked:
	            case ExpressionType.Multiply:
	            case ExpressionType.MultiplyChecked:
	            case ExpressionType.Divide:
	            case ExpressionType.Modulo:
	            case ExpressionType.And:
	            case ExpressionType.AndAlso:
	            case ExpressionType.Or:
	            case ExpressionType.OrElse:
	            case ExpressionType.LessThan:
	            case ExpressionType.LessThanOrEqual:
	            case ExpressionType.GreaterThan:
	            case ExpressionType.GreaterThanOrEqual:
	            case ExpressionType.Equal:
	            case ExpressionType.NotEqual:
	            case ExpressionType.Coalesce:
	            case ExpressionType.ArrayIndex:
	            case ExpressionType.RightShift:
	            case ExpressionType.LeftShift:
	            case ExpressionType.ExclusiveOr:
	                 this.VisitBinary((BinaryExpression)exp);
				     break;
	            case ExpressionType.TypeIs:
	                 this.VisitTypeIs((TypeBinaryExpression)exp);
				     break;
	            case ExpressionType.Conditional:
	                 this.VisitConditional((ConditionalExpression)exp);
				     break;
	            case ExpressionType.Constant:
	                 this.VisitConstant((ConstantExpression)exp);
				     break;
	            case ExpressionType.Parameter:
	                 this.VisitParameter((ParameterExpression)exp);
				     break;
	            case ExpressionType.MemberAccess:
	                 this.VisitMemberAccess((MemberExpression)exp);
				     break;
	            case ExpressionType.Call:
	                 this.VisitMethodCall((MethodCallExpression)exp);
				     break;
	            case ExpressionType.Lambda:
	                 this.VisitLambda((LambdaExpression)exp);
				     break;
	            case ExpressionType.New:
	                 this.VisitNew((NewExpression)exp);
				     break;
	            case ExpressionType.NewArrayInit:
	            case ExpressionType.NewArrayBounds:
	                 this.VisitNewArray((NewArrayExpression)exp);
				     break;
	            case ExpressionType.Invoke:
	                 this.VisitInvocation((InvocationExpression)exp);
				     break;
	            case ExpressionType.MemberInit:
	                 this.VisitMemberInit((MemberInitExpression)exp);
				     break;
	            case ExpressionType.ListInit:
	                 this.VisitListInit((ListInitExpression)exp);
				     break;
	            default:
	                throw new Exception(string.Format("Unhandled expression type: '{0}'", exp.NodeType));
	        }
	    }
	
	    protected virtual void VisitBinding(MemberBinding binding)
	    {
	        switch (binding.BindingType)
	        {
	            case MemberBindingType.Assignment:
	                this.VisitMemberAssignment((MemberAssignment)binding);
					break;
	            case MemberBindingType.MemberBinding:
	                this.VisitMemberMemberBinding((MemberMemberBinding)binding);
					break;
	            case MemberBindingType.ListBinding:
	                this.VisitMemberListBinding((MemberListBinding)binding);
					break;
	            default:
	                throw new Exception(string.Format("Unhandled binding type '{0}'", binding.BindingType));
	        }
			throw new NotImplementedException ();
	    }
	
	    protected virtual void VisitElementInitializer(ElementInit initializer)
	    {
	        this.VisitExpressionList(initializer.Arguments);
	        throw new NotImplementedException ();
	    }
	
	    protected virtual void VisitUnary(UnaryExpression u)
	    {
	        this.Visit(u.Operand);
			throw new NotImplementedException ();
	    }
	
	    protected virtual void VisitBinary(BinaryExpression b)
	    {
	        this.Visit(b.Left);
	        this.Visit(b.Right);
	        this.Visit(b.Conversion);
	        throw new NotImplementedException ();
	    }
	
	    protected virtual void VisitTypeIs(TypeBinaryExpression b)
	    {
	        this.Visit(b.Expression);
			throw new NotImplementedException ();
	    }
	
	    protected virtual void VisitConstant(ConstantExpression c)
	    {
	        throw new NotImplementedException ();
	    }
	
	    protected virtual void VisitConditional(ConditionalExpression c)
	    {
	        this.Visit(c.Test);
            
	        this.Visit(c.IfTrue);
	        this.Visit(c.IfFalse);
	        throw new NotImplementedException ();
	    }
	
	    protected virtual void VisitParameter(ParameterExpression p)
	    {
	        throw new NotImplementedException ();
	    }
	
	    protected virtual void VisitMemberAccess(MemberExpression m)
	    {
	        this.Visit(m.Expression);
	        throw new NotImplementedException ();
	    }
	
	    protected virtual void VisitMethodCall(MethodCallExpression m)
	    {
	        this.Visit(m.Object);
	        this.VisitExpressionList(m.Arguments);
	     	throw new NotImplementedException ();   
	    }
	
	    protected virtual void VisitExpressionList(ReadOnlyCollection<Expression> original)
	    {
	        for (int i = 0, n = original.Count; i < n; i++)
	            this.Visit(original[i]);
	            
			throw new NotImplementedException ();
	    }
	
	    protected virtual void VisitMemberAssignment(MemberAssignment assignment)
	    {
	        this.Visit(assignment.Expression);
			throw new NotImplementedException ();
	    }
	
	    protected virtual void VisitMemberMemberBinding(MemberMemberBinding binding)
	    {
	        this.VisitBindingList(binding.Bindings);
			throw new NotImplementedException ();
	    }
	
	    protected virtual void VisitMemberListBinding(MemberListBinding binding)
	    {
	        this.VisitElementInitializerList(binding.Initializers);
	        throw new NotImplementedException ();
	    }
	
	    protected virtual void VisitBindingList(ReadOnlyCollection<MemberBinding> original)
	    {
	        for (int i = 0, n = original.Count; i < n; i++)
	           this.VisitBinding(original[i]);
	            
			throw new NotImplementedException ();
	    }
	
	    protected virtual void VisitElementInitializerList(ReadOnlyCollection<ElementInit> original)
	    {
	        for (int i = 0, n = original.Count; i < n; i++)
	            this.VisitElementInitializer(original[i]);
	            
			throw new NotImplementedException ();
	    }
	
	    protected virtual void VisitLambda(LambdaExpression lambda)
	    {
	        this.Visit(lambda.Body);
	        throw new NotImplementedException ();
	    }
	
	    protected virtual void VisitNew(NewExpression nex)
	    {
	        this.VisitExpressionList(nex.Arguments);
	        throw new NotImplementedException ();
	    }
	
	    protected virtual void VisitMemberInit(MemberInitExpression init)
	    {
	       this.VisitNew(init.NewExpression);
	       this.VisitBindingList(init.Bindings);
	       throw new NotImplementedException ();
	    }
	
	    protected virtual void VisitListInit(ListInitExpression init)
	    {
	       this.VisitNew(init.NewExpression);
	       this.VisitElementInitializerList(init.Initializers);
	       throw new NotImplementedException ();
	    }
	
	    protected virtual void VisitNewArray(NewArrayExpression na)
	    {
	        this.VisitExpressionList(na.Expressions);
	       	throw new NotImplementedException ();
	    }
	
	    protected virtual void VisitInvocation(InvocationExpression iv)
	    {
	        this.VisitExpressionList(iv.Arguments);
	        this.Visit(iv.Expression);
			throw new NotImplementedException ();
	    }
	}
}
