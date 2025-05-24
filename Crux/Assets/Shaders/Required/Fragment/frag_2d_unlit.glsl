#version 430

#include <common_frag.glsl>

//Take From Vertex
in vec2 passUV;
in vec3 passViewDirection;

//Take From Vertex - Per Instance
#ifdef INSTANCED
flat in vec4 instHue;
#endif

//Non-Instanced Uniforms
uniform sampler2D albedoTexture; //TextureUnit.Texture0
uniform vec4 albedoHue = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec2 tiling = vec2(1.0, 1.0);

out vec4 outColor;

void main()
{
    //vec4 computedColor = texture(albedoTexture, passUV * tiling);
    vec4 computedColor = vec4(1.0);

    #ifdef INSTANCED
        computedColor = computedColor * instHue;
    #else
        computedColor = computedColor * albedoHue;
    #endif

    //if (computedColor.a < 0.9)
        //discard;

    outColor = computedColor;
}

