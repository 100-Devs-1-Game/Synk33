shader_type spatial;
render_mode unshaded, depth_draw_opaque, cull_back, blend_mix;

uniform sampler2D screen_texture : hint_screen_texture, filter_linear;

uniform vec4 color : source_color = vec4(0.0, 0.3, 0.6, 1.0);             
uniform vec4 highlight_color : source_color = vec4(1.0, 1.0, 1.0, 1.0); 

uniform float frequency : hint_range(0.1, 10.0, 0.1) = 2.0;
uniform float compress : hint_range(0.5, 2.0, 0.01) = 0.9;
uniform float add_light : hint_range(0.0, 4.0, 0.1) = 1.5;
uniform float scale : hint_range(0.1, 3.0, 0.1) = 1.0;
uniform float speed : hint_range(-10.0, 10.0, 0.1) = 1.0;
uniform float x_angle : hint_range(-1.57, 1.57, 0.01) = 1.57;
uniform float y_angle : hint_range(-1.57, 1.57, 0.01) = 1.57;
uniform float base_alpha : hint_range(0.2, 1.0, 0.01) = 0.7;
uniform float refraction_ratio : hint_range(0.0, 50.0, 0.5) = 12.0;
uniform float clear : hint_range(0.0, 1.0, 0.05) = 0.6;                    
uniform float surf_sinpowder : hint_range(1.0, 8.0, 1.0) = 8.0;
uniform float surf_speed : hint_range(-10.0, 10.0, 0.1) = 1.0;
uniform float surf_angle : hint_range(-1.57, 1.57, 0.01) = 1.57;
uniform float surf_magnitude : hint_range(0.0, 5.0, 0.1) = 0.5;
uniform float s11 : hint_range(-10.0, 10.0, 0.01) = 2.221;
uniform float sinpowder : hint_range(1.0, 8.0, 1.0) = 8.0;

vec2 UVtoSreenUV(vec2 uv, bool is_camera_center, vec2 texture_pixel_size, vec2 screen_pixel_size, vec2 fragcoord) {
	vec2 suv = (uv - 0.5) * 2.0;
	if (is_camera_center) {
		return (fragcoord + suv * 0.01 / texture_pixel_size) * screen_pixel_size * 0.5;
	} else {
		return (fragcoord + suv * 0.01 / texture_pixel_size) * screen_pixel_size;
	}
}

vec2 shiftuv2(vec2 uv, float shiftratio) {
	vec2 suv = (uv - 0.5) * 2.0;
	return (suv + suv * -1.0 * shiftratio) * 0.5 + 0.5;
}

// B R U H
float yget(float x, float fc1, float fc2, float fc3, float fc4, float tc1, float tc2, float tc3, float tc4, float amc1, float amc2, float amc3, float amc4, float way, float addt) {
	float t = speed * TIME * way + addt;
	float y = sin(x * frequency);
	y += pow(abs(sin(x * frequency * fc1 + t * tc1)), sinpowder) * amc1;
	y += pow(abs(sin(x * frequency * fc2 + t * tc2)), sinpowder) * amc2;
	y += pow(abs(sin(x * frequency * fc3 + t * tc3)), sinpowder) * amc3;
	y += pow(abs(sin(x * frequency * fc4 + t * tc4)), sinpowder) * amc4;
	y /= (amc1 + amc2 + amc3 + amc4 + 1.0);
	return y;
}

