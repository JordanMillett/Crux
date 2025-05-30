#version 430

#include <common_frag.glsl>

//Take From Vertex
in vec2 passUV;
in vec3 passViewDirection;

//Take From Vertex - Per Instance
#ifdef INSTANCED
flat in vec4 instHue;
flat in vec2 instUVOffset;
flat in vec2 instUVScale;
#endif

//Non-Instanced Uniforms
uniform sampler2D albedoTexture; //TextureUnit.Texture0
uniform vec4 albedoHue = vec4(1.0);
uniform float useSDF = 0;

out vec4 outColor;

void main()
{
    vec2 uv;
    vec4 computedColor;

    #ifdef INSTANCED
        uv = passUV * instUVScale + instUVOffset;
    #else
        uv = passUV;
    #endif

    vec4 sampledColor = texture(albedoTexture, uv);

    if(useSDF > 0.5)
    {
        float threshold = 0.5;
        float smoothing = 0.05;

        float dist = (sampledColor.r + sampledColor.g + sampledColor.b) / 3.0;
        smoothing = fwidth(dist) * 1.25; //lil smoothing on top
        float alpha = smoothstep(threshold - smoothing, threshold + smoothing, dist);
    
        computedColor = vec4(instHue.r, instHue.g, instHue.b, alpha);

    }else
    {
        #ifdef INSTANCED
            computedColor = sampledColor * instHue;
        #else
            computedColor = sampledColor * albedoHue;
        #endif
    }

    outColor = computedColor;
}