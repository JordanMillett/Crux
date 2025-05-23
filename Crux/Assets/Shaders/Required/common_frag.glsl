//UBOs
layout(std140, binding = 0) uniform Camera 
{
    mat4 view;
    mat4 projection;
};

layout(std140, binding = 1) uniform SunBuffer
{
    SceneLight Sun;
};

//SSBOs
layout(std430, binding = 0) buffer LightBuffer
{
    PointLight lights[4];
};

//Datatypes
struct PointLight
{
    vec3 Pos;
    vec4 Hue;
    float Intensity;
};

struct SceneLight
{
    vec3 Dir;
    vec4 Hue;
    vec4 Ambient;
    vec4 Fog;
    float FogStart;
    float FogEnd;
    float FadeStart;
    float FadeEnd;
};