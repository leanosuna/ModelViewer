using ImGuiNET;
using NativeFileDialogNET;
using Phoenix;
using Phoenix.Cameras;
using Phoenix.Rendering;
using Phoenix.Rendering.Geometry;
using Phoenix.Rendering.RT;
using Phoenix.Rendering.Textures;
using Silk.NET.Assimp;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

using System.Numerics;
using System.Text;

namespace ModelViewer.src
{
    public class Game : PhoenixGame
    {
        public const string ContentFolderShaders = "Content/Shaders/";

        private FreeCamera _freeCamera;
        public ModelShader ModelShader { get; private set; }
        public PostShader QuadShader { get; private set; }

        List<ModelData> _loadedModels = new List<ModelData>();

        bool _showEditor = true;
        bool _showImGuiDemo = false;
        bool _firstShown = true;
        string _status = "";
        float _statusTime;
        bool _showStatus = false;
        string _modelPath;
        int _assimpFlags = (int)
           (PostProcessSteps.Triangulate
           | PostProcessSteps.GenerateSmoothNormals
           | PostProcessSteps.GenerateUVCoords
           | PostProcessSteps.JoinIdenticalVertices
           | PostProcessSteps.SortByPrimitiveType
           | PostProcessSteps.FlipUVs
           | PostProcessSteps.ImproveCacheLocality
           //| PostProcessSteps.OptimizeGraph
           //| PostProcessSteps.OptimizeMeshes
           );

        public string Status { get => _status;
            set {
                _status = value;
                _statusTime = 0f;
                _showStatus = true;
            }


        }
        public List<GLTexture> Textures { get; } = new List<GLTexture>();
        public Game()
        {
            Log.ClearLog();
            Log.Enabled = true;
        }

        protected override void Initialize()
        {
            SetResolution(Window.Monitor.Bounds.Max.ToNum(), false);
            //scene = RTManager.CreateRenderTarget("scene", [new RenderTexture(), new RenderTexture()], new DepthBuffer());
            //mask = RTManager.CreateRenderTarget("mask", [new RenderTexture()], new DepthBuffer());

            //Window.FramesPerSecond = 0;
            //Window.UpdatesPerSecond = 0;
            //Window.VSync = false;

            _freeCamera = new FreeCamera(
                this,
                position: new Vector3(5, 5, 5),

                yaw: 4,
                pitch: -0.5f,
                fov: MathF.PI * 0.55f,
                nearPlane: 0.1f,
                farPlane: 1000f,
                aspectRatio: (float)WindowSize.X / WindowSize.Y);
            _freeCamera.SetMoveKeys(Key.W, Key.S, Key.A, Key.D, Key.Space, Key.AltLeft, Key.ShiftLeft, 10f);
            _freeCamera.SetPitchYawKeys(Key.Up, Key.Down, Key.Left, Key.Right, Vector2.One);
            _freeCamera.MouseAim = false;

            Camera = _freeCamera;
            InputManager.SetMouseMode(CursorMode.Normal);

            ModelShader = new ModelShader(this);
            QuadShader = new PostShader(this);


            //var path = "Content/3D/Racer/Racer.fbx";
            //_modelPath = path;
            //var md = new ModelData(this, path, _assimpFlags);
            //_loadedModels.Add(md);
            //Status = $"Loaded model {md.Name}";

            //Textures.AddRange(md.Model.Textures.Values);
        }

        protected override void Update(double deltaTime)
        {
            _freeCamera.Update((float)deltaTime);
            if (InputManager.KeyDown(Key.Escape))
                Stop();

            if (InputManager.KeyDownOnce(Key.Tab))
            {
                _showEditor = !_showEditor;

                InputManager.SetMouseMode(_showEditor ? CursorMode.Normal : CursorMode.Raw);
                _freeCamera.MouseAim = !_showEditor;

                if (_showEditor)
                    _firstShown = true;
            }
            if (InputManager.KeyDownOnce(Key.P))
            {
                _showImGuiDemo = !_showImGuiDemo;
            }

            if (InputManager.KeyDown(Key.ControlLeft) &&
                InputManager.KeyDownOnce(Key.R))
                OpenModelFilePicker();
            if (InputManager.KeyDown(Key.ControlLeft) &&
                InputManager.KeyDownOnce(Key.T))
                OpenTextureFilePicker();
            if (InputManager.KeyDown(Key.ControlLeft) &&
                InputManager.KeyDownOnce(Key.S))
                CFGSave();
            if (InputManager.KeyDown(Key.ControlLeft) &&
                InputManager.KeyDown(Key.ShiftLeft) &&
                InputManager.KeyDownOnce(Key.S))
                OpenCFGSaveAs();


            ModelShader.uCameraPosition.Set(_freeCamera.Position);
            ModelShader.uLightColor.Set(Vector3.One);
            ModelShader.uLightPosition.Set(Vector3.One * 3000);

        }

