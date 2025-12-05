using Phoenix.Rendering.Shaders;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace ModelViewer.src
{
    public class ModelShader
    {
        private GLShader _shaderModel;

        public ShaderUniform<Matrix4x4> uWorld { get; set; }
        public ShaderUniform<Vector3> uColor { get; set; }
        public ShaderUniform<Vector3> uCameraPosition{ get; set; }
        public ShaderUniform<Vector3> uLightPosition{ get; set; }
        public ShaderUniform<Vector3> uLightColor{ get; set; }
        public ShaderUniform<float> KA{ get; set; }
        public ShaderUniform<float> KD{ get; set; }
        public ShaderUniform<float> KS{ get; set; }
        public ShaderUniform<float> uShininess{ get; set; }
        public ShaderUniform<int> uUseTexture{ get; set; }
        public ShaderTextureUniform uTex{ get; set; }
        

        public ModelShader(Game game)
        {
            _shaderModel = new GLShader(game.GL, Game.ContentFolderShaders + "model/model");
            _shaderModel.AttachUBO(game.CommonUboHandle, "CommonData");

            uWorld = new ShaderUniform<Matrix4x4>(_shaderModel, "uWorld");
            uColor = new ShaderUniform<Vector3>(_shaderModel, "uColor");
            uCameraPosition = new ShaderUniform<Vector3>(_shaderModel, "uCameraPosition");
            uLightPosition = new ShaderUniform<Vector3>(_shaderModel, "uLightPosition");
            uLightColor = new ShaderUniform<Vector3>(_shaderModel, "uLightColor");
            KA = new ShaderUniform<float>(_shaderModel, "KA");
            KD = new ShaderUniform<float>(_shaderModel, "KD");
            KS = new ShaderUniform<float>(_shaderModel, "KS");
            uShininess = new ShaderUniform<float>(_shaderModel, "uShininess");
            uUseTexture = new ShaderUniform<int>(_shaderModel, "uUseTexture");
            uTex = new ShaderTextureUniform(_shaderModel, "uTex", 0);
        }
        public void SetAsCurrent()
        {
            _shaderModel.SetAsCurrentGLProgram();
        }
    }
}
