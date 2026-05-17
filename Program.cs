using System;
using System.Collections.Generic;
using System.Windows.Forms;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

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

class MainGame : GameWindow
{
    private CubeRenderer? _renderer;
    private InputHandler? _input;
    private WindowListUI? _ui;
    private WindowCapture? _capture;
    private Physics? _physics;

    private float _deltaTime;
    private bool _liveCapture = true;
    private IntPtr _currentWindowHandle = IntPtr.Zero;

    // Shape and animation state
    private int _currentShape = 0;  // 0=cube,1=sphere,2=torus,3=cylinder,4=plane
    private int _currentAnimation = 0; // 0=none,1=spin,2=bounce,3=pulse,4=wobble
    private float _animationSpeed = 1.0f;
    private float _animationTime = 0f;

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

        _input = new InputHandler(this);
        _ui = new WindowListUI(this);
        _capture = new WindowCapture();
        _physics = new Physics();

        _ui.OnWindowSelected += SelectWindow;
        _ui.OnShapeChanged += ChangeShape;
        _ui.OnAnimationChanged += ChangeAnimation;
        _ui.OnAnimationSpeedChanged += SetAnimationSpeed;
        _ui.OnLiveCaptureToggled += SetLiveCapture;
        _ui.OnLoadModelClicked += LoadCustomModel;
        _ui.OnChaosModeClicked += ActivateChaosMode;

        _ui.RefreshWindowList(_capture.GetVisibleWindows());
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

        // Update renderer with current shape and animation
        _renderer?.SetShape(_currentShape);
        _renderer?.SetAnimation(_currentAnimation, _animationTime);

        _renderer?.Render();
        SwapBuffers();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        _input?.Update();
        
        // Update physics with current cube position
        var pos = _renderer?.GetCubePosition() ?? Vector3.Zero;
        _physics?.Update(_deltaTime, pos);
        
        // Sync renderer position after physics
        var newPos = _physics?.GetPosition() ?? Vector3.Zero;
        _renderer?.SetCubePosition(newPos);
        
        // Update live capture texture every frame if enabled
        if (_liveCapture && _currentWindowHandle != IntPtr.Zero)
        {
            var texture = _capture?.CaptureWindow(_currentWindowHandle);
            if (texture != null)
                _renderer?.SetTexture(texture);
        }

        _ui?.Update();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        _renderer?.Resize(e.Width, e.Height);
    }

    protected override void OnUnload()
    {
        _renderer?.Unload();
        base.OnUnload();
    }

    private void SelectWindow(IntPtr hwnd)
    {
        _currentWindowHandle = hwnd;
        var texture = _capture?.CaptureWindow(hwnd);
        if (texture != null && _renderer != null)
        {
            _renderer.SetTexture(texture);
            _physics?.ResetPosition();
            _ui?.SetStatus($"Captured: {_capture?.GetWindowTitle(hwnd)}");
        }
    }

    private void ChangeShape(int shapeIndex)
    {
        _currentShape = shapeIndex;
        _renderer?.SetShape(shapeIndex);
        _ui?.SetStatus($"Shape changed to: {GetShapeName(shapeIndex)}");
    }

    private void ChangeAnimation(int animationIndex)
    {
        _currentAnimation = animationIndex;
        _animationTime = 0;
        _ui?.SetStatus($"Animation: {GetAnimationName(animationIndex)}");
    }

    private void SetAnimationSpeed(float speed)
    {
        _animationSpeed = speed;
    }

    private void SetLiveCapture(bool enabled)
    {
        _liveCapture = enabled;
        _ui?.SetStatus(enabled ? "Live capture ON" : "Static capture ON (click Refresh to update)");
        
        // If static, capture once now
        if (!enabled && _currentWindowHandle != IntPtr.Zero)
        {
            var texture = _capture?.CaptureWindow(_currentWindowHandle);
            if (texture != null)
                _renderer?.SetTexture(texture);
        }
    }

    private void LoadCustomModel()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Load Custom 3D Model",
            Filter = "3D Models|*.obj;*.gltf;*.stl;*.fbx|OBJ files|*.obj|glTF files|*.gltf|STL files|*.stl|FBX files|*.fbx"
        };
        
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _renderer?.LoadCustomModel(dialog.FileName);
            _ui?.SetStatus($"Loaded model: {System.IO.Path.GetFileName(dialog.FileName)}");
        }
    }

    private void ActivateChaosMode()
    {
        var random = new Random();
        for (int i = 0; i < 30; i++)
        {
            _physics?.ApplyForce(
                (float)(random.NextDouble() - 0.5) * 20f,
                (float)(random.NextDouble() - 0.5) * 20f,
                (float)(random.NextDouble() - 0.5) * 20f
            );
        }
        _ui?.SetStatus("CHAOS MODE ACTIVATED!");
    }

    public void MoveCube(Vector2 mouseDelta) => _renderer?.MoveCube(mouseDelta);
    public void ThrowCube(float velocityX, float velocityY) => _physics?.Throw(velocityX, velocityY);

    private string GetShapeName(int index) => index switch
    {
        0 => "Cube",
        1 => "Sphere",
        2 => "Torus",
        3 => "Cylinder",
        4 => "Plane",
        _ => "Unknown"
    };

    private string GetAnimationName(int index) => index switch
    {
        0 => "None",
        1 => "Spin",
        2 => "Bounce",
        3 => "Pulse",
        4 => "Wobble",
        _ => "Unknown"
    };
}
