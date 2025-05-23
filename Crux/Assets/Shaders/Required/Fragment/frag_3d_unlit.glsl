#version 430

#include <common_frag.glsl>

//Take From Vertex - Per Instance
#ifdef INSTANCED
flat in vec4 instHue;
#endif

//Non-Instanced Uniforms
uniform vec4 albedoHue = vec4(1.0, 1.0, 1.0, 1.0);

out vec4 outColor;

void main()
{       
    vec4 computedColor = vec4(0.0);

    #ifdef INSTANCED
        computedColor = instHue;
    #else
        computedColor = albedoHue;
    #endif
    
    if (computedColor.a < 0.9)
        discard;
    
    outColor = computedColor;
}