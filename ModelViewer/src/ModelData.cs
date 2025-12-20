using ImGuiNET;
using Phoenix.Rendering;
using Phoenix.Rendering.Geometry;
using Silk.NET.Assimp;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Mesh = Phoenix.Rendering.Geometry.Mesh;

namespace ModelViewer.src
{
    public class ModelData : IDisposable
    {
        public Model Model { get; private set; }
        Game game;
        public string Name { get; private set; }
        public PartData[] PartsData;
        int selectedPartIndex = 0;
        public Vector3 Position { get; set; } = Vector3.Zero;
        public Quaternion Orientation { get; set; } = Quaternion.Identity;
        public Vector3 Scale { get; set; } = new Vector3(0.01f);



        public Matrix4x4 World => Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateFromQuaternion(Orientation) * Matrix4x4.CreateTranslation(Position);

        private bool _useParentTransform = true;
        public bool UseParentTransform { get => _useParentTransform; set => _useParentTransform = value; }

        bool _uniformScale = true;

        Vector3 locateColor = Vector3.UnitY;

        public ModelData(Game game, string path, int flags) 
        {
            this.game = game;
            Model = new Model(game.GL, path,
                postProcessSteps: (PostProcessSteps)flags,
                meshAttributes: MeshAttributes.Position3D | MeshAttributes.TexCoord |
                MeshAttributes.Normals | MeshAttributes.Tangents | MeshAttributes.Bitangents,
                
                extractTextures: true);
            
            var parts = new List<PartData>();
            foreach(var part in Model.Parts)
            {
                parts.Add(new PartData(game, part));
            }
            PartsData = parts.ToArray();

            Name = path.Split("\\").Last();
        }
        
        public void Draw(float t)
        {
            locateColor.X = MathF.Sin(t) * 0.5f + 1;
            locateColor.Y = MathF.Tan(t) * 0.5f + 1;
            locateColor.Z = MathF.Cos(t) * 0.5f + 1;
                
            var s = game.ModelShader;
            s.SetAsCurrent();

            s.KA.Set(.25f);
            s.KD.Set(.8f);
            s.KS.Set(.6f);
            s.uShininess.Set(10);
            s.uLightEnabled.Set(1);
            var anyLocating = PartsData.Any(p => p.MeshData.Any(m => m.Locating));
            
            for (int p = 0; p < Model.Parts.Count; p++)
            {
                var part = Model.Parts[p];
                var partData = PartsData[p];
                string name = part.Name;
                for (int m = 0; m < part.Meshes.Count; m++)
                {
                    var mesh = part.Meshes[m];
                    var meshData = partData.MeshData[m];

                    s.uColor.Set(meshData.Color);
                    if (anyLocating && !meshData.Locating)
                        s.uColor.Set(meshData.Color * 0.1f);
                    if (meshData.Locating)
                        s.uColor.Set(locateColor);


                    Matrix4x4 w;
                    Matrix4x4 itw;

                    if (meshData.UseParentTransform)
                        w = mesh.Transform * World;
                    else
                        w = meshData.World;
                    //w = mesh.Transform * World;

                    s.uWorld.Set(w);
                    
                    s.uUseTexture.Set(meshData.UseTexture);

                    if(meshData.UseTexture == 1)
                    {
                        if(meshData.Tex != null)
                            s.uTex.Set(meshData.Tex);
                    }
                    mesh.Draw();

                }
            }
            
        }

        

        
        public void DrawEditor()
        {

            if (ImGui.CollapsingHeader($"Model {Name}"))
            {
                ImGui.Checkbox("Uniform Scale", ref _uniformScale);

                float scale = Scale.X;
                if(_uniformScale)
                {
                    ImGui.DragFloat($"Scale", ref scale, 0.001f);

                    Scale = new Vector3(scale);
                }
                else
                {
                    var f3Scale = Scale;
                    if (ImGui.DragFloat3($"Scale", ref f3Scale, 0.001f))
                    {
                        Scale = f3Scale;
                    }
                }

                Vector3 pos = Position;
                if (ImGui.DragFloat3($"Position", ref pos, 0.001f))
                {
                    Position = pos;
                }
                if(ImGui.Button("Re-center"))
                {
                    Position = Vector3.Zero;
                }
                Orientation.ExtractYawPitchRoll(out var yaw, out var pitch, out var roll);
                var euler = new Vector3(yaw.ToDeg(), pitch.ToDeg(), roll.ToDeg());

                if (ImGui.DragFloat3($"Yaw Pitch Roll", ref euler))
                {
                    yaw = euler.X.ToRad();
                    pitch = euler.Y.ToRad();
                    roll = euler.Z.ToRad();
                    Orientation = Quaternion.CreateFromYawPitchRoll(-yaw,pitch,roll);
                }



                if (ImGui.Checkbox("Apply parent transforms", ref _useParentTransform))
                {
                    if(_useParentTransform)
                    {
                        foreach (var pd in PartsData)
                            foreach (var md in pd.MeshData)
                                md.UseParentTransform = true;
                    }
                }
            }
            for (int p = 0; p < Model.Parts.Count; p++)
            {
                var part = Model.Parts[p];
                var partData = PartsData[p];

                DrawPartUI(p, part, partData);
            }
        }
        void DrawPartUI(int p, ModelPart part, PartData partData)
        {
            if (ImGui.CollapsingHeader($"Part {p}: {part.Name}"))
            {
                
                var meshCount = part.Meshes.Count;
                var meshNames = new string[meshCount];

                for (int m = 0; m < meshCount; m++)
                {
                    meshNames[m] = $"Mesh {m}";
                }

                for (int m = 0; m < meshCount; m++)
                {
                    var mesh = part.Meshes[m];
                    var meshData = partData.MeshData[m];

                    DrawMeshUI(p, m, partData, mesh, meshData);
                }

            }
        }


