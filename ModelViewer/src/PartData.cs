using Phoenix.Rendering.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModelViewer.src
{
    public class PartData
    {
        public MeshData[] MeshData;
        public int SelectedMeshIndex;
        public bool UseParentTransform = true;
        
        public PartData(Game game, ModelPart part)
        {
            var meshes = new List<MeshData>();
            foreach(var mesh in part.Meshes)
            {
                meshes.Add(new MeshData(game, mesh));
            }
            MeshData = meshes.ToArray();
        }
    }
}
