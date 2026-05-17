using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows.Forms;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using ImGuiNET;

namespace WinFloat;

class Program
{
    static void Main(string[] args)
    {
        var gameSettings = GameWindowSettings.Default;
        var nativeSettings = new NativeWindowSettings
        {
            Size = new Vector2i(1280, 720),
            Title = "WinFloat - 3D Window Capture",
            Vsync = VSyncMode.On,
            NumberOfSamples = 4
        };

        using var game = new MainGame(gameSettings, nativeSettings);
        game.Run();
    }
}

public class MainGame : GameWindow  // Made PUBLIC
{
    // Core components
    private CubeRenderer _renderer;
    private WindowCapture _capture;
    private Physics _physics;
    
    // ImGui
    private ImGuiController _imguiController;
    
    // UI state
    private List<WindowCapture.WindowInfo> _windows = new();
    private IntPtr _selectedWindowHandle = IntPtr.Zero;
    private string _selectedWindowTitle = "None";
    private bool _liveCapture = true;
    private int _currentShape = 0;
    private string[] _shapeNames = { "Cube", "Sphere", "Torus", "Cylinder", "Plane" };
    private int _currentAnimation = 0;
    private string[] _animationNames = { "None", "Spin", "Bounce", "Pulse", "Wobble" };
    private float _animationSpeed = 1.0f;
    private float _animationTime = 0f;
    private string _statusMessage = "Ready";
    private float _statusMessageTime = 0f;
    private bool _chaosMode = false;
    private float _chaosTimer = 0f;
    private Random _random = new Random();
    
    // Performance
    private float _deltaTime;

    public MainGame(GameWindowSettings gameSettings, NativeWindowSettings nativeSettings)
        : base(gameSettings, nativeSettings)
    {
        CenterWindow();
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        
        _renderer = new CubeRenderer();
        _renderer.Load();
        
        _capture = new WindowCapture();
        _physics = new Physics();
        
        _imguiController = new ImGuiController(ClientSize.X, ClientSize.Y);
        
        RefreshWindowList();
        
        SetStatus("WinFloat Ready");
    }
    
    private void RefreshWindowList()
    {
        _windows = _capture.GetVisibleWindows();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        _deltaTime = (float)args.Time;
        
        // Update animation time
        if (_currentAnimation != 0)
        {
            _animationTime += _deltaTime * _animationSpeed;
        }
        
        // Update chaos mode
        if (_chaosMode)
        {
            _chaosTimer -= _deltaTime;
            if (_chaosTimer <= 0)
            {
                // Random force
                _physics.ApplyForce(
                    (float)(_random.NextDouble() - 0.5) * 15f,
                    (float)(_random.NextDouble() - 0.5) * 10f,
                    (float)(_random.NextDouble() - 0.5) * 15f
                );
                _chaosTimer = 0.1f;
            }
        }
        
        // Update physics
        var currentPos = _renderer.GetCubePosition();
        _physics.Update(_deltaTime, currentPos);
        _renderer.SetCubePosition(_physics.GetPosition());
        
        // Update live capture texture
        if (_liveCapture && _selectedWindowHandle != IntPtr.Zero)
        {
            var texture = _capture.CaptureWindow(_selectedWindowHandle);
            if (texture != -1)
                _renderer.SetTexture(texture);
        }
        
        // Update status message timeout
        if (_statusMessageTime > 0)
        {
            _statusMessageTime -= _deltaTime;
            if (_statusMessageTime <= 0)
                _statusMessage = "Ready";
        }
        
        // Render
        _renderer.SetShape(_currentShape);
        _renderer.SetAnimation(_currentAnimation, _animationTime);
        _renderer.Render();
        
        // Render ImGui UI
        _imguiController.Update(this, _deltaTime);
        
        RenderUI();
        
        _imguiController.Render();
        
        SwapBuffers();
    }
    
