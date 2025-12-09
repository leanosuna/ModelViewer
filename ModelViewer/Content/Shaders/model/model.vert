#version 410 core
layout (location = 0) in vec3 vPos;
layout (location = 1) in vec2 vTexCoords;
layout (location = 2) in vec3 vNormal;
//layout (location = 3) in vec2 vTangent;
//layout (location = 4) in vec3 vBitangent;

uniform mat4 uWorld;

layout(std140) uniform CommonData
{
    mat4 sView;
    mat4 sProjection;
    float sTime;
    float sDeltaTime;
};


out VS_OUT
{
    vec2 texCoords;
    vec3 normal;
    vec3 position;
} vs_out;


void main()
{
    vs_out.texCoords = vTexCoords;

    vec4 world = uWorld * vec4(vPos, 1);
    
    vs_out.position = world.xyz;
    
    mat4 itw = transpose(inverse(uWorld));
    vec4 normal = itw * vec4(vNormal, 1);
    vs_out.normal = normalize(normal.xyz);
    
    gl_Position = sProjection * sView  * world;
}