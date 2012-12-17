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
	}
}