        void DrawMeshUI(int p, int m, PartData partData, Mesh mesh, MeshData meshData)
        {

            if (ImGui.TreeNode($"Mesh {m}: {mesh.Name} F {mesh.NumFaces} V {mesh.VerticesLength}"))
            {

                ImGui.Checkbox($"Locate ##{p}_{m}", ref meshData.Locating);

                ImGui.Checkbox($"Use Parent Transform##{p}_{m}", ref meshData.UseParentTransform);

                if (!meshData.UseParentTransform)
                {
                    // Position
                    Vector3 pos = meshData.Position;
                    if (ImGui.DragFloat3($"Pos##{p}_{m}", ref pos, 0.1f))
                    {
                        meshData.Position = pos;
                    }

                    //// Rotation (quaternion as euler or as individual components)
                    //// For simplicity: show euler angles (you'd convert to/from quaternion)
                    //var euler = meshData.Orientation.ToEulerAngles();  // pseudo-code
                    meshData.Orientation.ExtractYawPitchRoll(out var yaw, out var pitch, out var roll);
                    var euler = new Vector3(yaw.ToDeg(), pitch.ToDeg(), roll.ToDeg());
                    
                    if (ImGui.DragFloat3($"Rot (Euler)##{p}_{m}", ref euler))
                    {
                        yaw = euler.X.ToRad();
                        pitch = euler.Y.ToRad();
                        roll = euler.Z.ToRad();
                        meshData.Orientation = Quaternion.CreateFromYawPitchRoll(-yaw,pitch, roll);
                    }

                    // Scale
                    Vector3 scale = meshData.Scale;
                    if (ImGui.DragFloat3($"Scale##{p}_{m}", ref scale, 0.01f, 0.001f, 100f))
                    {
                        meshData.Scale = scale;
                    }
                }

                // Use texture toggle
                int useTex = meshData.UseTexture;
                bool tex = useTex != 0;
                if (ImGui.Checkbox($"Use Texture##{p}_{m}", ref tex))
                {
                    meshData.UseTexture = tex ? 1 : 0;
                }
                if (meshData.UseTexture == 1)
                {
                    if (meshData.Tex == null)
                        ImGui.Text("Texture not set");
                    else
                        ImGui.Text($"Texture: {meshData.Tex.Name}");

                    ImGui.SameLine();
                    if (ImGui.Button("Select"))
                        ImGui.OpenPopup("TextureSelect");

                    TextureSelect(meshData);
                }
                else
                {
                    // Color
                    Vector3 color = meshData.Color;
                    if (ImGui.ColorEdit3($"Color##{p}_{m}", ref color))
                    {
                        meshData.Color = color;
                    }

                }


                // If using texture, maybe show a selector / path input, etc.


                ImGui.TreePop();
            }
        }
        void TextureSelect(MeshData meshData)
        {
            if(ImGui.BeginPopup("TextureSelect"))
            {
                var maxSize = 150f;
                if(game.Textures.Count > 0)
                {
                    foreach(var tex in game.Textures)
                    {
                        Vector2 size = Vector2.One;
                        if(tex.Width >= tex.Height)
                        {
                            var ar = (float)tex.Height / tex.Width;
                            size.X = maxSize;
                            size.Y = maxSize * ar;
                            
                        }
                        else
                        {
                            var ar = (float)tex.Width / tex.Height;
                            size.X = maxSize * ar;
                            size.Y = maxSize;
                        }

                        var name = tex.Name;
                        if (ImGui.ImageButton(name, (nint)tex.GetHandle(), size))
                        {
                            meshData.Tex = tex;
                        }
                    }
                    //var sizeSqrt = MathF.Ceiling(MathF.Sqrt(size));

                    ////Console.WriteLine($"sz {size} sqrt {sizeSqrt}");
                    //for (int x = 0; x < sizeSqrt; x++)
                    //{
                    //    for (int y = 0; y < sizeSqrt; y++)
                    //    {
                    //        var i = (int)(x * sizeSqrt) + y;
                    //        var tex = game.Textures[i];

                    //        //ImGui.g
                    //        if (ImGui.ImageButton(game.ToName(tex.Path), (nint)tex.GetHandle(), new Vector2(100, 100)))
                    //        {
                    //            meshData.Tex = tex;
                    //        }

                    //        //Console.Write($"{v}-{game.Textures[v + y]} ");
                    //    }
                    //    //Console.Write("\n");
                    //}

                }


                //if (game.Textures.Count() > 0)
                //{
                //    var x = game.Textures.Count % 

                //    for (var x = 0; x < ; x++ )
                //    {
                //        for(var x = 0; x < game.Textures.Count; y++)
                //    }

                //    var tex = game.Textures[0];
                    

                //}
                ImGui.EndPopup();
            }
        }
        public void Dispose()
        {
            Model.Dispose();
        }
    }
}
