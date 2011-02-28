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
	
	using LuaTable     = IDictionary<LuaObject,LuaObject>;
	using LuaTableImpl = Dictionary<LuaObject,LuaObject>;
	using LuaTableItem = KeyValuePair<LuaObject,LuaObject>;
	
	public delegate LuaObject LuaFunction (LuaObject [] args);
	
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
	
	public struct LuaObject : IEnumerable<KeyValuePair<LuaObject,LuaObject>>, IEquatable<LuaObject>	{
		
		private object luaobj;
		private LuaType type;
		
		// pre-create some common values
		public static readonly LuaObject Nil         = new LuaObject ();
		public static readonly LuaObject True        = new LuaObject { luaobj = true, type = LuaType.boolean };
		public static readonly LuaObject False       = new LuaObject { luaobj = false, type = LuaType.boolean };
		public static readonly LuaObject Zero        = new LuaObject { luaobj = 0d, type = LuaType.number };
		public static readonly LuaObject EmptyString = new LuaObject { luaobj = "", type = LuaType.@string };
		
		public LuaType Type { get { return type; } }
		
		public bool Is (LuaType type)
		{
			return this.type == type;
		}
		
		
		public static LuaObject FromBool (bool bln)
		{
			if (bln)
				return True;
			
			return False;
		}
		public static implicit operator LuaObject (bool bln)
		{
			return FromBool (bln);
		}
		
		public static LuaObject FromNumber (double number)
		{
			if (number == 0d)
				return Zero;
			
			return new LuaObject { luaobj = number, type = LuaType.number };
		}
		public static implicit operator LuaObject (double number)
		{
			return FromNumber (number);
		}
		
		public static LuaObject FromString (string str)
		{
			if (str == null)
				return Nil;
			
			if (str.Length == 0)
				return EmptyString;
			
			return new LuaObject { luaobj = str, type = LuaType.@string };
		}
		public static implicit operator LuaObject (string str)
		{
			return FromString (str);
		}
		
		public static LuaObject FromTable (LuaTable table)
		{
			if (table == null)
				return Nil;
			
			return new LuaObject { luaobj = table, type = LuaType.table };
		}
		public static LuaObject NewTable (params LuaTableItem [] initItems)
		{
			var table = FromTable (new LuaTableImpl ());
			
			foreach (var item in initItems)
				table [item.Key] = item.Value;
			
			return table;
		}
		
		public static LuaObject FromFunction (LuaFunction fn)
		{
			if (fn == null)
				return Nil;
			
			return new LuaObject { luaobj = fn, type = LuaType.function };
		}
		public static implicit operator LuaObject (LuaFunction fn)
		{
			return FromFunction (fn);
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
		public LuaTable AsTable () {
			return luaobj as LuaTable;
		}

		
        public IEnumerator<KeyValuePair<LuaObject,LuaObject>> GetEnumerator ()
        {
			var table = luaobj as IEnumerable<KeyValuePair<LuaObject,LuaObject>>;
			if (table == null)
				return null;
			
			return table.GetEnumerator ();
        }
		
		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}
		
		public LuaObject this [LuaObject key] {
			get {
				var table = AsTable ();
				if (table == null)
					throw new LuaException ("cannot index non-table");
				
				// we don't care whether the get was successful, because the default LuaObject is nil.
				LuaObject result;
				table.TryGetValue (key, out result);
				return result;
			}
			set {
				var table = AsTable ();
				if (table == null)
					throw new LuaException ("cannot index non-table");
				
				table [key] = value;
			}
		}
		
		// Unlike AsString, this will return string representations of nil, tables, and functions
		public override string ToString ()
		{
			if (IsNil)
				return "nil";
			
			if (IsTable)
				return "{ " + string.Join (", ", AsTable ().Select (kv => string.Format ("[{0}]={1}", kv.Key, kv.Value.ToString ())).ToArray ()) + " }";
			
			if (IsFunction)
				return AsFunction ().Method.ToString ();
			
			return luaobj.ToString ();
		}
		
		// See last paragraph in http://www.lua.org/pil/13.2.html
		public bool Equals (LuaObject other)
		{
			// luaobj will not be null unless type is Nil
			return (other.type == type) && (luaobj == null || luaobj.Equals (other.luaobj));
		}
		
		public override bool Equals (object obj)
		{
			if (obj is LuaObject)
				return Equals ((LuaObject)obj);
			
			// FIXME: It would be nice to automatically compare other types (strings, ints, doubles, etc.) to LuaObjects.
			return false;
		}

		public override int GetHashCode ()
		{
			unchecked {
				return (luaobj != null ? luaobj.GetHashCode () : 0) ^ type.GetHashCode ();
			}
		}
    }
}