        protected override void Render(double deltaTime)
        {
            GUIManager.SetFontSize(15);
            GL.Enable(GLEnum.DepthTest);
            GL.Enable(GLEnum.CullFace);
            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            foreach (var md in _loadedModels)
            {
                md.Draw((float)Time);
            }

            DrawEditor();
            DrawStatus((float)deltaTime);

            if (_showImGuiDemo)
                ImGui.ShowDemoWindow();

            GUIManager.DrawRAlignedText($"{(int)FPS_SAMPLE} fps", new Vector2(WindowWidth, 0), Vector4.One, 15);
            Gizmos.AddAxisLines(1000);
        }

       
        void DrawEditor()
        {
            if (!_showEditor)
                return;
            
            ImGui.SetNextWindowSize(new Vector2(WindowWidth / 4, WindowHeight));
            ImGui.SetNextWindowPos(Vector2.Zero);
            if (_firstShown)
            {
                ImGui.SetNextWindowCollapsed(false);
                _firstShown = false;
            }
            ImGui.Begin("Model Viewer", ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoMove);

            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Load Model...", "Ctrl+R"))
                        OpenModelFilePicker();
                    if (ImGui.MenuItem("Load Texture...", "Ctrl+T"))
                        OpenTextureFilePicker();
                    if (ImGui.MenuItem("Load CFG...", "Ctrl+C"))
                        OpenCFGFilePicker();
                    if (ImGui.MenuItem("Save CFG", "Ctrl+S"))
                        Console.WriteLine("saved cfg");
                    //SaveCFG();
                    if (ImGui.MenuItem("Save CFG as...", "Ctrl+Shift+S"))
                        OpenCFGSaveAs();
                    ImGui.EndMenu();
                }
                ImGui.EndMenuBar();

            }
            if (ImGui.Button("Reset Camera") || InputManager.KeyDownOnce(Key.R))
            {
                _freeCamera.Position = new Vector3(5, 5, 5);
                _freeCamera.Yaw = 4;
                _freeCamera.Pitch = -0.5f;
                
            }
            if (ImGui.CollapsingHeader("Assimp flags"))
            {
                ImGui.CheckboxFlags("Triangulate", ref _assimpFlags, (int)PostProcessSteps.Triangulate);
                
                if (ImGui.CheckboxFlags("Generate Smooth Normals", ref _assimpFlags, (int)PostProcessSteps.GenerateSmoothNormals))
                {
                    var en = (PostProcessSteps)_assimpFlags;
                    if(en.HasFlag(PostProcessSteps.GenerateNormals))
                    {
                        en &= ~PostProcessSteps.GenerateNormals;
                        _assimpFlags = (int)en;
                    }   
                }
                if(ImGui.CheckboxFlags("Generate Normals", ref _assimpFlags, (int)PostProcessSteps.GenerateNormals))
                {
                    var en = (PostProcessSteps)_assimpFlags;
                    if (en.HasFlag(PostProcessSteps.GenerateSmoothNormals))
                    {
                        en &= ~PostProcessSteps.GenerateSmoothNormals;
                        _assimpFlags = (int)en;
                    }
                }
                ImGui.CheckboxFlags("Generate UV", ref _assimpFlags, (int)PostProcessSteps.GenerateUVCoords);
                ImGui.CheckboxFlags("Join Vertices", ref _assimpFlags, (int)PostProcessSteps.JoinIdenticalVertices);
                ImGui.CheckboxFlags("Sort by Type", ref _assimpFlags, (int)PostProcessSteps.SortByPrimitiveType);
                ImGui.CheckboxFlags("Flip UV", ref _assimpFlags, (int)PostProcessSteps.FlipUVs);
                ImGui.CheckboxFlags("Improve cache locality", ref _assimpFlags, (int)PostProcessSteps.ImproveCacheLocality);
                ImGui.CheckboxFlags("Optimize Graph", ref _assimpFlags, (int)PostProcessSteps.OptimizeGraph);
                ImGui.CheckboxFlags("Optimize Meshes", ref _assimpFlags, (int)PostProcessSteps.OptimizeMeshes);
                ImGui.CheckboxFlags("Limit bone weights", ref _assimpFlags, (int)PostProcessSteps.LimitBoneWeights);
                ImGui.CheckboxFlags("Find Degenerates", ref _assimpFlags, (int)PostProcessSteps.FindDegenerates);
                ImGui.CheckboxFlags("Fix In Facing normals", ref _assimpFlags, (int)PostProcessSteps.FixInFacingNormals);
                ImGui.CheckboxFlags("PreTransform Vertices", ref _assimpFlags, (int)PostProcessSteps.PreTransformVertices);
                ImGui.CheckboxFlags("Flip winding", ref _assimpFlags, (int)PostProcessSteps.FlipWindingOrder);
                ImGui.CheckboxFlags("Split Large Meshes", ref _assimpFlags, (int)PostProcessSteps.SplitLargeMeshes);

                if (ImGui.Button("Reload Model"))
                {
                    if (_loadedModels.Count > 0)
                    {
                        LoadModel(_modelPath);
                    }
                    else
                    {
                        Status = "No model to reload";
                    }
                }
            }
            