    private void RenderUI()
    {
        // Main window
        ImGui.Begin("WinFloat Control Panel", ImGuiWindowFlags.AlwaysAutoResize);
        
        // Window selection section
        ImGui.Text("Window Capture");
        ImGui.Separator();
        
        if (ImGui.Button("Refresh Window List"))
        {
            RefreshWindowList();
            SetStatus("Window list refreshed");
        }
        
        ImGui.SameLine();
        if (ImGui.Button("Capture Desktop"))
        {
            _selectedWindowHandle = _capture.GetDesktopWindow();
            _selectedWindowTitle = "Desktop";
            var texture = _capture.CaptureWindow(_selectedWindowHandle);
            if (texture != -1)
                _renderer.SetTexture(texture);
            SetStatus("Captured Desktop");
        }
        
        // Window list
        ImGui.Text("Open Windows:");
        if (ImGui.BeginListBox("##windowlist", new Vector2(300, 150)))
        {
            foreach (var window in _windows)
            {
                bool isSelected = (_selectedWindowHandle == window.Handle);
                if (ImGui.Selectable(window.Title, isSelected))
                {
                    _selectedWindowHandle = window.Handle;
                    _selectedWindowTitle = window.Title;
                    var texture = _capture.CaptureWindow(window.Handle);
                    if (texture != -1)
                        _renderer.SetTexture(texture);
                    SetStatus($"Captured: {window.Title}");
                }
            }
            ImGui.EndListBox();
        }
        
        // Live capture toggle
        ImGui.Checkbox("Live Capture", ref _liveCapture);
        if (!_liveCapture)
        {
            ImGui.SameLine();
            if (ImGui.Button("Refresh Texture"))
            {
                if (_selectedWindowHandle != IntPtr.Zero)
                {
                    var texture = _capture.CaptureWindow(_selectedWindowHandle);
                    if (texture != -1)
                        _renderer.SetTexture(texture);
                    SetStatus("Texture refreshed");
                }
            }
        }
        
        ImGui.Separator();
        
        // Shape selection
        ImGui.Text("Shape");
        if (ImGui.BeginCombo("##shape", _shapeNames[_currentShape]))
        {
            for (int i = 0; i < _shapeNames.Length; i++)
            {
                if (ImGui.Selectable(_shapeNames[i], _currentShape == i))
                {
                    _currentShape = i;
                    _renderer.SetShape(i);
                    SetStatus($"Shape: {_shapeNames[i]}");
                }
            }
            ImGui.EndCombo();
        }
        
        // Animation selection
        ImGui.Text("Animation");
        if (ImGui.BeginCombo("##animation", _animationNames[_currentAnimation]))
        {
            for (int i = 0; i < _animationNames.Length; i++)
            {
                if (ImGui.Selectable(_animationNames[i], _currentAnimation == i))
                {
                    _currentAnimation = i;
                    _animationTime = 0;
                    SetStatus($"Animation: {_animationNames[i]}");
                }
            }
            ImGui.EndCombo();
        }
        
        // Animation speed slider
        if (_currentAnimation != 0)
        {
            ImGui.Text("Speed");
            if (ImGui.SliderFloat("##speed", ref _animationSpeed, 0.25f, 3.0f))
            {
                SetStatus($"Speed: {_animationSpeed:F2}x");
            }
        }
        
        ImGui.Separator();
        
        // Custom model
        if (ImGui.Button("Load Custom Model"))
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Load 3D Model",
                Filter = "3D Models|*.obj;*.gltf;*.stl;*.fbx|OBJ files|*.obj|glTF files|*.gltf|STL files|*.stl|FBX files|*.fbx"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _renderer.LoadCustomModel(dialog.FileName);
                SetStatus($"Loaded: {System.IO.Path.GetFileName(dialog.FileName)}");
            }
        }
        
        ImGui.Separator();
        
        // Chaos mode
        if (ImGui.Button(_chaosMode ? "Stop Chaos" : "Start Chaos"))
        {
            _chaosMode = !_chaosMode;
            SetStatus(_chaosMode ? "CHAOS MODE ACTIVE" : "Chaos mode stopped");
            if (!_chaosMode)
            {
                _physics.ResetPosition();
                _renderer.SetCubePosition(Vector3.Zero);
            }
        }
        
        // Reset position
        ImGui.SameLine();
        if (ImGui.Button("Reset Position"))
        {
            _physics.ResetPosition();
            _renderer.SetCubePosition(Vector3.Zero);
            SetStatus("Position reset");
        }
        
        ImGui.Separator();
        
        // Status bar
        ImGui.Text($"Status: {_statusMessage}");
        ImGui.Text($"Position: {_physics.GetPosition():F2}");
        ImGui.Text($"Window: {_selectedWindowTitle}");
        
        ImGui.End();
    }
    
    private void SetStatus(string message)
    {
        _statusMessage = message;
        _statusMessageTime = 2.0f;
    }
    
    public void ThrowCube(float velocityX, float velocityY)
    {
        _physics.Throw(velocityX, velocityY);
        SetStatus("Thrown!");
    }
    
    public void ActivateChaosMode()
    {
        _chaosMode = true;
        _chaosTimer = 0;
        SetStatus("CHAOS MODE!");
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        _renderer.Resize(e.Width, e.Height);
        _imguiController.WindowResized(e.Width, e.Height);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        var keyboard = KeyboardState;
        
        // Keyboard shortcuts
        if (keyboard.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Space))
        {
            ActivateChaosMode();
        }
        if (keyboard.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.R))
        {
            _physics.ResetPosition();
            _renderer.SetCubePosition(Vector3.Zero);
            SetStatus("Position reset");
        }
        if (keyboard.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape))
        {
            Close();
        }
    }

    protected override void OnUnload()
    {
        _imguiController.Dispose();
        _renderer.Unload();
        base.OnUnload();
    }
}

