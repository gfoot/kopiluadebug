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

log("start debugger")
require("mobdebug").start()

log("init")

move_rate = 10
turn_rate = 90

pos_x = 0
pos_y = 0
heading = 0
pen = true
stack = {}

moved_since_pen_down = false

function left(amount)
	right(-amount)
end

function right(amount)
	log("right", amount)

	local oldpen = pen
	if pen and moved_since_pen_down then penup() end

	local sign = 1
	if amount < 0 then
		sign = -1
		amount = -amount
	end

	local oheading = heading
	
	local moved = 0
	while moved < amount do
		moved = moved + turn_rate
		if moved > amount then moved = amount end
		heading = oheading + moved * sign
		coroutine.yield()
	end

	if oldpen and not pen then pendown() end
end

function forward(amount)
	log("forward", amount)
	
	local dx = math.sin(math.rad(heading))
	local dy = math.cos(math.rad(heading))
	
	if amount < 0 then
		dx = -dx
		dy = -dy
		amount = -amount
	end

	moved_since_pen_down = true

	local opos_x = pos_x
	local opos_y = pos_y
	
	local moved = 0
	while moved < amount do
		moved = moved + move_rate
		if moved > amount then moved = amount end
		pos_x = opos_x + moved * dx
		pos_y = opos_y + moved * dy
		coroutine.yield()
	end
end

function backward(amount)
	forward(-amount)
end

function penup()
	pen = false
	coroutine.yield()
end

function pendown()
	moved_since_pen_down = false
	pen = true
	coroutine.yield()
end
	
function push()
	local data = {
		pos_x = pos_x,
		pos_y = pos_y,
		heading = heading
	}
	table.insert(stack, data)
end

function pop()
	local oldpen = pen
	if pen then penup() end
		
	local data = table.remove(stack)
	pos_x = data.pos_x
	pos_y = data.pos_y
	heading = data.heading
		
	if oldpen then pendown() end
end


cr = nil

function runcode(code)
	cr = coroutine.create(code)
end


function update()
	if cr == nil then
		return nil
	end

	result, error = coroutine.resume(cr)
		
	if coroutine.status(cr) == "dead" then
		log("cr died")
		cr = nil
	end

	if result then
		return nil
	else
		log("error:", error)
		return error
	end
end



function testpenupdown()
	forward(1)
	penup()
	forward(1)
	pendown()
	right(90)
	forward(1)
	right(90)
	penup()
	forward(1)
	pendown()
	forward(1)
end

function spyro()
	for n = 1, 1000 do
		forward(10)
		left(11)
		forward(2)
		right(180)
		forward(2)
		left(11)
	end
end

function treething()
	function f(depth, length)
		if (depth > 0) then
			local d = length/3
			local p = (length-d)/2
			f(depth-1, p)
			right(45)
			f(depth-1, d*math.sqrt(2))
			right(135)
			f(depth-1, d)
			right(135)
			f(depth-1, d*math.sqrt(2))
			right(45)
			f(depth-1, p)
		else
			forward(length)
		end
	end

	f(4, 10)
end

function hilbert()
	function bend(depth, length, dir)
		if (depth == 0) then return end
		left(90*dir)
		bend(depth-1, length, -dir)
		forward(length)
		right(90*dir)
		bend(depth-1, length, dir)
		forward(length)
		bend(depth-1, length, dir)
		right(90*dir)
		forward(length)
		bend(depth-1, length, -dir)
		left(90*dir)
	end

	depth = 4
	bend(depth, 10 / math.pow(3, depth), 1)
end

function sierpinski()
	function f(depth, length, dir)
		if (depth == 0) then forward(length) return end
		left(60*dir)
		f(depth-1, length, -dir)
		right(60*dir)
		f(depth-1, length, dir)
		right(60*dir)
		f(depth-1, length, -dir)
		left(60*dir)
	end

	depth = 6
	f(depth, 10 / math.pow(2, depth), 1)
end

function plant()
	function x(depth, length)
		if (depth == 0) then return end

		-- F-[[X]+X]+F[+FX]-X
		f(depth-1, length)
		left(20)
		push()
		push()
		x(depth-1, length)
		pop()
		right(25)
		x(depth-1, length)
		pop()
		right(23)
		f(depth-1, length)
		push()
		right(25)
		f(depth-1, length)
		x(depth-1, length)
		pop()
		left(20)
		x(depth-1, length)
	end

	function f(depth, length)
		if (depth == 0) then forward(length) return end
		f(depth-1, length)
		f(depth-1, length)
	end

	steps = 6
	x(steps, 10 / math.pow(2, steps))
end

runcode(plant)
