using Phoenix.Rendering.Geometry;
using Phoenix.Rendering.Textures;
using System.Numerics;

namespace ModelViewer.src
{
    public class MeshData
    {
        public Vector3 Position { get; set; } = Vector3.Zero;
        public Quaternion Orientation { get; set; } = Quaternion.Identity;
        public Vector3 Scale { get; set; } = new Vector3(0.01f);

        public Matrix4x4 World => Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateFromQuaternion(Orientation) * Matrix4x4.CreateTranslation(Position);

        public Vector3 Color { get; set; } = Vector3.One;

        public int UseTexture { get; set; } = 0;
        public GLTexture Tex { get; set; }

        public bool UseParentTransform = true;
        public MeshData(Game game, Mesh mesh)
        {

        }
    }
}
