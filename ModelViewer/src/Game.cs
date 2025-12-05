using ImGuiNET;
using NativeFileDialogNET;
using Phoenix;
using Phoenix.Cameras;
using Phoenix.Rendering;
using Phoenix.Rendering.Geometry;
using Phoenix.Rendering.Textures;
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
        List<ModelData> _loadedModels = new List<ModelData>();

        bool _showImGui = true;
        bool _showImGuiDemo = false;
        bool _firstShown = true;
        string _status = "";
        float _statusTime = 0f;

        public string Status { get => _status;
            set {
                _status = value;
                _statusTime = 0f;
            }

            
        }
        public List<GLTexture> Textures { get;} = new List<GLTexture>();
        public Game()
        {
            Log.ClearLog();
            Log.Enabled = true;
        }
        
        protected override void Initialize()
        {
            SetResolution(Window.Monitor.Bounds.Max.ToNum(), false);
            _freeCamera = new FreeCamera(
                this,
                position: new Vector3(5,5,5),

                yaw: 4,
                pitch: -0.5f,
                fov: MathF.PI * 0.55f,
                nearPlane: 0.1f,
                farPlane: 1000f,
                aspectRatio: (float)WindowSize.X / WindowSize.Y);
            _freeCamera.SetMoveKeys(Key.W, Key.S, Key.A, Key.D, Key.Space, Key.ControlLeft, Key.ShiftLeft, 10f);
            _freeCamera.SetPitchYawKeys(Key.Up, Key.Down, Key.Left, Key.Right, Vector2.One);
            _freeCamera.MouseAim = false;

            Camera = _freeCamera;
            InputManager.SetMouseMode(CursorMode.Normal);

            ModelShader = new ModelShader(this);

        }

        protected override void Update(double deltaTime)
        {
            _freeCamera.Update((float)deltaTime);
            _statusTime += (float)deltaTime;
            if (InputManager.KeyDown(Key.Escape))
                Stop();

            if (InputManager.KeyDownOnce(Key.Tab))
            {
                _showImGui = !_showImGui;

                InputManager.SetMouseMode(_showImGui ? CursorMode.Normal : CursorMode.Raw);
                _freeCamera.MouseAim = !_showImGui;

                if (_showImGui)
                    _firstShown = true;
            }
            if (InputManager.KeyDownOnce(Key.Number0))
            {
                _showImGuiDemo = !_showImGuiDemo;
            }

            ModelShader.uCameraPosition.Set(_freeCamera.Position);
            ModelShader.uLightColor.Set(Vector3.One);
            ModelShader.uLightPosition.Set(Vector3.One * 50);

        }

        protected override void Render(double deltaTime)
        {
            GL.Enable(GLEnum.DepthTest);
            GL.Enable(GLEnum.CullFace);
            GL.ClearColor(.2f, .2f, .2f, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GUIManager.SetFontSize(15);
            
            foreach (var md in _loadedModels)
            {
                md.Draw();
            }

            if (_showImGui)
            {
                ImGui.SetNextWindowSize(new Vector2(WindowWidth / 4, WindowHeight));
                ImGui.SetNextWindowPos(Vector2.Zero);
                if(_firstShown)
                {
                    ImGui.SetNextWindowCollapsed(false);
                    _firstShown = false;
                }
                ImGui.Begin("Model Viewer", ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoMove);
                
                if (ImGui.BeginMenuBar())
                {
                    if (ImGui.BeginMenu("File"))
                    {
                        if (ImGui.MenuItem("Load Model...", "Ctrl+1"))
                            OpenModelFilePicker();
                        if (ImGui.MenuItem("Load Texture...", "Ctrl+2"))
                            OpenTextureFilePicker();
                        if (ImGui.MenuItem("Load CFG...", "Ctrl+3"))
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
                if(_statusTime < 3f)
                    ImGui.Text($"{_status}");
                
                foreach (var md in _loadedModels)
                {
                    md.DrawImGui();
                }
                

                ImGui.End();
            }

            
            if(_showImGuiDemo)
                ImGui.ShowDemoWindow();
            
            Gizmos.AddAxisLines(1000);
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
                if(_loadedModels.Count > 0)
                {
                    _loadedModels.ForEach(l => l.Dispose());
                    _loadedModels.Clear();
                }
                var md = new ModelData(this, path);
                _loadedModels.Add(md);
                Status = $"Loaded model {md.Name}";
            }
            else
            {
                //Console.WriteLine("Dialog canceled or no file selected.");
            }

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
        protected override void OnWindowResize(Vector2D<int> windowSize)
        {
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
