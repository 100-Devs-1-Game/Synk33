shader_type spatial;

/*
Hold ribbon wiggle with axis toggle:
- wiggle_mode = 0 -> side-to-side (local X)
- wiggle_mode = 1 -> up-down (local up_axis)
*/

render_mode unshaded, cull_disabled, blend_mix, depth_draw_opaque, depth_prepass_alpha;

uniform float amp = 0.10;
uniform float freq = 14.0;
uniform float speed = 6.0;
uniform float end_fade = 0.12;

uniform vec4 core_color : source_color = vec4(0.6, 1.0, 0.9, 1.0);
uniform float emission_strength = 6.0;

// Which direction is "up" for your mesh (also used for z-fight bias and vertical wiggle)
uniform vec3 up_axis = vec3(0.0, 0.0, 1.0);
uniform float z_bias = 0.002;

// Axis toggle (acts like a checkbox in inspector)
uniform bool up_to_bottom = false; // false = side-to-side, true = up-down

float envelope(float u) {
    float a = smoothstep(0.0, end_fade, u);
    float b = smoothstep(0.0, end_fade, 1.0 - u);
    return a * b;
}

void vertex() {
    float u = UV.y;
    float e = envelope(u);

    vec3 up = normalize(up_axis);

    // lift a tiny bit to avoid z-fighting with the track
    VERTEX += up * z_bias;

    float w = sin(TIME * speed + u * freq) * amp;

    // choose wiggle axis
    vec3 axis = up_to_bottom ? up : vec3(1.0, 0.0, 0.0);

    VERTEX += axis * (w * e);
}

void fragment() {
    float v = clamp(UV.x, 0.0, 1.0);
    float edge = 1.0 - abs(v * 2.0 - 1.0);
    edge = pow(max(edge, 0.0), 2.2);

    ALBEDO = core_color.rgb;
    EMISSION = core_color.rgb * (emission_strength * edge);
    ALPHA = core_color.a * edge;
}