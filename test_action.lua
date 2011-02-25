-- Register 'test' action with Premake

newaction {

	trigger     = "test",
	description = "Run tests; must build first",
	execute     = function ()
		print ("")
	
		local alua_exe = path.getabsolute "alua.exe"
		local test_dir = "test"
		os.chdir (test_dir)
	
		if (not os.isfile (alua_exe)) then
			print (alua_exe.." not found; did you forget to build?")
			return
		end
		
		local tests = os.matchfiles "**.lua"
		local cmd   = iif (os.is "windows", "", "mono ") .. alua_exe
		
		local passed = 0
		local failed = 0
		local total  = 0
		for _,test in ipairs (tests) do
			total = total + 1
			
			local alua = io.popen (cmd.." "..test)
			local txt  = io.open (test..".txt")
			local line = 1
			local didfail = false
			
			for actual in alua:lines () do
				local expected = txt:read "*l"
				
				if expected == nil then
					print ("test:\t", test.." (output line "..line..")")
					print ("expected:", "<end of output>")
					print ("but got:", actual)
					didfail = true
					break
				end
				
				if actual ~= expected then
					print ("test:\t",test.." (output line "..line..")")
					print ("expected:", expected)
					print ("but got:", actual)
					didfail = true
					break
				end
				
				line = line + 1
			end
			
			if didfail then
				failed = failed + 1
			else
				if line > 1 then
					passed = passed + 1
				end
			end
			
			alua:close ()
			txt:close ()
			print ("")
		end
		
		print ("Results: out of "..total.." tests...",passed.." passed",failed.." failed")
	end
}