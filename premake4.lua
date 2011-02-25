--
-- AluminumLua build configuration
--

solution "AluminumLua"
	language "C#"
	framework "3.5"
	configurations { "Release", "Debug" }
	
	project "AluminumLua"
		kind "SharedLib"
		links {
			"System",
			"System.Core"
		}
		
		files {
			"src/**.cs"
		}
		
		excludes {
			"src/Main.cs"
		}
		
		configuration { "Debug*" }
			defines { "DEBUG" }
			flags { "Symbols" }
			
		configuration { "Release" }
			flags { "Optimize" }
	
	project "alua"
		kind "ConsoleApp"
		links {
			"System",
			"System.Core"
		}
		
		files {
			"src/**.cs"
		}
		
		configuration { "Debug*" }
			defines { "DEBUG" }
			flags { "Symbols" }
			
		configuration { "Release" }
			flags { "Optimize" }


	-- add test action
	dofile "test_action.lua"
	
	-- make sure all products are removed on clean
	if _ACTION == "clean" then
		mdbs = os.matchfiles "*.mdb"
		for _,file in ipairs (mdbs) do
			os.remove (file)
		end
	end
