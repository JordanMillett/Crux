#version 430

layout(location = 0) in vec2 vertexPosition; 
layout(location = 1) in vec2 vertexUV;    

// Per-instance data
layout(location = 2) in mat4 model; // Instance-specific model matrix
layout(location = 6) in vec4 hue;   // Instance-specific color

layout(std140, binding = 0) uniform Camera 
{
    mat4 view;
    mat4 projection;
};

flat out vec4 vHue;

void main()
{
    vHue = hue;
    gl_Position = model * vec4(vertexPosition, 0.0, 1.0);
}
