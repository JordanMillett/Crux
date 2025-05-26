#version 430

#include <common_frag.glsl>

//Take From Vertex
in vec2 passUV;

//Take From Vertex - Per Instance
flat in vec4 instHue;
flat in vec2 instAtlasOffset;

//Non-Instanced Uniforms
uniform sampler2D albedoTexture; //TextureUnit.Texture0
uniform vec4 albedoHue = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec2 atlasScale;

out vec4 outColor;

void main()
{
    vec2 uv = passUV / atlasScale + instAtlasOffset;
    vec4 sampled = texture(albedoTexture, uv);

    if (length(sampled.rgb) < 0.75)
        discard;

    outColor = sampled * instHue;
}

