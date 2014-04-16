a = 1
b = 2

function log(...)
	local s = select(1, ...)
	for n = 2,select('#', ...) do
		s = s .. ' ' .. select(n, ...)
	end
	io.stdout:write(s .. "\n")
end

--log("require sockets")
--require("socket")
--
--s = socket.connect("www.google.co.uk", 80)
--
--s:settimeout(5)
--
--s:send("GET / HTTP/1.1\r\nHost: www.google.co.uk\r\nConnection: close\r\n\r\n")
--while true do
--	msg, err = s:receive()
--	if msg ~= nil then
--		log(msg)
--	else
--		log(err)
--		break
--	end
--end

log("initializing")
os.exit(1, true)

function test(a)
  log(a)
  if (a == 1) then return end
  if a % 2 == 0 then
    test(a/2)
  else
    test(a*3+1)
  end
end

log("running")

test(7)

function do_lua_stuff()
  test(10)
end
