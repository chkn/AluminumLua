function test()
	if (2 > 1) and (2 ~= 1) then print "yes" else print "no!" end
	if (2 < 1) and (2 ~= 1) then print "yes" else print "no!" end
	if (1 > 1) or  (2 ~= 1) then print "yes" else print "no!" end
	if (1 > 1) or  (2 == 1) then print "yes" else print "no!" end
	if (2 > 1) or  (2 ~= 1) then print "yes" else print "no!" end
end
test()