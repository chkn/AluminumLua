/*
	LuaParser.cs: Handwritten Lua parser
	
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

// Please note: Work in progress.
// For instance, there is little support for Lua statement types beside function calls

using System;
using System.IO;
using System.Linq.Expressions;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using AluminumLua.Executors;

namespace AluminumLua {
	
	public class LuaParser {
		
		protected string file_name;
		protected TextReader input;
		
		protected bool eof, OptionKeyword;
		protected int row, col, scope_count;
		
		protected IExecutor CurrentExecutor { get; set; }
		
		public LuaParser (IExecutor executor, string file)
		{
			this.file_name = Path.GetFileName (file);
			this.input = File.OpenText (file);
			this.row = 1;
			this.col = 1;
			
			this.CurrentExecutor = executor;
		}
		
		public LuaParser (LuaContext ctx, string file)
			: this (LuaSettings.Executor (ctx), file)
		{
		}
		
		public LuaParser (IExecutor executor)
		{
			this.file_name = "stdin";
#if WINDOWS_PHONE
            this.input = Console.In;
#else
			this.input = new StreamReader (Console.OpenStandardInput ());
#endif
			this.CurrentExecutor = executor;
		}
		
		public LuaParser (LuaContext ctx)
			: this (LuaSettings.Executor (ctx))
		{
		}
		
		public void Parse ()
		{
			Parse (false);
		}
		
		public void Parse (bool interactive)
		{
			do {

				var identifier = ParseLVal ();
				
				if (eof)
					break;
				
				switch (identifier) {
				
				case "function":
					ParseFunctionDefStatement (false);
					break;
					
				case "do":
					scope_count++;
					CurrentExecutor.PushScope ();
					break;

                case "else":
				case "end":
					if (--scope_count < 0) {
						scope_count = 0;
						Err ("unexpected 'end'");
					}
                    OptionKeyword = identifier == "else";
					CurrentExecutor.PopScope ();
					break;
					
				case "local":
					var name = ParseLVal ();
					if (name == "function")
						ParseFunctionDefStatement (true);
					else
						ParseAssign (name, true);
					break;
					
				case "return":
					ParseRVal ();
					CurrentExecutor.Return ();
					break;
				
                case "if":
                    ParseConditionalStatement();
                    break;
				default:
					if (Peek () == '=') {
						ParseAssign (identifier, false);
						break;
					}
					
					CurrentExecutor.Variable (identifier);
					ParseLValOperator ();
					break;
				}
				
			} while (!eof && !interactive);
		}
		
		protected string ParseLVal ()
		{
			var val = ParseOne (false);
			if (val == null && !eof)
				Err ("expected identifier");
			
			return val;
		}
		
		protected void ParseLValOperator ()
		{
			var isTerminal = false;
			
			while (true) {
				
				switch (Peek ()) {
					
				case '[':
				case '.':
					ParseTableAccess ();
					isTerminal = false;
					
					// table assign is special
					if (Peek () == '=') {
						Consume ();
						ParseRVal ();
						CurrentExecutor.TableSet ();
						return;
					}
					break;
					
				case '(':
				case '{':
				case '"':
				case '\'':
					ParseCall ();
					isTerminal = true;
					break;
				
				default:
					if (isTerminal) {
						CurrentExecutor.PopStack ();
						return;
					}
					Err ("syntax error");
					break;
				}
			}
		}
		
		protected void ParseRVal ()
		{
			var identifier = ParseOne (true);
			
			if (identifier != null) {
				switch (identifier) {
					
				case "function":
					var currentScope = scope_count;
					CurrentExecutor.PushFunctionScope (ParseArgDefList ());
					scope_count++;
					
					while (scope_count > currentScope)
						Parse (true);
					break;
					
				case "not":
					ParseRVal ();
					CurrentExecutor.Negate ();
					break;
					
				default:
					CurrentExecutor.Variable (identifier);
					break;
				}
			}
			
			while (TryParseOperator ()) { /* do it */ }
		}
					
		protected bool TryParseOperator ()
		{
			var next = Peek ();
			
			switch (next) {
				
			case '[':
				ParseTableAccess ();
				break;
				
			case '.':
				ParseTableAccessOrConcatenation ();
				break;
				
			case '(':
			case '{':
			case '"':
			case '\'':
				ParseCall ();
				break;
					
			// FIXME: ORDER OF OPERATIONS!
			case '+':
				Consume ();
				ParseRVal ();
				CurrentExecutor.Add ();
				break;
				
			case '-':
				Consume ();
				ParseRVal ();
				CurrentExecutor.Subtract ();
				break;
				
			case '*':
				Consume ();
				ParseRVal ();
				CurrentExecutor.Multiply ();
				break;
				
			case '/':
				Consume ();
				ParseRVal ();
				CurrentExecutor.Divide ();
				break;

            case 'o':
            case 'O':
                Consume();
                if (char.ToLowerInvariant(Consume()) != 'r')
                    Err("unexpected 'o'");
                ParseRVal();
                CurrentExecutor.Or();
                break;

            case 'a':
            case 'A':
                Consume();
                if (char.ToLowerInvariant(Consume()) != 'n')
                    Err("unexpected 'a'");
                if (char.ToLowerInvariant(Consume()) != 'd')
                    Err("unexpected 'an'");
                ParseRVal();
                CurrentExecutor.And();
                break;
            case '>':
                Consume();
                bool OrEqual = Peek() == '=';
                if (OrEqual) Consume();
                ParseRVal();
                if (OrEqual) CurrentExecutor.GreaterOrEqual(); else CurrentExecutor.Greater();
                break;
            case '<':
                Consume();
                bool OrEqual2 = Peek() == '=';
                if (OrEqual2) Consume();
                ParseRVal();
                if (OrEqual2) CurrentExecutor.SmallerOrEqual(); else CurrentExecutor.Smaller();
                break;
			case '=':
				Consume ();
                if (Consume() != '=')
					Err ("unexpected '='");
                ParseRVal();
                CurrentExecutor.Equal();
				break;
				
			case '~':
				Consume ();
                if (Consume() != '=')
					Err ("unexpected '~'");
				ParseRVal();
                CurrentExecutor.NotEqual();
				break;
				
			default:
				return false;
			}
			
			return true;
		}
		
		protected void ParseAssign (string identifier, bool localScope)
		{
			Consume ('=');
			ParseRVal (); // push value
			
			CurrentExecutor.Assign (identifier, localScope);
		}
		
		protected void ParseCall ()
		{
			int argCount = 0;
			var next = Peek ();
			
			if (next == '"' || next == '\'' || next == '{') {
				// function call with 1 arg only.. must be string or table

				ParseRVal ();
				argCount = 1;
					
			} else if (next == '(') {
				// function call
				
				Consume ();
				
				next = Peek ();
				while (next != ')') {
				
					ParseRVal ();
					argCount++;
					
					next = Peek ();
					if (next == ',') {
						Consume ();
						next = Peek ();
						
					} else if (next != ')') {
					
						Err ("expecting ',' or ')'");
					}
				}
				
				Consume (')');
				
			} else {
				
				Err ("expecting string, table, or '('");
			}
			
			CurrentExecutor.Call (argCount);
		}

        protected void ParseConditionalStatement()
        {
            ParseRVal();
            if (!(
                char.ToLowerInvariant(Consume()) == 't' &&
                char.ToLowerInvariant(Consume()) == 'h' &&
                char.ToLowerInvariant(Consume()) == 'e' &&
                char.ToLowerInvariant(Consume()) == 'n')) Err("Expected 'then'");
            
            var currentScope = scope_count;
            CurrentExecutor.PushBlockScope();
            scope_count++;

            while (scope_count > currentScope && !eof)
                Parse(true);
            if (eof) { CurrentExecutor.PopScope(); OptionKeyword = false; }

            if (OptionKeyword)
            {
                currentScope = scope_count;
                CurrentExecutor.PushBlockScope();
                scope_count++;

                while (scope_count > currentScope && !eof)
                    Parse(true);
                if (eof) CurrentExecutor.PopScope();
            }
            else
            {
                CurrentExecutor.PushBlockScope();
                CurrentExecutor.PopScope();
            }

            CurrentExecutor.IfThenElse();
        }

		protected void ParseFunctionDefStatement (bool localScope)
		{
			var name = ParseLVal ();
			var next = Peek ();
			bool isTableSet = (next == '.');
			
			if (isTableSet) {
				
				CurrentExecutor.Variable (name); // push (first) table
				Consume (); // '.'
				CurrentExecutor.Constant (ParseLVal ()); // push key
				
				next = Peek ();
			
				while (next == '.') {
					
					CurrentExecutor.TableGet (); // push (subsequent) table
					Consume (); // '.'
					CurrentExecutor.Constant (ParseLVal ()); // push key
					
					next = Peek ();
				}
			}
			
			var currentScope = scope_count;
			CurrentExecutor.PushFunctionScope (ParseArgDefList ());
			scope_count++;
			
			while (scope_count > currentScope)
				Parse (true);
			
			if (isTableSet)
				CurrentExecutor.TableSet ();
			else
				CurrentExecutor.Assign (name, localScope);
		}
		
		// parses named argument list in func definition
		protected string [] ParseArgDefList ()
		{
			Consume ('(');
			
			var args = new List<string> ();
			var next = Peek ();
			
			while (next != ')') {
			
				args.Add (ParseLVal ());
				
				next = Peek ();
				if (next == ',') {
					Consume ();
					next = Peek ();
					
				} else if (next != ')') {
				
					Err ("expecting ',' or ')'");
				}
			}
			
			Consume (')');
			return args.ToArray ();
		}
		
		// assumes that the table has already been pushed
		protected void ParseTableAccess ()
		{
			var next = Peek ();
			
			if (next != '[' && next != '.')
				Err ("expected '[' or '.'");
				
			Consume ();
			
			switch (next) {
				
			case '[':
				ParseRVal (); // push key
				Consume (']');
				break;
				
			case '.':
				CurrentExecutor.Constant (ParseLVal ()); // push key
				break;
				
			}
			
			if (Peek () != '=')
				CurrentExecutor.TableGet ();
		}
		
		// assumes that the first item has already been pushed
		protected void ParseTableAccessOrConcatenation ()
		{
			Consume ('.');
			var next = Peek ();
			
			if (next == '.') {
				// concatenation
				
				Consume ();
				ParseRVal ();
				CurrentExecutor.Concatenate ();
			
			} else {
				// table access
				
				CurrentExecutor.Constant (ParseLVal ());
				CurrentExecutor.TableGet ();
				
			}
		}
		
		// -----
		
		// Parses a single value or identifier
		// Identifiers come out as strings
		protected string ParseOne (bool expr) 
		{
		top:
			var la = Peek ();
			switch (la) {

			case (char)0: eof = true; break;

			case ';':
				Consume ();
				goto top;

			case '-':
				Consume ();
				if (Peek () == '-') {
					ParseComment ();
					goto top;
					
				} else if (expr) {
					
					ParseRVal ();
					CurrentExecutor.Constant (LuaObject.FromNumber (-1));
					CurrentExecutor.Multiply ();	
				}
				break;

			case '"' : if (expr) CurrentExecutor.Constant (ParseStringLiteral ()); break;
			case '\'': if (expr) CurrentExecutor.Constant (ParseStringLiteral ()); break;
			case '{' : if (expr) ParseTableLiteral (); break;
			
			case '(':
				if (expr) {
					Consume ();
					ParseRVal ();
					Consume (')');
				}
				break;
				
			default:
				if (char.IsLetter (la))
					return ParseIdentifier ();
				
				if (expr && (char.IsDigit (la) || la == '.'))
					CurrentExecutor.Constant (ParseNumberLiteral ());
				else
					Err ("unexpected '{0}'", la);
				break;
			}
			
			return null; //?
		}
		
		// http://www.lua.org/pil/3.6.html
		protected void ParseTableLiteral ()
		{	
			Consume ('{');
			
			int i = 1;
			int count = 0;
			var next = Peek ();
			
			while (next != '}' && !eof) {
				count++;
				
				if (next == '[') {
				    Consume ();
					ParseRVal ();
					Consume (']');
					Consume ('=');
					
				} else { //array style
				
					CurrentExecutor.Constant (LuaObject.FromNumber (i++));
				}
				
				ParseRVal ();
				
				next = Peek ();
				if (next == ',') {
					
					Consume ();
					next = Peek ();
					
				} else if (next != '}') {
					
					Err ("expecting ',' or '}'");
				}
            }
			
			CurrentExecutor.TableCreate (count);
			
			Consume ('}');
		}
		
		protected LuaObject ParseNumberLiteral ()
		{
			var next = Peek ();
			var sb = new StringBuilder ();
			
			while (char.IsDigit (next) || next == '-' || next == '.' || next == 'e' || next == 'E') {
				Consume ();
				sb.Append (next);
				next = Peek (true);
			}
			
			var val = double.Parse (sb.ToString ());
			return LuaObject.FromNumber (val);
		}
		
		
		// FIXME: Handle multi line strings
		// http://www.lua.org/pil/2.4.html
		protected LuaObject ParseStringLiteral ()
		{
			var next = Peek ();
			var sb = new StringBuilder ();
			var escaped = false;
			
			if (next != '"' && next != '\'')
				Err ("expected string");
			
			var quote = next;
			
			Consume ();
			next = Consume (); 
			
			while (next != quote || escaped) {
				
				if (!escaped && next == '\\') {
					
					escaped = true;
					
				} else if (escaped) {
					
					switch (next) {
					
					case 'a' : next = '\a'; break;
					case 'b' : next = '\b'; break;
					case 'f' : next = '\f'; break;
					case 'n' : next = '\n'; break;
					case 'r' : next = '\r'; break;
					case 't' : next = '\t'; break;
					case 'v' : next = '\v'; break;
					
					}
					
					sb.Append (next);
					escaped = false;
				} else {
					
					sb.Append (next);
				}
				
				next = Consume ();
			}
			
			return LuaObject.FromString (sb.ToString ());
		}
		
		protected string ParseIdentifier ()
		{
			var next = Peek ();
			var sb = new StringBuilder ();
			
			do {
				Consume ();
				sb.Append (next);
				next = Peek (true);
			} while (char.IsLetterOrDigit (next) || next == '_');
			
			return sb.ToString ();
		}
		
		protected void ParseComment ()
		{
			Consume ('-');
			if (Peek () == '-')
					Consume ();
			
			if (Consume () == '[' && Consume () == '[') {
				while (!eof && (Consume () != ']' || Consume () != ']')) { /* consume entire block comment */; }
			} else {
				eof = (input.ReadLine () == null);
				col = 1;
				row++;
			}
		}
		
		// scanner primitives:
		
		protected void Consume (char expected)
		{
			var actual = Peek ();
			if (eof || actual != expected)
				Err ("expected '{0}'", expected);
			
			while (Consume () != actual)
				{ /* eat whitespace */ }
			
		}
		
		protected char Consume ()
		{
			var r = input.Read ();
			if (r == -1) {
				eof = true;
				return (char)0;
			}
			
			col++;
			return (char)r;
		}
		
		protected char Peek ()
		{
			return Peek (false);
		}
		
		protected char Peek (bool whitespaceSignificant)
		{
		top:
			var p = input.Peek ();
			if (p == -1) {
				eof = true;
				return (char)0;
			}
			
			var la = (char)p;
			
			if (!whitespaceSignificant) {
				if (la == '\r') {
					input.Read ();
					if (((char)input.Peek ()) == '\n')
						input.Read ();
					
					col = 1;
					row++;
					goto top;
				}
				
				if (la == '\n') {
					input.Read ();
					col = 1;
					row++;
					goto top;
				}
				
				if (char.IsWhiteSpace (la)) {
					Consume ();
					goto top;
				}
			}
			
			return la;
		}
		
		
		// convenience methods:
		
		
		protected void Err (string message, params object [] args)
		{
			Consume ();
			throw new LuaException (file_name, row, col - 1, string.Format (message, args));
		}
	}
	
	public class LuaException : Exception {
		
		public LuaException (string file, int row, int col, string message) 
			: base (string.Format ("Error in {0}({1},{2}): {3}", file, row, col, message))
		{
		}
		
		public LuaException (string message)
			: base ("Error (unknown context): " + message)
		{	
		}
	}
}

