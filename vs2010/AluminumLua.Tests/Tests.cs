using System;
using System.IO;

using NUnit.Framework;

namespace AluminumLua.Tests
{
	[TestFixture]
	public class Tests
	{
		[Test]
		public void DoFileTest()
		{
			LuaParser parser;

			var context = new LuaContext();
			context.AddBasicLibrary();
			context.AddIoLibrary();

			parser = new LuaParser(context, "dofile.lua");
			parser.Parse();
		}

		[Test]
		public void FunctionScopesTest()
		{
			LuaParser parser;

			var context = new LuaContext();
			context.AddBasicLibrary();
			context.AddIoLibrary();

			parser = new LuaParser(context, "functionscopes.lua");
			parser.Parse();
		}

		[Test]
		public void HelloWorldTest()
		{
			LuaParser parser;

			var context = new LuaContext();
			context.AddBasicLibrary();
			context.AddIoLibrary();

			parser = new LuaParser(context, "helloworld.lua");
			parser.Parse();
		}

		[Test]
		public void MethodBinding()
		{
			LuaParser parser;

			var context = new LuaContext();
			context.AddBasicLibrary();
			context.AddIoLibrary();
			double res = 0;
			var func = (Func<double, double>)((a) =>
				{
					res = a;
					return a + 1;
				});
			context.SetGlobal("test", LuaObject.FromDelegate(func));
			parser = new LuaParser(context, new StringReader("test(123)"));
			parser.Parse();
			Assert.AreEqual(123.0,res);
		}
	}
}
