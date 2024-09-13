#version 430

in vec3 viewDirection;
out vec4 FragColor;

uniform vec4 topColor = vec4(0.3f, 0.3f, 1.0f, 1.0f);
uniform vec4 bottomColor = vec4(1.0f, 1.0f, 1.0f, 1.0f);
uniform vec4 TextureHue = vec4(1.0, 1.0, 1.0, 1.0);

void main()
{
    vec3 dir = normalize(viewDirection);
    
    float lerp = (dir.y + 1.0f) * 0.5f;
    
    FragColor = mix(bottomColor, topColor, lerp);
}

