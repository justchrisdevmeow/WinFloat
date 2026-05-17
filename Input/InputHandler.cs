using System;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;

namespace WinFloat;

public class InputHandler
{
    private MainGame _game;
    private bool _isDragging = false;
    private Vector2 _lastMousePos;
    private Vector2 _dragStartPos;
    private bool _wasMouseDown = false;

    public InputHandler(MainGame game)
    {
        _game = game;
    }

    public void Update()
    {
        var mouseState = _game.MouseState;
        var keyboardState = _game.KeyboardState;
        var mouse = _game.MousePosition;

        // Left click: start drag or throw
        if (mouseState.IsButtonDown(MouseButton.Left))
        {
            if (!_wasMouseDown)
            {
                // Just pressed
                _isDragging = true;
                _dragStartPos = new Vector2(mouse.X, mouse.Y);
                _lastMousePos = new Vector2(mouse.X, mouse.Y);
            }
            else if (_isDragging)
            {
                // Dragging: move cube
                Vector2 currentPos = new Vector2(mouse.X, mouse.Y);
                Vector2 delta = currentPos - _lastMousePos;
                
                if (delta.LengthSquared > 0.01f)
                {
                    _game.MoveCube(delta);
                }
                
                _lastMousePos = currentPos;
            }
            _wasMouseDown = true;
        }
        else
        {
            if (_wasMouseDown && _isDragging)
            {
                // Released: throw cube
                Vector2 releasePos = new Vector2(mouse.X, mouse.Y);
                Vector2 dragVector = releasePos - _dragStartPos;
                
                // Scale drag to velocity (limit max speed)
                float speedX = Math.Clamp(dragVector.X * 0.05f, -8f, 8f);
                float speedY = Math.Clamp(dragVector.Y * 0.05f, -8f, 8f);
                
                _game.ThrowCube(speedX, speedY);
            }
            
            _isDragging = false;
            _wasMouseDown = false;
        }

        // Right click: reset position
        if (mouseState.IsButtonPressed(MouseButton.Right))
        {
            _game.ThrowCube(0, 0);
            // Force reset position (physics will handle next frame)
        }

        // Keyboard shortcuts
        if (keyboardState.IsKeyPressed(Keys.Space))
        {
            // Toggle chaos mode
            _game.ActivateChaosMode();
        }
        
        if (keyboardState.IsKeyPressed(Keys.R))
        {
            // Reset cube position
            _game.ThrowCube(0, 0);
        }
        
        if (keyboardState.IsKeyPressed(Keys.Escape))
        {
            _game.Close();
        }
    }
}