            foreach (var md in _loadedModels)
            {
                md.DrawEditor();
            }


            ImGui.End();
            
        }

        void DrawStatus(float dt)
        {
            if (!_showStatus)
                return;

            _statusTime += dt;
            if (_statusTime > 5f)
            {
                _showStatus = false;
                _statusTime = 0;
            }
            //var width = 300;
            ImGui.SetNextWindowPos(new Vector2(WindowWidth/2, 0));
            ImGui.Begin("status", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove);
            GUIManager.SetFontSize(30);
            ImGui.Text($"{_status}");
            ImGui.End();

            //GUIManager.DrawCenteredText($"{_status}", new Vector2(WindowWidth / 2, WindowHeight / 2), new Vector4(0, .6f, 1, 1), 40);
        }
        void OpenModelFilePicker()
        {
            using var dlg = new NativeFileDialog()
            .SelectFile()
            .AddFilter("Model files", "*.*");

            DialogResult result = dlg.Open(out string[]? files, defaultPath: Environment.CurrentDirectory);
            //defaultPath: Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            if (result == DialogResult.Okay && files != null && files.Length > 0)
            {
                // files is a string array — might contain multiple paths if multiselect allowed
                var path = files[0];
                LoadModel(path);
            }
            else
            {
                //Console.WriteLine("Dialog canceled or no file selected.");
            }

        }
        void LoadModel(string path)
        {
            if (_loadedModels.Count > 0)
            {
                _loadedModels.ForEach(l => l.Dispose());
                _loadedModels.Clear();
                Textures.Clear();
            }
            _modelPath = path;
            var md = new ModelData(this, path, _assimpFlags);
            _loadedModels.Add(md);
            Status = $"Loaded model {md.Name}";
            Textures.AddRange(md.Model.Textures.Values);
        }
        void OpenTextureFilePicker()
        {
            using var dlg = new NativeFileDialog()
            .SelectFile()
            .AddFilter("Texture files", "*.*")
            .AllowMultiple();
            DialogResult result = dlg.Open(out string[]? files, defaultPath: Environment.CurrentDirectory);

            if (result == DialogResult.Okay && files != null && files.Length > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Loaded textures");
                foreach (var path in files)
                {
                    
                    var name = path.Split("\\").Last();
                    Textures.Add(new GLTexture(GL, path));
                    sb.AppendLine(name);
                }
                Status = sb.ToString();

                //var path = files[0];
            }
            else
            {
                //Console.WriteLine("Dialog canceled or no file selected.");
            }

        }
        void OpenCFGFilePicker()
        {
            using var dlg = new NativeFileDialog()
            .SelectFile()
            .AddFilter("Model CFG files", "*.*");

            DialogResult result = dlg.Open(out string[]? files, defaultPath: Environment.CurrentDirectory);

            if (result == DialogResult.Okay && files != null && files.Length > 0)
            {
                var path = files[0];
                // add cfg
            }
            else
            {
                //Console.WriteLine("Dialog canceled or no file selected.");
            }

        }
        void OpenCFGSaveAs()
        {

            using var dlg = new NativeFileDialog()
            .SaveFile()
            .AddFilter("All files", "*.*");

            DialogResult result = dlg.Open(out string? outputPath, defaultPath: Environment.CurrentDirectory);

            if (result == DialogResult.Okay && !string.IsNullOrEmpty(outputPath))
            {
                //Console.WriteLine("User wants to save to: " + outputPath);

                // Example: write some content to that file
                //File.WriteAllText(outputPath, "Hello, world!\nSaved via NativeFileDialogNET.");
                //Console.WriteLine("File written.");
            }
            else
            {
                //Console.WriteLine("Save dialog cancelled or no filename selected.");
            }
        }
        public void CFGSave()
        {

        }
        protected override void OnWindowResize(Vector2D<int> windowSize)
        {
            //if(ModelShader != null)
                
        }
        protected override void OnClose()
        {

        }

        public string ToName(string path)
        {
            return path.Split("\\").Last();
        }
    }
}
