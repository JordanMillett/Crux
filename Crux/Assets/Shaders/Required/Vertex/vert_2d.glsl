#version 430

#include <common_vert.glsl>

//Static VBO Input
layout(location = 0) in vec2 inPosition;
layout(location = 1) in vec2 inUV;

//Dynamic VBO Input - Per Instance
#ifdef INSTANCED
layout(location = 3) in mat4 inInstanceModel;
layout(location = 7) in vec4 inInstanceHue;
#endif

//Non-Instanced Uniforms
uniform mat4 model = mat4(1.0);

//Pass To Fragment
out vec2 passUV;
out vec3 passViewDirection;

//Pass To Fragment - Per Instance
#ifdef INSTANCED
flat out vec4 instHue;
#endif

void main()
{
    vec4 clipPosition = vec4(inPosition, 0.0, 1.0);
    vec4 viewPosition = inverse(projection) * clipPosition;
    vec3 eyeDirection = normalize(viewPosition.xyz);
    passViewDirection = (inverse(view) * vec4(eyeDirection, 0.0)).xyz;
    
    vec4 localPos = vec4(inPosition, 0.0, 1.0);

    #ifdef INSTANCED
        gl_Position = inInstanceModel * localPos;
        instHue = inInstanceHue;
    #else
        gl_Position = model * localPos;
    #endif

    passUV = inUV;
}