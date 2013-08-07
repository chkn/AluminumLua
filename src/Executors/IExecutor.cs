/*
	IExecutor.cs: Decouples execution from parsing
	
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
	
	using LuaTable     = IDictionary<LuaObject,LuaObject>;
	
	// All IExecutor implementations should have a constructor that takes a LuaContext
	public interface IExecutor {
		
		// scoping:
		void PushScope                      ();
		void PushBlockScope                 (); // a block of code that may be repeated (ex. loop body)
		void PushFunctionScope              (string [] argNames);
		LuaContext CurrentScope             { get; }
		void PopScope                       (); // if it was a function or block scope, function object is left on stack

		
		// expressions:
		void Constant                       (LuaObject value); // pushes value
		void Variable                       (string identifier); // pushes value
		void Call                           (int argCount); // pops arg * argCount, pops function; pushes return value
		void TableCreate                    (int initCount); // pops (value, key) * initCount; pushes table
		void TableGet                       (); // pops key, pops table, pushes value
		
		void Concatenate                    (); // pops <value2>, pops <value1>; pushes <value1><value2>
		void Negate                         (); // pops value; pushes negated value (boolean)
        void And                            (); // pops <val2>, pops <val1>; pushes <val1> && <val2> (bool)
        void Or                             (); // pops <val2>, pops <val1>; pushes <val1> || <val2> (bool)
        void Equal                          (); // pops <val2>, pops <val1>; pushes <val1> == <val2> (bool)
        void NotEqual                       (); // pops <val2>, pops <val1>; pushes <val1> != <val2> (bool)
        void IfThenElse                     (); // pops <falseFunc>, pops <trueFunc>, pops <cond>; pushes <cond> ? <trueFunc> : <falseFunc> (function)

        void Greater                        (); // pops <val2>, pops <val1>; pushes <val1> > <val2> (bool)
        void Smaller                        (); // pops <val2>, pops <val1>; pushes <val1> < <val2> (bool)
        void GreaterOrEqual                 (); // pops <val2>, pops <val1>; pushes <val1> >= <val2> (bool)
        void SmallerOrEqual                 (); // pops <val2>, pops <val1>; pushes <val1> <= <val2> (bool)

		void Add                            (); // pops <val2>, pops <val1>; pushes <val1> + <val2> (numeric)
		void Subtract                       (); // pops <val2>, pops <val1>; pushes <val1> - <val2> (numeric)
		void Multiply                       (); // pops <val2>, pops <val1>; pushes <val1> * <val2> (numeric)
		void Divide                         (); // pops <val2>, pops <val1>; pushes <val1> / <val2> (numeric)
		
		// statements:
		void PopStack                       (); // pops and discards value
		void Assign                         (string identifier, bool localScope); // pops a value
		void TableSet                       (); // pops value, pops key, pops table, sets table.key = value
		void Return                         (); // pops a value

        void ColonOperator();

		// to execute: (some IExecutors - like InterpreterExecutor - may have executed instructions as they came in)
		LuaObject Result ();

		void ColonOperator();
	}
}

