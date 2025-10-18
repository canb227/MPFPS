shader_type spatial;

uniform sampler2D screen_tex : source_color;
global uniform float time;
uniform float curvature = 0.2;
uniform float scanline_strength = 0.2;
uniform float vignette_strength = 0.3;

void fragment() {
    //crt curvage
    vec2 uv = UV * 2.0 - 1.0;
    float r = length(uv);
    uv = uv * (0.75 + curvature * r * r);
    uv = uv * 0.5 + 0.5;

    //sample viewport
    vec4 col = texture(screen_tex, uv);

    //scanline thing
    float scan = sin(UV.y * 800.0 + time * 10.0) * 0.5 + 0.5;
    col.rgb *= mix(1.0, scan, scanline_strength);

    float dist = distance(UV, vec2(0.5));
    col.rgb *= 1.0 - vignette_strength * dist;

    ALBEDO = col.rgb;
    EMISSION = col.rgb;
}
