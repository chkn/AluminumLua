using System;
using System.Linq;

namespace AluminumLua {
	
	public static class MainClass {
		
		public static void Main (string [] args)
		{
			LuaParser parser;
			
			var context = new LuaContext ();
			context.AddBasicLibrary ();
			context.AddIoLibrary ();
			
			if (args.Any ()) { // take first arg as file name
				parser = new LuaParser (context, args [0]);
				parser.Parse ();
				return;
			}
			
			// otherwise, run repl
			parser = new LuaParser (context);
			while (true) {
				try { 
					parser.Parse ();
				}
				catch (LuaException e) { 
					Console.WriteLine (e.Message);
				}
			}
		}
		
	}
	

}

