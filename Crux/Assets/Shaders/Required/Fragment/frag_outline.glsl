#version 430

uniform vec4 TextureHue = vec4(1.0, 1.0, 1.0, 1.0);

out vec4 FragColor;

void main()
{
    FragColor = TextureHue;
}
