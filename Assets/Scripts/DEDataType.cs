public enum DEDirection : byte
{
	up,
	down,
	left, //x
	right,
	forward, //y
	back,
}


public enum DEObjectType : byte
{
	cube,
	quad,
	trigger, //an event trigger
	texture,
	sprite, //a fake 3D object using sprite
	character,
}

