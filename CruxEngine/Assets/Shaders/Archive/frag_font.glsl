#version 430

uniform sampler2D ColorTexture;
uniform vec4 TextureHue = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec2 AtlasScale;

in vec2 fragUV;
flat in vec2 vAtlasOffset; // Instance-specific offset (flat-qualified for better performance)

out vec4 FragColor;

void main()
{
    vec2 uv = fragUV / AtlasScale + vAtlasOffset; // Adjust UV for atlas
    vec4 sampled = texture(ColorTexture, uv);

    if (length(sampled.rgb) < 0.75)
        discard;

    FragColor = sampled * TextureHue;
}
