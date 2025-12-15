#version 430

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

layout(std430, binding = 0) buffer LightBuffer
{
    PointLight lights[4];
};

layout(std140, binding = 0) uniform Camera 
{
    mat4 view;
    mat4 projection;
};

layout(std140, binding = 1) uniform SunBuffer
{
    SceneLight Sun;
};

uniform sampler2D ColorTexture;
uniform vec4 TextureHue = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec2 TextureTiling = vec2(1.0, 1.0);

in vec3 fragPosition;
in vec3 fragNormal;
in vec2 fragUV; 
flat in int fragLightIndices[4];

out vec4 FragColor;

void main()
{       
    vec4 albedo = texture(ColorTexture, fragUV * TextureTiling);

    if (albedo.a < 0.9)
        discard;
    
    vec4 computedColor = vec4(0.0);
    
    for (int i = 0; i < 4; i++) 
    {
        int idx = fragLightIndices[i];
        
        if (idx == -1) continue;
        
        vec3 lightDir = normalize(lights[idx].Pos - fragPosition);
        
        float intensity = max(dot(normalize(fragNormal), lightDir), 0.0);
        
        float distance = length(fragPosition - lights[idx].Pos);
        //float falloff = 1.0 / (distance * distance);
        float falloff = 1.0 / distance;
  
  
        computedColor += albedo * lights[idx].Hue * intensity * falloff * lights[idx].Intensity;
    }
    
    vec3 sunLightDir = normalize(-Sun.Dir);
    float sunIntensity = max(dot(normalize(fragNormal), sunLightDir), 0.0);
    computedColor += albedo * Sun.Hue * sunIntensity * 1.0;
    
    computedColor = max(computedColor, albedo * Sun.Ambient * 1.0);
    
    vec3 cameraPosition = inverse(view)[3].xyz;
    float fragDistance = length(fragPosition - cameraPosition);
    float fogFactor = clamp((fragDistance - Sun.FogStart) / (Sun.FogEnd - Sun.FogStart), 0.0, 1.0);

    computedColor = mix(computedColor, Sun.Fog, fogFactor);
    
    float fadeFactor = clamp((fragDistance - Sun.FadeStart) / (Sun.FadeEnd - Sun.FadeStart), 0.0, 1.0);
    FragColor = vec4(computedColor.rgb * TextureHue.rgb, 1.0 - fadeFactor);
    //FragColor = computedColor * Hue;
}