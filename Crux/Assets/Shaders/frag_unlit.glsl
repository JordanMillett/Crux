#version 430

uniform vec4 Hue = vec4(1.0, 1.0, 1.0, 1.0);  // Color uniform from the C# code
uniform sampler2D Texture; // Texture sampler uniform
uniform vec2 TextureTiling = vec2(1.0, 1.0);

in vec3 fragNormal;  // Passed from the vertex shader
in vec2 fragUV;      // Passed from the vertex shader

out vec4 FragColor;

void main()
{
    vec4 texColor = texture(Texture, fragUV * TextureTiling);
    FragColor = texColor * Hue;
}
