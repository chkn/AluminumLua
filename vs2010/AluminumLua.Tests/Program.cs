using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AluminumLua.Tests
{
	class Program
	{
		static void Main(string[] args)
		{
			TestFile("dofile.lua");
			TestFile("functionscopes.lua");
			TestFile("helloworld.lua");
			TestFile("test.lua");
			Console.ReadKey();
		}

		private static void TestFile(string fileName)
		{
			var ctx = new AluminumLua.LuaContext();
			ctx.AddBasicLibrary();
			ctx.AddIoLibrary();
			var alua = new AluminumLua.LuaParser(ctx, fileName);

			alua.Parse();
		}
	}
}
