#version 430

#include <common_vert.glsl>

//Static VBO Input
layout(location = 0) in vec3 inPosition; 
layout(location = 1) in vec3 inNormal;   
layout(location = 2) in vec2 inUV;

//Dynamic VBO Input - Per Instance
#ifdef INSTANCED
layout(location = 3) in mat4 inInstanceModel;
layout(location = 7) in vec4 inInstanceHue;
#endif

//Non-Instanced Uniforms
uniform mat4 model = mat4(1.0);

//Pass To Fragment
out vec3 passPosition;
out vec3 passNormal;
out vec2 passUV;

//Pass To Fragment - Per Instance
#ifdef INSTANCED
flat out vec4 instHue;
#endif

void main()
{
    mat3 normalMatrix;

    #ifdef INSTANCED
        passPosition = vec3(inInstanceModel * vec4(inPosition, 1.0));
        normalMatrix = mat3(transpose(inverse(inInstanceModel)));
        instHue = inInstanceHue;
        gl_Position = projection * view * inInstanceModel * vec4(inPosition, 1.0);
    #else
        passPosition = vec3(model * vec4(inPosition, 1.0));
        normalMatrix = mat3(transpose(inverse(model)));
        gl_Position = projection * view * model * vec4(inPosition, 1.0);
    #endif

    passNormal = normalize(normalMatrix * inNormal);
    passUV = inUV;
}
