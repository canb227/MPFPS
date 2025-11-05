shader_type canvas_item;

uniform float vignette_radius : hint_range(0.0, 1.0) = 0.85;
uniform float vignette_softness : hint_range(0.0, 1.0) = 0.15;
uniform float vignette_intensity : hint_range(0.0, 1.0) = 0.0;

void fragment() {
    vec2 uv = SCREEN_UV * 2.0 - 1.0;
    float dist = length(uv);
    float vignette = smoothstep(vignette_radius, vignette_radius + vignette_softness, dist);
    COLOR = vec4(1.0, 0.0, 0.0, vignette * vignette_intensity);
}

