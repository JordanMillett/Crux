#version 430

#include <common_frag.glsl>

//Take From Vertex
in vec3 passViewDirection;

//Non-Instanced Uniforms
uniform vec4 topColor = vec4(0.3f, 0.3f, 1.0f, 1.0f);
uniform vec4 bottomColor = vec4(1.0f, 1.0f, 1.0f, 1.0f);
uniform vec4 albedoHue = vec4(1.0, 1.0, 1.0, 1.0);

out vec4 outColor;

void main()
{
    vec3 dir = normalize(passViewDirection);
    float lerp = (dir.y + 1.0f) * 0.5f;
    
    outColor = mix(bottomColor, topColor, lerp);
}

