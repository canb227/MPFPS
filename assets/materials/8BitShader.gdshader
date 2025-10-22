shader_type canvas_item;

uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;
uniform int pixel_size : hint_range(1, 32) = 4;

// Game Boy palette (you can tweak these)
const vec3 gb_colors[4] = vec3[4](
    vec3(15.0/255.0, 56.0/255.0, 15.0/255.0),   // darkest green
    vec3(48.0/255.0, 98.0/255.0, 48.0/255.0),   // dark green
    vec3(139.0/255.0, 172.0/255.0, 15.0/255.0), // light green
    vec3(155.0/255.0, 188.0/255.0, 15.0/255.0)  // lightest green
);

void fragment() {
    // Pixelation
    vec2 uv = SCREEN_UV;
    //uv = floor(uv * float(pixel_size)) / float(pixel_size);
    vec4 col = texture(SCREEN_TEXTURE, uv);

    // Convert to grayscale brightness
    float gray = dot(col.rgb, vec3(0.299, 0.587, 0.114));

    // Map brightness to nearest Game Boy color
    int index = int(floor(gray * 4.0));
    index = clamp(index, 0, 3);
    col.rgb = gb_colors[index];

    COLOR = col;
}
