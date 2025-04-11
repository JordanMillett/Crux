#version 430

layout(location = 0) in vec3 vertexPosition;   

// Per-instance data
layout(location = 1) in mat4 instanceModel;
layout(location = 5) in vec4 hue;

layout(std140, binding = 0) uniform Camera 
{
    mat4 view;
    mat4 projection;
};

out vec3 fragPosition;
flat out vec4 vHue;

void main()
{
    fragPosition = vec3(instanceModel * vec4(vertexPosition, 1.0));
    
    vHue = hue;

    gl_Position = projection * view * instanceModel * vec4(vertexPosition, 1.0);
}
