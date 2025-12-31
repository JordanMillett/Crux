#version 430

#include <common_frag.glsl>

//Take From Vertex
in vec3 passViewDirection;

//Non-Instanced Uniforms
uniform vec4 topColor = vec4(1.0f, 0.0f, 0.996f, 1.0f);
uniform vec4 middleColor = vec4(1.0f, 0.0f, 0.996f, 1.0f);
uniform vec4 bottomColor = vec4(1.0f, 0.0f, 0.996f, 1.0f);

uniform vec4 albedoHue = vec4(1.0, 1.0, 1.0, 1.0);

out vec4 outColor;

/*
void main()
{
    vec3 dir = normalize(passViewDirection);
    float lerp = (dir.y + 1.0f) * 0.5f;
    
    outColor = mix(bottomColor, topColor, lerp);
}
*/
void main()
{
    vec3 dir = normalize(passViewDirection);
    float t = (dir.y + 1.0) * 0.5;

    vec4 color = mix(bottomColor, middleColor, smoothstep(0.0, 0.5, t));
    color      = mix(color,       topColor,    smoothstep(0.5, 1.0, t));

    outColor = color;
}


