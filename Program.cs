using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using Vector2 = OpenTK.Mathematics.Vector2;
using Vector3 = OpenTK.Mathematics.Vector3;
using Keys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;

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

public class MainGame : GameWindow
{
    private CubeRenderer _renderer;
    private WindowCapture _capture;
    private Physics _physics;
    private Form _controlPanel;
    private ListBox _windowList;
    private Button _captureBtn, _chaosBtn, _resetBtn;
    private ComboBox _shapeCombo, _animationCombo;
    private TrackBar _speedSlider;
    private CheckBox _liveCheck;
    private Label _statusLabel;
    
    private List<WindowCapture.WindowInfo> _windows = new();
    private IntPtr _selectedWindowHandle = IntPtr.Zero;
    private bool _liveCapture = true;
    private int _currentShape = 0;
    private int _currentAnimation = 0;
    private float _animationSpeed = 1.0f;
    private float _animationTime = 0f;
    private bool _chaosMode = false;
    private float _chaosTimer = 0f;
    private Random _random = new Random();
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
        
        CreateControlPanel();
        RefreshWindowList();
        
        SetStatus("WinFloat Ready");
    }
    
    private void CreateControlPanel()
    {
        _controlPanel = new Form
        {
            Text = "WinFloat Control Panel",
            Size = new Size(300, 500),
            StartPosition = FormStartPosition.Manual,
            Location = new Point(10, 10),
            FormBorderStyle = FormBorderStyle.SizableToolWindow,
            TopMost = true
        };
        
        // Window list
        var listLabel = new Label { Text = "Open Windows:", Location = new Point(10, 10), Size = new Size(280, 20) };
        _windowList = new ListBox { Location = new Point(10, 30), Size = new Size(280, 150) };
        _windowList.SelectedIndexChanged += OnWindowSelected;
        
        // Capture button
        _captureBtn = new Button { Text = "Capture Selected", Location = new Point(10, 190), Size = new Size(135, 30) };
        _captureBtn.Click += (s, e) => CaptureSelectedWindow();
        
        var refreshBtn = new Button { Text = "Refresh List", Location = new Point(155, 190), Size = new Size(135, 30) };
        refreshBtn.Click += (s, e) => RefreshWindowList();
        
        // Live capture checkbox
        _liveCheck = new CheckBox { Text = "Live Capture", Location = new Point(10, 230), Size = new Size(100, 20), Checked = true };
        _liveCheck.CheckedChanged += (s, e) => { _liveCapture = _liveCheck.Checked; SetStatus(_liveCapture ? "Live Capture ON" : "Static Capture ON"); };
        
        // Shape combo
        var shapeLabel = new Label { Text = "Shape:", Location = new Point(10, 260), Size = new Size(50, 20) };
        _shapeCombo = new ComboBox { Location = new Point(70, 258), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        _shapeCombo.Items.AddRange(new[] { "Cube", "Sphere", "Torus", "Cylinder", "Plane" });
        _shapeCombo.SelectedIndex = 0;
        _shapeCombo.SelectedIndexChanged += (s, e) => { _currentShape = _shapeCombo.SelectedIndex; _renderer.SetShape(_currentShape); };
        
        // Animation combo
        var animLabel = new Label { Text = "Animation:", Location = new Point(10, 290), Size = new Size(60, 20) };
        _animationCombo = new ComboBox { Location = new Point(70, 288), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        _animationCombo.Items.AddRange(new[] { "None", "Spin", "Bounce", "Pulse", "Wobble" });
        _animationCombo.SelectedIndex = 0;
        _animationCombo.SelectedIndexChanged += (s, e) => { _currentAnimation = _animationCombo.SelectedIndex; _animationTime = 0; };
        
        // Speed slider
        var speedLabel = new Label { Text = "Speed:", Location = new Point(10, 320), Size = new Size(50, 20) };
        _speedSlider = new TrackBar { Location = new Point(70, 318), Size = new Size(200, 30), Minimum = 25, Maximum = 300, Value = 100 };
        _speedSlider.ValueChanged += (s, e) => { _animationSpeed = _speedSlider.Value / 100f; SetStatus($"Speed: {_animationSpeed:F2}x"); };
        
        // Buttons
        _chaosBtn = new Button { Text = "Chaos Mode", Location = new Point(10, 360), Size = new Size(135, 30), BackColor = Color.Orange };
        _chaosBtn.Click += (s, e) => { _chaosMode = !_chaosMode; SetStatus(_chaosMode ? "CHAOS MODE ACTIVE" : "Chaos stopped"); };
        
        _resetBtn = new Button { Text = "Reset Position", Location = new Point(155, 360), Size = new Size(135, 30) };
        _resetBtn.Click += (s, e) => { _physics.ResetPosition(); _renderer.SetCubePosition(Vector3.Zero); SetStatus("Position reset"); };
        
        var loadModelBtn = new Button { Text = "Load Custom Model", Location = new Point(10, 400), Size = new Size(280, 30) };
        loadModelBtn.Click += (s, e) => LoadCustomModel();
        
        // Status label
        _statusLabel = new Label { Text = "Ready", Location = new Point(10, 440), Size = new Size(280, 20), ForeColor = Color.Green };
        
        // Add controls to form
        _controlPanel.Controls.AddRange(new Control[] { listLabel, _windowList, _captureBtn, refreshBtn, _liveCheck, shapeLabel, _shapeCombo, animLabel, _animationCombo, speedLabel, _speedSlider, _chaosBtn, _resetBtn, loadModelBtn, _statusLabel });
        
        _controlPanel.Show();
    }
    
    private void RefreshWindowList()
    {
        _windows = _capture.GetVisibleWindows();
        _windowList.Items.Clear();
        foreach (var w in _windows)
            _windowList.Items.Add(w.Title);
    }
    
    private void OnWindowSelected(object sender, EventArgs e)
    {
        if (_windowList.SelectedIndex >= 0 && _windowList.SelectedIndex < _windows.Count)
            CaptureSelectedWindow();
    }
    
    private void CaptureSelectedWindow()
    {
        if (_windowList.SelectedIndex < 0 || _windowList.SelectedIndex >= _windows.Count) return;
        _selectedWindowHandle = _windows[_windowList.SelectedIndex].Handle;
        var texture = _capture.CaptureWindow(_selectedWindowHandle);
        if (texture != -1)
            _renderer.SetTexture(texture);
        SetStatus($"Captured: {_windows[_windowList.SelectedIndex].Title}");
    }
    
    private void LoadCustomModel()
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
    
    private void SetStatus(string msg)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = msg;
            _statusLabel.ForeColor = msg.Contains("ERROR") ? Color.Red : Color.Green;
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        _deltaTime = (float)args.Time;
        
        if (_currentAnimation != 0)
            _animationTime += _deltaTime * _animationSpeed;
        
        if (_chaosMode)
        {
            _chaosTimer -= _deltaTime;
            if (_chaosTimer <= 0)
            {
                _physics.ApplyForce((float)(_random.NextDouble() - 0.5) * 15f, (float)(_random.NextDouble() - 0.5) * 10f, (float)(_random.NextDouble() - 0.5) * 15f);
                _chaosTimer = 0.1f;
            }
        }
        
        var currentPos = _renderer.GetCubePosition();
        _physics.Update(_deltaTime, currentPos);
        _renderer.SetCubePosition(_physics.GetPosition());
        
        if (_liveCapture && _selectedWindowHandle != IntPtr.Zero)
        {
            var texture = _capture.CaptureWindow(_selectedWindowHandle);
            if (texture != -1)
                _renderer.SetTexture(texture);
        }
        
        _renderer.SetShape(_currentShape);
        _renderer.SetAnimation(_currentAnimation, _animationTime);
        _renderer.Render();
        
        SwapBuffers();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        var keyboard = KeyboardState;
        if (keyboard.IsKeyPressed(Keys.Space)) { _chaosMode = !_chaosMode; SetStatus(_chaosMode ? "CHAOS MODE" : "Chaos stopped"); }
        if (keyboard.IsKeyPressed(Keys.R)) { _physics.ResetPosition(); _renderer.SetCubePosition(Vector3.Zero); SetStatus("Position reset"); }
        if (keyboard.IsKeyPressed(Keys.Escape)) { _controlPanel.Close(); Close(); }
    }

    public void ThrowCube(float velocityX, float velocityY)
    {
        _physics.Throw(velocityX, velocityY);
        SetStatus("Thrown!");
    }
    
    public CubeRenderer GetRenderer() => _renderer;

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        _renderer?.Resize(e.Width, e.Height);
    }

    protected override void OnUnload()
    {
        _controlPanel?.Close();
        _renderer?.Unload();
        base.OnUnload();
    }
}
