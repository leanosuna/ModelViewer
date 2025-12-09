#version 410 core

out vec4 oColor;

uniform vec3 uColor;
uniform int uUseTexture;
uniform int uLightEnabled;

uniform int uMask;
uniform vec3 uMaskColor;

uniform sampler2D uTex;

in VS_OUT
{
    vec2 texCoords;
    vec3 normal;
    vec3 position;
} fs_in;

layout(std140) uniform CommonData
{
    mat4 sView;
    mat4 sProjection;
    float sTime;
    float sDeltaTime;
};


uniform vec3 uCameraPosition;
uniform vec3 uLightPosition;
uniform vec3 uLightColor;

uniform float KA;
uniform float KD;
uniform float KS;
uniform float uShininess;


vec3 BlinnPhong(vec3 worldPos, vec3 normal)
{
    vec3 lightDirection = normalize(uLightPosition - vec3(0, 0, 0));
    vec3 viewDirection = normalize(uCameraPosition - worldPos);
    vec3 halfVector = normalize(lightDirection + viewDirection);
    
    float NdotL = clamp(dot(normal, lightDirection),0,1);
    vec3 diffuseLight = KA * uLightColor + KD * uLightColor * NdotL;
    float NdotH = dot(normal, halfVector);
    vec3 specularLight = sign(NdotL) * KS * uLightColor * pow(clamp(NdotH,0,1), uShininess);
    return diffuseLight + specularLight;
}

void main()
{
    vec4 color;
    if(uUseTexture == 0)
        color = vec4(uColor,1);
    else
        color = texture(uTex, fs_in.texCoords);  
   
    if(uLightEnabled == 1)
    {        
        color.rgb *= BlinnPhong(fs_in.position, fs_in.normal);
    }   
    
    oColor = color;
}