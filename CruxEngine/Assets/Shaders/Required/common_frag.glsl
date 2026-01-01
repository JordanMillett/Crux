//Datatypes
struct PointLight
{
    vec3 Pos;
    vec4 Hue;
    float Intensity;
};

struct SceneLighting
{
    vec3 SunDirection;
    float SunIntensity;
    vec4 SunColor;
    vec4 AmbientColor;
    vec4 FogColor;
    float AlphaFadeStart;
    float AlphaFadeEnd;
    float FogStart;
    float FogEnd;
};

//UBOs
layout(std140, binding = 0) uniform Camera 
{
    mat4 view;
    mat4 projection;
};

layout(std140, binding = 1) uniform SceneBuffer
{
    SceneLighting Scene;
};

//SSBOs
layout(std430, binding = 0) buffer LightBuffer
{
    PointLight lights[4];
};