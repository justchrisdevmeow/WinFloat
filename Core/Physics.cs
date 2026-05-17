using System;
using OpenTK.Mathematics;

namespace WinFloat;

public class Physics
{
    private Vector3 _position;
    private Vector3 _velocity;
    private bool _useGravity = true;
    private float _groundY = -1.5f;
    private float _bounciness = 0.6f;
    private float _drag = 0.98f;  // Air resistance

    public Physics()
    {
        _position = Vector3.Zero;
        _velocity = Vector3.Zero;
    }

    public void Update(float deltaTime, Vector3 currentPosition)
    {
        // Sync position (in case renderer moved it)
        _position = currentPosition;

        // Apply gravity
        if (_useGravity)
        {
            _velocity.Y -= 9.8f * deltaTime;
        }

        // Apply drag
        _velocity *= _drag;

        // Update position
        _position += _velocity * deltaTime;

        // Ground collision
        if (_position.Y <= _groundY)
        {
            _position.Y = _groundY;
            
            // Reverse Y velocity and reduce by bounciness
            if (_velocity.Y < 0)
                _velocity.Y = -_velocity.Y * _bounciness;
            
            // Stop bouncing if very small
            if (Math.Abs(_velocity.Y) < 0.5f)
                _velocity.Y = 0;
        }

        // Simple boundaries (X and Z limits)
        float limit = 8f;
        if (Math.Abs(_position.X) > limit)
        {
            _position.X = Math.Sign(_position.X) * limit;
            _velocity.X *= -_bounciness;
        }
        if (Math.Abs(_position.Z) > limit)
        {
            _position.Z = Math.Sign(_position.Z) * limit;
            _velocity.Z *= -_bounciness;
        }
    }

    public void ApplyForce(float x, float y, float z)
    {
        _velocity.X += x;
        _velocity.Y += y;
        _velocity.Z += z;
    }

    public void ApplyForce(Vector3 force)
    {
        _velocity += force;
    }

    public void Throw(float velocityX, float velocityY)
    {
        // Convert screen drag to 3D velocity
        _velocity.X = velocityX * 5f;
        _velocity.Z = velocityY * 5f;
        _velocity.Y = 3f;  // Small upward arc
    }

    public void ResetPosition()
    {
        _position = Vector3.Zero;
        _velocity = Vector3.Zero;
    }

    public void SetPosition(Vector3 pos)
    {
        _position = pos;
    }

    public Vector3 GetPosition() => _position;
    public Vector3 GetVelocity() => _velocity;

    public void SetGravity(bool enabled) => _useGravity = enabled;
    public void SetGroundY(float y) => _groundY = y;
    public void SetBounciness(float value) => _bounciness = Math.Clamp(value, 0f, 1f);
}
