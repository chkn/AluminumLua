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
		
		protected bool interactive, eof;
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
			this.input = new StreamReader (Console.OpenStandardInput ());
			this.interactive = true;
			
			this.CurrentExecutor = executor;
		}
		
		public LuaParser (LuaContext ctx)
			: this (LuaSettings.Executor (ctx))
		{
		}
		
		public void Parse ()
		{
			do {
				
				var identifier = ParseLVal ();
				
				if (eof)
					break;
				
				switch (identifier) {
				
				case "function":
					scope_count++;
					ParseFunctionDef ();
					break;
				
				case "do":
					scope_count++;
					CurrentExecutor.PushScope ();
					break;
					
				case "end":
					if (--scope_count < 0) {
						scope_count = 0;
						Err ("unexpected 'end'");
					}
					CurrentExecutor.PopScope ();
					break;
					
				case "local":
					var name = ParseLVal ();
					if (!TryAssign (name, true))
						Err ("unexpected 'local'");
					break;
					
				case "return":
					ParseRVal ();
					CurrentExecutor.Return ();
					break;
					
				default:
					if (TryFunctionCall (identifier)) {
						CurrentExecutor.PopStack ();
						break;
					}
					
					if (!TryAssign (identifier, false))
						Err ("unknown identifier '{0}'", identifier);
					break;
				}
				
			} while (!eof && !interactive);
		}
		
		protected string ParseLVal ()
		{
			var val = ParseOne (false) as string;
			if (val == null && !eof)
				Err ("expected identifier");
			
			return val;
		}
		
		// FIXME: handle operators like concatenation and addition, etc.
		protected void ParseRVal ()
		{
			var expr = ParseOne (true);
			
			if (expr is string) {
				// function call or variable
				
				if (!TryFunctionCall ((string)expr)) {
					
					CurrentExecutor.Variable ((string)expr);
				}
					
			} else {
			
				CurrentExecutor.Constant ((LuaObject)expr);
			}
		}
		
		protected bool TryAssign (string identifier, bool localScope)
		{
			if (Peek () != '=')
				return false;
						
			Consume ();
			ParseRVal ();
			CurrentExecutor.Assign (identifier, localScope);
			
			return true;
		}
		
		protected bool TryFunctionCall (string identifier)
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
				
				return false;
			}
			
			if (!CurrentExecutor.CurrentScope.IsDefined (identifier))
				Err ("'{0}' is not defined", identifier);
			
			CurrentExecutor.Call (identifier, argCount);
			return true;
		}
					
		protected string ParseFunctionDef ()
		{
			var funcName = ParseLVal ();
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
			CurrentExecutor.PushFunctionScope (funcName, args.ToArray ());
			return funcName;
		}
		
		// Identifiers come out as strings; everything else as LuaObject
		protected object ParseOne (bool expr) 
		{
		top:
			object val;
			var la = Peek ();
			switch (la) {

			case (char)0: eof = true; break;

			case ';':
				Consume ();
				goto top;

			case '-':
				if (expr) {
					// speculatively parse number (might also be comment)
					val = ConsumeNumberLiteral ();
					if (val != null)
						return val;
				}
				ConsumeComment ();
				goto top;

			case '"' : if (expr) return ConsumeStringLiteral (); break;
			case '\'': if (expr) return ConsumeStringLiteral (); break;
			case '{' : if (expr) return ConsumeTableLiteral (); break;
			
			case '(':
				if (expr) {
					Consume ();
					val = ParseOne (true);
					Consume (')');
					return val;
				}
				break;
				
			default:
				if (char.IsLetter (la))
					return ConsumeIdentifier ();
				
				if (expr && (char.IsDigit (la) || la == '.' || la == '-'))
					return ConsumeNumberLiteral ();
				
				Err ("unexpected '{0}'", la);
				break;
			}
			
			return null; //?
		}
		
		// FIXME: Allow real RVals (ie. results from evaluating functions) to be items in tables
		// http://www.lua.org/pil/3.6.html
		protected LuaObject ConsumeTableLiteral ()
		{
			var table = new Dictionary<string, LuaObject>();
			string name = null;
			int i = 0;
			
			Consume ('{');
			var next = Peek ();
			
			while (next != '}' && !eof) {
				
				if (next == '[') {
				    Consume ();
					name = ConsumeStringLiteral ().AsString ();
					Consume (']');
					Consume ('=');
				}
				
				var item = ParseOne (true);
				if (item is string) {
					
					name = (string)item;
					Consume ('=');
					
					item = ParseOne (true);
				}
				
				table.Add (name ?? i.ToString (), (LuaObject)item);
				
				i++;
				name = null;
				next = Peek ();
				if (next == ',') {
					
					Consume ();
					next = Peek ();
					
				} else if (next != '}') {
					
					Err ("expecting ',' or '}'");
				}
            }
			
			Consume ('}');
            return LuaObject.FromTable (table);
		}
		
		protected LuaObject? ConsumeNumberLiteral ()
		{
			var next = Peek ();
			var sb = new StringBuilder ();
			
			while (char.IsDigit (next) || next == '-' || next == '.' || next == 'e' || next == 'E') {
				Consume ();
				sb.Append (next);
				next = Peek (true);
			}
			
			double val;
			if (double.TryParse (sb.ToString (), out val))
				return LuaObject.FromNumber (val);
			
			return null;
		}
		
		
		// FIXME: Handle multi line strings
		// http://www.lua.org/pil/2.4.html
		protected LuaObject ConsumeStringLiteral ()
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
		
		protected string ConsumeIdentifier ()
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
		
		protected void ConsumeComment ()
		{
			Consume ('-');
			Consume ('-');
			
			if (Consume () == '[' && Consume () == '[') {
				while (!eof && (Consume () != ']' || Consume () != ']')) { /* consume entire block comment */; }
			} else {
				eof = (input.ReadLine () == null);
				col = 1;
				row++;
			}
		}
		
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
			throw new LuaException (file_name, row, col, string.Format (message, args));
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

