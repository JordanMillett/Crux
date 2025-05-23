#version 430

// MOVE REQUIRED UNIFORMS INTO INSERTION AUTO FOR SHADER
uniform sampler2D ColorTexture;
uniform vec4 TextureHue = vec4(1.0, 1.0, 1.0, 1.0);

flat in vec4 vHue; // Instance-specific offset (flat-qualified for better performance)

out vec4 FragColor;

void main()
{
    FragColor = vHue;
}