void fragment() {
	vec2 st = (UV - 0.5) * 2.0 * scale;
	float dl = length(vec2(dFdx(st.x), dFdy(st.y)));
	vec2 st2 = ((UV - 0.5) * 2.0 + dl) * scale;
	float x = st.x;
	float y = st.y;
	
	float adda = pow(abs(sin(surf_speed * TIME * scale + x * sin(surf_angle) + y * cos(surf_angle))), surf_sinpowder) * surf_magnitude;
	st.x += adda * cos(surf_angle);
	st.y += adda * sin(surf_angle);
	
	
	float a1 = (
		yget(distance(vec2(0.0), st), 1.30, 1.72, s11, 3.1122, 1.0, 1.121, 0.437, 4.0, 4.5, 4.0, 5.0, 2.5, 1.0, 0.0) +
		yget(distance(vec2(0.5, 3.5), st), 1.32, 1.72, s11, 3.1122, 1.0, 1.121, 0.437, 4.269, 4.5, 4.0, 5.0, 2.5, -1.0, 0.0) +
		yget(distance(vec2(4.5, -4.5), st), 1.31, 1.72, s11, 3.1122, 1.0, 1.121, 0.437, 4.269, 4.5, 4.0, 5.0, 2.5, 1.0, 0.0) +
		yget(distance(vec2(-5.5, -2.5), st), 1.27, 1.72, s11, 3.1122, 1.0, 1.121, 0.437, 5.0, 4.5, 4.0, 5.0, 2.5, -1.0, 0.0) +
		yget(distance(vec2(-7.5, 3.5), st), 1.25, 1.72, s11, 3.1122, 1.0, 1.121, 0.437, 5.0, 4.5, 4.0, 5.0, 2.5, 1.0, 0.0) +
		yget(distance(vec2(3.25, 3.25), st), 1.34, 1.72, s11, 3.1122, 1.0, 1.121, 0.437, 4.269, 4.5, 4.0, 5.0, 2.5, 1.0, 0.0) +
		yget(distance(vec2(6.25, -4.25), st), 1.36, 1.72, s11, 3.1122, 1.0, 1.121, 0.437, 4.269, 4.5, 4.0, 5.0, 2.5, -1.0, 0.0) +
		yget(distance(vec2(-7.25, -1.25), st), 1.27, 1.72, s11, 3.1122, 1.0, 1.121, 0.437, 5.0, 4.5, 4.0, 5.0, 2.5, 1.0, 0.0) +
		yget(distance(vec2(-0.25, 5.25), st), 1.59, 1.72, s11, 3.1122, 1.0, 1.121, 0.437, 5.0, 4.5, 4.0, 5.0, 2.5, -1.0, 0.0)
	) / 9.0;
	
	float a2 = (
		yget(distance(vec2(0.0), st2), 1.30, 1.72, s11, 3.1122, 1.0, 1.121, 0.437, 4.0, 4.5, 4.0, 5.0, 2.5, 1.0, 0.0) +
		yget(distance(vec2(0.5, 3.5), st2), 1.32, 1.72, s11, 3.1122, 1.0, 1.121, 0.437, 4.269, 4.5, 4.0, 5.0, 2.5, -1.0, 0.0) +
		yget(distance(vec2(4.5, -4.5), st2), 1.31, 1.72, s11, 3.1122, 1.0, 1.121, 0.437, 4.269, 4.5, 4.0, 5.0, 2.5, 1.0, 0.0) +
		yget(distance(vec2(-5.5, -2.5), st2), 1.27, 1.72, s11, 3.1122, 1.0, 1.121, 0.437, 5.0, 4.5, 4.0, 5.0, 2.5, -1.0, 0.0) +
		yget(distance(vec2(-7.5, 3.5), st2), 1.25, 1.72, s11, 3.1122, 1.0, 1.121, 0.437, 5.0, 4.5, 4.0, 5.0, 2.5, 1.0, 0.0) +
		yget(distance(vec2(3.25, 3.25), st2), 1.34, 1.72, s11, 3.1122, 1.0, 1.121, 0.437, 4.269, 4.5, 4.0, 5.0, 2.5, 1.0, 0.0) +
		yget(distance(vec2(6.25, -4.25), st2), 1.36, 1.72, s11, 3.1122, 1.0, 1.121, 0.437, 4.269, 4.5, 4.0, 5.0, 2.5, -1.0, 0.0) +
		yget(distance(vec2(-7.25, -1.25), st2), 1.27, 1.72, s11, 3.1122, 1.0, 1.121, 0.437, 5.0, 4.5, 4.0, 5.0, 2.5, 1.0, 0.0) +
		yget(distance(vec2(-0.25, 5.25), st2), 1.59, 1.72, s11, 3.1122, 1.0, 1.121, 0.437, 5.0, 4.5, 4.0, 5.0, 2.5, -1.0, 0.0)
	) / 9.0;
	
	float da = clamp((a2 - a1) * compress, 0.0, 1.0);
	float ang = atan(da / dl);
	
	vec4 bC = textureLod(screen_texture, UVtoSreenUV(shiftuv2(UV, ang * refraction_ratio), false, vec2(1.0), VIEWPORT_SIZE, FRAGCOORD.xy), 0.0);
	
	vec3 refract_tint = mix(bC.rgb, highlight_color.rgb, clear);
	refract_tint = refract_tint * highlight_color.rgb * (1.0 + add_light);  // shitty retarded fake caustics
	refract_tint = max(refract_tint, highlight_color.rgb * 0.2);            // Minimum glow even on dark areas, it doesn't even work.
	
	float a = ((sin(ang - PI * 0.5 + y_angle) + cos(ang - PI * 1.5 + x_angle)) + adda) / 1.5;
	
	ALPHA = clamp(base_alpha + a * 0.8, 0.0, 1.0);
	ALBEDO.rgb = clamp(color.rgb + refract_tint * ALPHA, vec3(0.0), vec3(1.5));
	
	// normal bump idea
	NORMAL = normalize(vec3(da * 0.6, da * 0.6, 1.0));
}