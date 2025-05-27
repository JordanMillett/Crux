#version 430

#include <common_vert.glsl>

//Static VBO Input
layout(location = 0) in vec2 inPosition;
layout(location = 1) in vec2 inUV;

//Dynamic VBO Input - Per Instance
layout(location = 3) in mat4 inInstanceModel;
layout(location = 7) in vec4 inInstanceHue;
layout(location = 8) in vec2 inAtlasOffset;

//Pass To Fragment
out vec2 passUV;

//Pass To Fragment - Per Instance
flat out vec4 instHue;
flat out vec2 instAtlasOffset;

void main()
{   
    gl_Position = inInstanceModel * vec4(inPosition, 0.0, 1.0);
    passUV = inUV;
    instHue = inInstanceHue;
    instAtlasOffset = inAtlasOffset;
}