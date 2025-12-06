shader_type canvas_item;




uniform bool greyscale;

float colorimetric_greyscale(vec3 color){
	// Some bullshit magic number coefficents I stole from wikipedia
	// Supposedly represent the luminance at which humans see each color for the
	// sRGB colorspace
	const vec3 MAGIC_COEFFICENTS = vec3(.2126, .7152, .0722);
	// Gamma compression is also some bullshit wikipedia told me to do
	const float GAMMA_COMPRESSION_RATE = 2.0;
	const float GAMMA_RANGE = 0.39;

	color *= MAGIC_COEFFICENTS;

	float value = color.r + color.g + color.b;
	value = GAMMA_RANGE * pow(value, GAMMA_COMPRESSION_RATE);
	return value;
}

void fragment() {
	vec4 col = COLOR;
	if (greyscale){
		col.rgb = vec3(colorimetric_greyscale(col.rgb));
	}
	COLOR = col;
	// Called for every pixel the material is visible on.
}
