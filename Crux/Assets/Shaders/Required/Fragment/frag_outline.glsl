#version 430

uniform vec4 TextureHue = vec4(1.0, 1.0, 1.0, 1.0);

out vec4 FragColor;

#ifdef INSTANCED

flat in vec4 vHue; 

#endif

void main()
{
    #ifdef INSTANCED

    FragColor = vHue;

    #else

    FragColor = TextureHue;

    #endif
}