// ImGuiController class (needed for OpenTK integration)
public class ImGuiController : IDisposable
{
    private int _vertexArray;
    private int _vertexBuffer;
    private int _indexBuffer;
    private int _fontTexture;
    private int _shaderProgram;
    private int _windowWidth;
    private int _windowHeight;
    
    public ImGuiController(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
        
        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        
        var io = ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        
        ImGui.StyleColorsDark();
        
        CreateDeviceObjects();
    }
    
    private void CreateDeviceObjects()
    {
        // Vertex shader
        string vertexShaderSource = @"
            #version 330 core
            layout (location = 0) in vec2 aPosition;
            layout (location = 1) in vec2 aTexCoord;
            layout (location = 2) in vec4 aColor;
            
            uniform mat4 uProjection;
            
            out vec2 vTexCoord;
            out vec4 vColor;
            
            void main()
            {
                gl_Position = uProjection * vec4(aPosition, 0.0, 1.0);
                vTexCoord = aTexCoord;
                vColor = aColor;
            }
        ";
        
        // Fragment shader
        string fragmentShaderSource = @"
            #version 330 core
            in vec2 vTexCoord;
            in vec4 vColor;
            uniform sampler2D uTexture;
            out vec4 FragColor;
            
            void main()
            {
                FragColor = vColor * texture(uTexture, vTexCoord);
            }
        ";
        
        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexShaderSource);
        GL.CompileShader(vertexShader);
        
        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);
        GL.CompileShader(fragmentShader);
        
        _shaderProgram = GL.CreateProgram();
        GL.AttachShader(_shaderProgram, vertexShader);
        GL.AttachShader(_shaderProgram, fragmentShader);
        GL.LinkProgram(_shaderProgram);
        
        GL.DetachShader(_shaderProgram, vertexShader);
        GL.DetachShader(_shaderProgram, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
        
        _vertexArray = GL.GenVertexArray();
        _vertexBuffer = GL.GenBuffer();
        _indexBuffer = GL.GenBuffer();
        
        // Create font texture
        var io = ImGui.GetIO();
        byte[] pixels;
        int width, height;
        io.Fonts.GetTexDataAsRGBA32(out pixels, out width, out height);
        
        _fontTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _fontTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.BindTexture(TextureTarget.Texture2D, 0);
        
        io.Fonts.SetTexID((IntPtr)_fontTexture);
    }
    
    public void Update(GameWindow window, float deltaSeconds)
    {
        var io = ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2(_windowWidth, _windowHeight);
        io.DisplayFramebufferScale = new System.Numerics.Vector2(1f, 1f);
        io.DeltaTime = deltaSeconds;
        
        // Update mouse
        var mouse = window.MouseState;
        io.MousePos = new System.Numerics.Vector2(mouse.X, mouse.Y);
        io.MouseDown[0] = mouse.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left);
        io.MouseDown[1] = mouse.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Right);
        io.MouseDown[2] = mouse.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Middle);
        
        // Update keyboard
        var keyboard = window.KeyboardState;
        // Simplified keyboard handling - full implementation would be longer
        
        ImGui.NewFrame();
    }
    
    public void Render()
    {
        ImGui.Render();
        RenderDrawData(ImGui.GetDrawData());
    }
    
    private void RenderDrawData(ImDrawDataPtr drawData)
    {
        if (drawData.CmdListsCount == 0)
            return;
        
        GL.BindVertexArray(_vertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
        
        GL.Enable(EnableCap.Blend);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.ScissorTest);
        
        var projection = Matrix4.CreateOrthographicOffCenter(0, _windowWidth, _windowHeight, 0, -1, 1);
        GL.UseProgram(_shaderProgram);
        var projectionLocation = GL.GetUniformLocation(_shaderProgram, "uProjection");
        GL.UniformMatrix4(projectionLocation, false, ref projection);
        
        for (int i = 0; i < drawData.CmdListsCount; i++)
        {
            var cmdList = drawData.CmdLists[i];
            
            GL.BufferData(BufferTarget.ArrayBuffer, cmdList.VtxBuffer.Size, cmdList.VtxBuffer.Data, BufferUsageHint.StreamDraw);
            GL.BufferData(BufferTarget.ElementArrayBuffer, cmdList.IdxBuffer.Size, cmdList.IdxBuffer.Data, BufferUsageHint.StreamDraw);
            
            // Setup vertex attributes
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);
            
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, ImGuiVertex.Size, 0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, ImGuiVertex.Size, 8);
            GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, ImGuiVertex.Size, 16);
            
            for (int j = 0; j < cmdList.CmdLists.Size; j++)
            {
                var cmd = cmdList.CmdBuffer[j];
                GL.BindTexture(TextureTarget.Texture2D, (int)cmd.TextureId);
                GL.Scissor((int)cmd.ClipRect.X, (int)(_windowHeight - cmd.ClipRect.W), (int)(cmd.ClipRect.Z - cmd.ClipRect.X), (int)(cmd.ClipRect.W - cmd.ClipRect.Y));
                GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)cmd.ElemCount, DrawElementsType.UnsignedShort, (IntPtr)(cmd.IdxOffset * sizeof(ushort)), (int)cmd.VtxOffset);
            }
        }
        
        GL.Disable(EnableCap.Blend);
        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);
        GL.Disable(EnableCap.ScissorTest);
        
        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
    }
    
    public void WindowResized(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
    }
    
    public void Dispose()
    {
        GL.DeleteVertexArray(_vertexArray);
        GL.DeleteBuffer(_vertexBuffer);
        GL.DeleteBuffer(_indexBuffer);
        GL.DeleteTexture(_fontTexture);
        GL.DeleteProgram(_shaderProgram);
    }
    
    private struct ImGuiVertex
    {
        public const int Size = 20;
    }
}
