#version 430

layout(location = 0) in vec3 vertexPosition; 
layout(location = 1) in vec3 vertexNormal;   
layout(location = 2) in vec2 vertexUV;    

layout(std140, binding = 0) uniform Camera 
{
    mat4 view;
    mat4 projection;
};

uniform int lightIndices[4] = {-1, -1, -1, -1}; 
uniform mat4 model = mat4(1.0);

out vec3 fragPosition;
out vec3 fragNormal;
out vec2 fragUV;
flat out int fragLightIndices[4];

void main()
{
    fragPosition = vec3(model * vec4(vertexPosition, 1.0));
    
    mat3 normalMatrix = mat3(transpose(inverse(model)));
    fragNormal = normalize(normalMatrix * vertexNormal);
    
    fragUV = vertexUV;
    
    for (int i = 0; i < 4; i++)
    {
        fragLightIndices[i] = lightIndices[i];
    }

    gl_Position = projection * view * model * vec4(vertexPosition, 1.0);
}
