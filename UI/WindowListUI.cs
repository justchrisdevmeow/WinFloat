using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace WinFloat;

public class WindowListUI
{
    private MainGame _game;
    private List<WindowCapture.WindowInfo> _windows = new();
    private int _selectedIndex = -1;
    private bool _showUI = true;
    private float _uiWidth = 250f;
    
    // UI state
    private bool _liveCapture = true;
    private int _currentShape = 0;
    private int _currentAnimation = 0;
    private float _animationSpeed = 1f;
    
    // Events
    public event Action<IntPtr>? OnWindowSelected;
    public event Action<int>? OnShapeChanged;
    public event Action<int>? OnAnimationChanged;
    public event Action<float>? OnAnimationSpeedChanged;
    public event Action<bool>? OnLiveCaptureToggled;
    public event Action? OnLoadModelClicked;
    public event Action? OnChaosModeClicked;
    
    private readonly string[] _shapeNames = { "Cube", "Sphere", "Torus", "Cylinder", "Plane" };
    private readonly string[] _animationNames = { "None", "Spin", "Bounce", "Pulse", "Wobble" };

    public WindowListUI(MainGame game)
    {
        _game = game;
        
        // Setup keyboard toggle for UI
        _game.KeyDown += OnKeyDown;
    }

    private void OnKeyDown(OpenTK.Windowing.Common.KeyboardKeyEventArgs e)
    {
        if (e.Key == Keys.Tab)
        {
            _showUI = !_showUI;
        }
    }

    public void RefreshWindowList(List<WindowCapture.WindowInfo> windows)
    {
        _windows = windows;
    }

    public void Update()
    {
        if (!_showUI) return;
        
        // This is called from game loop - UI rendering would go here
        // For now, we'll handle input and let the game render the UI overlay
    }

    public void SetStatus(string message)
    {
        // Status will be shown in UI overlay
        Console.WriteLine($"[WinFloat] {message}");
    }

    // These methods would be called by the UI (if we had one)
    // For now, we'll simulate button clicks with keyboard for testing
    
    public void NextWindow()
    {
        if (_windows.Count == 0) return;
        _selectedIndex = (_selectedIndex + 1) % _windows.Count;
        OnWindowSelected?.Invoke(_windows[_selectedIndex].Handle);
        SetStatus($"Selected: {_windows[_selectedIndex].Title}");
    }
    
    public void PreviousWindow()
    {
        if (_windows.Count == 0) return;
        _selectedIndex = (_selectedIndex - 1 + _windows.Count) % _windows.Count;
        OnWindowSelected?.Invoke(_windows[_selectedIndex].Handle);
        SetStatus($"Selected: {_windows[_selectedIndex].Title}");
    }
    
    public void NextShape()
    {
        _currentShape = (_currentShape + 1) % _shapeNames.Length;
        OnShapeChanged?.Invoke(_currentShape);
        SetStatus($"Shape: {_shapeNames[_currentShape]}");
    }
    
    public void PreviousShape()
    {
        _currentShape = (_currentShape - 1 + _shapeNames.Length) % _shapeNames.Length;
        OnShapeChanged?.Invoke(_currentShape);
        SetStatus($"Shape: {_shapeNames[_currentShape]}");
    }
    
    public void NextAnimation()
    {
        _currentAnimation = (_currentAnimation + 1) % _animationNames.Length;
        OnAnimationChanged?.Invoke(_currentAnimation);
        SetStatus($"Animation: {_animationNames[_currentAnimation]}");
    }
    
    public void PreviousAnimation()
    {
        _currentAnimation = (_currentAnimation - 1 + _animationNames.Length) % _animationNames.Length;
        OnAnimationChanged?.Invoke(_currentAnimation);
        SetStatus($"Animation: {_animationNames[_currentAnimation]}");
    }
    
    public void IncreaseSpeed()
    {
        _animationSpeed = Math.Min(_animationSpeed + 0.25f, 3f);
        OnAnimationSpeedChanged?.Invoke(_animationSpeed);
        SetStatus($"Speed: {_animationSpeed}x");
    }
    
    public void DecreaseSpeed()
    {
        _animationSpeed = Math.Max(_animationSpeed - 0.25f, 0.25f);
        OnAnimationSpeedChanged?.Invoke(_animationSpeed);
        SetStatus($"Speed: {_animationSpeed}x");
    }
    
    public void ToggleLiveCapture()
    {
        _liveCapture = !_liveCapture;
        OnLiveCaptureToggled?.Invoke(_liveCapture);
        SetStatus(_liveCapture ? "Live Capture: ON" : "Live Capture: OFF");
    }
    
    public void LoadModel()
    {
        OnLoadModelClicked?.Invoke();
    }
    
    public void ChaosMode()
    {
        OnChaosModeClicked?.Invoke();
        SetStatus("CHAOS MODE!");
    }
}
