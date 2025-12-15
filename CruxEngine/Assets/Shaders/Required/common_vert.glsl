//UBOs
layout(std140, binding = 0) uniform Camera 
{
    mat4 view;
    mat4 projection;
};