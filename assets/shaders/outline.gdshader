shader_type spatial;
render_mode unshaded, depth_test_disabled, fog_disabled;

uniform vec4 outline_color : source_color = vec4(0.2, 0.8, 1.0, 1.0);
uniform float thickness = 0.03;

void vertex() {
    NORMAL = normalize(NORMAL);
    VERTEX += NORMAL * thickness;
}

void fragment() {
    ALBEDO = outline_color.rgb;
    ALPHA = outline_color.a;
}
