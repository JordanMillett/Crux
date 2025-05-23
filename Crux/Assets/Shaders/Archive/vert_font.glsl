#version 430

layout(location = 0) in vec2 vertexPosition; 
layout(location = 1) in vec2 vertexUV;    

// Per-instance data
layout(location = 2) in mat4 model;      // Instance-specific model matrix
layout(location = 6) in vec2 AtlasOffset; // Instance-specific atlas offset

layout(std140, binding = 0) uniform Camera 
{
    mat4 view;
    mat4 projection;
};

out vec2 fragUV;
flat out vec2 vAtlasOffset;

void main()
{
    fragUV = vertexUV; 
    vAtlasOffset = AtlasOffset;
    gl_Position = model * vec4(vertexPosition, 0.0, 1.0);
}
