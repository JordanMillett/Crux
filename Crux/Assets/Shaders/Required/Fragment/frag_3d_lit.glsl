#version 430

#include <common_frag.glsl>

//Take From Vertex
in vec3 passPosition;
in vec3 passNormal;
in vec2 passUV;

//Take From Vertex - Per Instance
#ifdef INSTANCED
flat in vec4 instHue;
#endif

//Non-Instanced Uniforms
uniform sampler2D albedoTexture; //TextureUnit.Texture0
uniform vec4 albedoHue = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec2 tiling = vec2(1.0, 1.0);

out vec4 outColor;

void main()
{       
    vec4 albedo = texture(albedoTexture, passUV * tiling);

    if (albedo.a < 0.9)
        discard;
    
    vec4 computedColor = vec4(0.0);
    
    vec3 sunLightDir = normalize(-Sun.Dir);
    float sunIntensity = max(dot(normalize(passNormal), sunLightDir), 0.0);
    computedColor += albedo * Sun.Hue * sunIntensity * 1.0;
    
    computedColor = max(computedColor, albedo * Sun.Ambient * 1.0);
    
    vec3 cameraPosition = inverse(view)[3].xyz;
    float fragDistance = length(passPosition - cameraPosition);
    float fogFactor = clamp((fragDistance - Sun.FogStart) / (Sun.FogEnd - Sun.FogStart), 0.0, 1.0);

    computedColor = mix(computedColor, Sun.Fog, fogFactor);
    float fadeFactor = clamp((fragDistance - Sun.FadeStart) / (Sun.FadeEnd - Sun.FadeStart), 0.0, 1.0);

    #ifdef INSTANCED
        computedColor *= instHue;
    #else
        computedColor *= albedoHue;
    #endif
    
    outColor = vec4(computedColor.rgb, 1.0 - fadeFactor);
}