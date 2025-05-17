#version 430

layout(location = 0) in vec3 vertexPosition; 
layout(location = 1) in vec3 vertexNormal;   
layout(location = 2) in vec2 vertexUV;    

#ifdef INSTANCED

layout(location = 3) in mat4 instanceModel;

#endif

layout(std140, binding = 0) uniform Camera 
{
    mat4 view;
    mat4 projection;
};

#ifndef INSTANCED

uniform int lightIndices[4] = {-1, -1, -1, -1}; 
uniform mat4 model = mat4(1.0);

#endif

out vec3 fragPosition;
out vec3 fragNormal;
out vec2 fragUV;
flat out int fragLightIndices[4];

void main()
{
    #ifdef INSTANCED
    fragPosition = vec3(instanceModel * vec4(vertexPosition, 1.0));
    mat3 normalMatrix = mat3(transpose(inverse(instanceModel)));
    
    #else

    fragPosition = vec3(model * vec4(vertexPosition, 1.0));
    mat3 normalMatrix = mat3(transpose(inverse(model)));

    #endif

    fragNormal = normalize(normalMatrix * vertexNormal);
    fragUV = vertexUV;
    
    for (int i = 0; i < 4; i++)
    {
        fragLightIndices[i] = -1;
    }

    #ifdef INSTANCED
        
    gl_Position = projection * view * instanceModel * vec4(vertexPosition, 1.0);

    #else

    gl_Position = projection * view * model * vec4(vertexPosition, 1.0);

    #endif
}
