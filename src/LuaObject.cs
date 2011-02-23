/*
	LuaObject.cs: Lua object abstraction
	
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
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace AluminumLua {
	
	// http://www.lua.org/pil/2.html
	public enum LuaType {
		nil,
		boolean,
		number,
		@string,
		userdata,
		function,
		thread,
		table
	};
	
	public delegate LuaObject LuaFunction (LuaObject [] args);
	
	 public class LuaObject : IEnumerable<LuaObject> {
        private object luaobj;
		private LuaType type;
		
		public bool Is (LuaType type)
		{
			return this.type == type;
		}
		
		public LuaType Type { get { return type; } }
		
		public LuaObject (bool bln)
		{
			luaobj = bln;
			type = LuaType.boolean;
		}
		public static implicit operator LuaObject (bool bln)
		{
			return new LuaObject (bln);
		}
		
		public LuaObject (double number)
		{
			luaobj = number;
			type = LuaType.number;
		}
		public static implicit operator LuaObject (double number)
		{
			return new LuaObject (number);
		}
		
		public LuaObject (string str)
		{
			if (str != null) {
				luaobj = str;
				type = LuaType.@string;
			}
		}
		public static implicit operator LuaObject (string str)
		{
			return new LuaObject (str);
		}
		
		public LuaObject (IDictionary<string,LuaObject> table)
		{
			if (table != null) {
				luaobj = table;
				type = LuaType.table;
			}
		}
		
		public LuaObject (LuaFunction fn)
		{
			if (fn != null) {
				luaobj = fn;
				type = LuaType.function;
			}
		}
		public static implicit operator LuaObject (LuaFunction fn)
		{
			return new LuaObject (fn);
		}
		
		public bool IsNil      { get { return type == LuaType.nil; } }

		public bool IsBool     { get { return type == LuaType.boolean; } }
		public bool AsBool ()
		{
			if (luaobj == null)
				return false;
			
			if (luaobj is bool && ((bool)luaobj) == false)
				return false;
			
			return true;
		}
		
		public bool IsNumber   { get { return type == LuaType.number; } }
		public double AsNumber ()
		{
			return (double)luaobj;
		}
		
		public bool IsString   { get { return type == LuaType.@string; } }
		public string AsString ()
		{
			return luaobj.ToString ();
		}
		
		public bool IsFunction { get { return type == LuaType.function; } }
		public LuaFunction AsFunction ()
		{
			var fn = luaobj as LuaFunction;
			if (fn == null)
				throw new LuaException ("cannot call non-function");
			
			return fn;
		}
		
		public bool IsTable    { get { return type == LuaType.table; } }
		public IDictionary<string,LuaObject> AsTable () {
			return luaobj as IDictionary<string,LuaObject>;
		}

		
        public IEnumerator<LuaObject> GetEnumerator ()
        {
			return AsTable ().Values.GetEnumerator ();
        }
		
		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}
		
        public LuaObject this[int ix]
        {
            get {
                return AsTable ().Values.ElementAtOrDefault (ix);
            }
        }
		
        public LuaObject this[string index]
        {
            get {				
				LuaObject result;
				if (AsTable ().TryGetValue (index, out result))
					return result;
				
				return null;
            }
        }

		public override string ToString ()
		{
			if (IsNil)
				return "nil";
			
			if (IsTable)
				return "{ " + string.Join (", ", AsTable ().Select (kv => string.Format ("[\"{0}\"]={1}", kv.Key, kv.Value.ToString ())).ToArray ()) + " }";
			
			return IsString? "\"" + (string)luaobj + "\"" : luaobj.ToString ();
		}

    }
}

