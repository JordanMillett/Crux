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
    
    vec3 sunLightDir = normalize(-Scene.SunDirection);
    float sunHit = max(dot(normalize(passNormal), sunLightDir), 0.0);
    computedColor += albedo * Scene.SunColor * sunHit * Scene.SunIntensity;
    
    computedColor = max(computedColor, albedo * Scene.AmbientColor * 1.0);
    
    vec3 cameraPosition = inverse(view)[3].xyz;
    float fragDistance = length(passPosition - cameraPosition);
    float fogFactor = clamp((fragDistance - Scene.FogStart) / (Scene.FogEnd - Scene.FogStart), 0.0, 1.0);

    computedColor = mix(computedColor, Scene.FogColor, fogFactor);
    float fadeFactor = clamp((fragDistance - Scene.AlphaFadeStart) / (Scene.AlphaFadeEnd - Scene.AlphaFadeStart), 0.0, 1.0);

    #ifdef INSTANCED
        computedColor *= instHue;
    #else
        computedColor *= albedoHue;
    #endif
    
    outColor = vec4(computedColor.rgb, 1.0 - fadeFactor);
}