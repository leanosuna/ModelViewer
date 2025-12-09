using Phoenix.Rendering.Shaders;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace ModelViewer.src
{
    public class PostShader
    {
        private GLShader _post;
        public ShaderTextureUniform uTexScene;
        public ShaderTextureUniform uTexMask;


        public PostShader(Game game) 
        {
            _post = new GLShader(game.GL, Game.ContentFolderShaders + "post/post");
        
            uTexScene = new ShaderTextureUniform(_post, "uTexScene", 0);
            uTexMask = new ShaderTextureUniform(_post, "uTexMask", 1);

        }

        public void SetAsCurrent()
        {
            _post.SetAsCurrentGLProgram();
        }
    }
}
