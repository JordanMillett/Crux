#version 430

#include <common_frag.glsl>

//Take From Vertex
in vec2 passUV;
in vec3 passViewDirection;

//Take From Vertex - Per Instance
#ifdef INSTANCED
flat in vec4 instHue;
flat in vec2 instUVOffset;
flat in vec2 instUVScale;
#endif

//Non-Instanced Uniforms
uniform sampler2D albedoTexture; //TextureUnit.Texture0
uniform vec4 albedoHue = vec4(1.0);

out vec4 outColor;

void main()
{
    vec2 uv;
    vec4 computedColor;

    #ifdef INSTANCED
        uv = passUV * instUVScale + instUVOffset;
        computedColor = texture(albedoTexture, uv) * instHue;
    #else
        uv = passUV;
        computedColor = texture(albedoTexture, uv) * albedoHue;
    #endif

    outColor = computedColor;
}