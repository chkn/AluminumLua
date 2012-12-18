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
		public void HelloColonOperator()
		{
			LuaParser parser;

			var context = new LuaContext();
			context.AddBasicLibrary();
			context.AddIoLibrary();

			parser = new LuaParser(context, "colonoperator.lua");
			parser.Parse();

			Assert.AreEqual(100, context.Get("Account").AsTable()["balance"].AsNumber());
		}
	}
}
