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
            var ctx = new AluminumLua.LuaContext();
            ctx.AddBasicLibrary();
            ctx.AddIoLibrary();
            var alua = new AluminumLua.LuaParser(ctx, "test.lua");
            
            alua.Parse();
            Console.ReadKey();
		}
	}
}
