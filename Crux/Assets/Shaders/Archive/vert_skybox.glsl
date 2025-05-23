#version 430

layout (location = 0) in vec2 vertexPosition;

layout(std140, binding = 0) uniform Camera 
{
    mat4 view;
    mat4 projection;
};

out vec3 viewDirection;

void main()
{
    // Convert the vertex position to clip space
    vec4 clipPosition = vec4(vertexPosition, 0.0, 1.0);

    // Convert the clip position to eye (view) space
    vec4 viewPosition = inverse(projection) * clipPosition;

    // Normalize the eye space position to get the direction
    vec3 eyeDirection = normalize(viewPosition.xyz);

    // Pass the view direction to the fragment shader
    viewDirection = (inverse(view) * vec4(eyeDirection, 0.0)).xyz;

    gl_Position = vec4(vertexPosition, 0.0, 1.0);
}