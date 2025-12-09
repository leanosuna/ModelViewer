#version 410 core

out vec4 oColor;

in vec2 texCoords;
uniform sampler2D uTexScene;
uniform sampler2D uTexMask;

layout(std140) uniform CommonData
{
    mat4 sView;
    mat4 sProjection;
    float sTime;
    float sDeltaTime;
};

void main()
{
    vec4 color = texture(uTexScene, texCoords);
    vec4 mask = texture(uTexMask, texCoords);
    
    float sen = sin(sTime) * 0.5 + 1;
    vec3 highlight = vec3(0,1,1) * sen;
    
    if(mask.r == 1)
        color.rgb *= highlight;

    oColor = color;
}