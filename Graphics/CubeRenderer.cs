using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

namespace WinFloat;

public class CubeRenderer
{
    private int _vertexArray;
    private int _vertexBuffer;
    private int _indexBuffer;
    private int _shaderProgram;
    private int _textureId = -1;
    private int _uniformModelView;
    private int _uniformProjection;
    
    private Matrix4 _modelMatrix = Matrix4.Identity;
    private Matrix4 _viewMatrix;
    private Matrix4 _projectionMatrix;
    
    private int _windowWidth = 1280;
    private int _windowHeight = 720;
    
    // Current shape: 0=cube,1=sphere,2=torus,3=cylinder,4=plane
    private int _currentShape = 0;
    
    // Animation: 0=none,1=spin,2=bounce,3=pulse,4=wobble
    private int _currentAnimation = 0;
    private float _animationTime = 0f;
    private Vector3 _position = Vector3.Zero;
    
    private List<float> _vertices = new();
    private List<uint> _indices = new();

    public CubeRenderer()
    {
        BuildCube(); // Default shape
    }

    public void Load()
    {
        // Compile shaders
        string vertexShaderSource = @"
            #version 330 core
            layout (location = 0) in vec3 aPosition;
            layout (location = 1) in vec2 aTexCoord;
            
            uniform mat4 uModelView;
            uniform mat4 uProjection;
            
            out vec2 vTexCoord;
            
            void main()
            {
                gl_Position = uProjection * uModelView * vec4(aPosition, 1.0);
                vTexCoord = aTexCoord;
            }
        ";
        
        string fragmentShaderSource = @"
            #version 330 core
            in vec2 vTexCoord;
            uniform sampler2D uTexture;
            out vec4 FragColor;
            
            void main()
            {
                FragColor = texture(uTexture, vTexCoord);
            }
        ";
        
        // Compile vertex shader
        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexShaderSource);
        GL.CompileShader(vertexShader);
        
        // Compile fragment shader
        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);
        GL.CompileShader(fragmentShader);
        
        // Link program
        _shaderProgram = GL.CreateProgram();
        GL.AttachShader(_shaderProgram, vertexShader);
        GL.AttachShader(_shaderProgram, fragmentShader);
        GL.LinkProgram(_shaderProgram);
        
        GL.DetachShader(_shaderProgram, vertexShader);
        GL.DetachShader(_shaderProgram, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
        
        // Get uniform locations
        _uniformModelView = GL.GetUniformLocation(_shaderProgram, "uModelView");
        _uniformProjection = GL.GetUniformLocation(_shaderProgram, "uProjection");
        
        // Setup buffers
        _vertexArray = GL.GenVertexArray();
        _vertexBuffer = GL.GenBuffer();
        _indexBuffer = GL.GenBuffer();
        
        GL.BindVertexArray(_vertexArray);
        
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Count * sizeof(float), _vertices.ToArray(), BufferUsageHint.StaticDraw);
        
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Count * sizeof(uint), _indices.ToArray(), BufferUsageHint.StaticDraw);
        
        // Position attribute
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        
        // TexCoord attribute
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        
        GL.BindVertexArray(0);
        
        // Setup projection matrix
        _projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45f), (float)_windowWidth / _windowHeight, 0.1f, 100f);
        
        // Setup view matrix (camera position)
        _viewMatrix = Matrix4.LookAt(new Vector3(3, 3, 5), Vector3.Zero, Vector3.UnitY);
    }

    private void BuildCube()
    {
        _vertices.Clear();
        _indices.Clear();
        
        // Front face (z = 0.5f)
        AddQuad(new Vector3(-0.5f, -0.5f,  0.5f), new Vector3( 0.5f,  0.5f,  0.5f), new Vector2(0,0), new Vector2(1,1));
        // Back face (z = -0.5f)
        AddQuad(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3( 0.5f,  0.5f, -0.5f), new Vector2(0,0), new Vector2(1,1));
        // Left face (x = -0.5f)
        AddQuad(new Vector3(-0.5f, -0.5f,  0.5f), new Vector3(-0.5f,  0.5f, -0.5f), new Vector2(0,0), new Vector2(1,1));
        // Right face (x = 0.5f)
        AddQuad(new Vector3( 0.5f, -0.5f, -0.5f), new Vector3( 0.5f,  0.5f,  0.5f), new Vector2(0,0), new Vector2(1,1));
        // Top face (y = 0.5f)
        AddQuad(new Vector3(-0.5f,  0.5f,  0.5f), new Vector3( 0.5f,  0.5f, -0.5f), new Vector2(0,0), new Vector2(1,1));
        // Bottom face (y = -0.5f)
        AddQuad(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3( 0.5f, -0.5f,  0.5f), new Vector2(0,0), new Vector2(1,1));
    }

    private void AddQuad(Vector3 min, Vector3 max, Vector2 uvMin, Vector2 uvMax)
    {
        uint startIndex = (uint)_vertices.Count / 5;
        
        // Vertex order: front-facing triangles
        // Triangle 1
        _vertices.AddRange(new float[] { min.X, min.Y, max.Z, uvMin.X, uvMin.Y });
        _vertices.AddRange(new float[] { max.X, min.Y, max.Z, uvMax.X, uvMin.Y });
        _vertices.AddRange(new float[] { max.X, max.Y, max.Z, uvMax.X, uvMax.Y });
        // Triangle 2
        _vertices.AddRange(new float[] { min.X, min.Y, max.Z, uvMin.X, uvMin.Y });
        _vertices.AddRange(new float[] { max.X, max.Y, max.Z, uvMax.X, uvMax.Y });
        _vertices.AddRange(new float[] { min.X, max.Y, max.Z, uvMin.X, uvMax.Y });
        
        // Indices
        for (uint i = 0; i < 6; i++)
            _indices.Add(startIndex + i);
    }

    public void Render()
    {
        GL.Enable(EnableCap.DepthTest);
        GL.ClearColor(0.1f, 0.1f, 0.15f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        GL.UseProgram(_shaderProgram);
        
        // Update model matrix with animation
        UpdateModelMatrix();
        
        var modelView = _viewMatrix * _modelMatrix;
        GL.UniformMatrix4(_uniformModelView, false, ref modelView);
        GL.UniformMatrix4(_uniformProjection, false, ref _projectionMatrix);
        
        // Bind texture
        if (_textureId > 0)
        {
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _textureId);
            GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "uTexture"), 0);
        }
        
        GL.BindVertexArray(_vertexArray);
        GL.DrawElements(PrimitiveType.Triangles, _indices.Count, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
    }

    private void UpdateModelMatrix()
    {
        _modelMatrix = Matrix4.CreateTranslation(_position);
        
        if (_currentAnimation == 1) // Spin
        {
            _modelMatrix *= Matrix4.CreateRotationY(_animationTime * 2f);
            _modelMatrix *= Matrix4.CreateRotationX(_animationTime * 1.3f);
        }
        else if (_currentAnimation == 2) // Bounce
        {
            float offsetY = (float)Math.Sin(_animationTime * 3f) * 0.2f;
            _modelMatrix *= Matrix4.CreateTranslation(0, offsetY, 0);
        }
        else if (_currentAnimation == 3) // Pulse
        {
            float scale = 1f + (float)Math.Sin(_animationTime * 4f) * 0.1f;
            _modelMatrix *= Matrix4.CreateScale(scale);
        }
        else if (_currentAnimation == 4) // Wobble
        {
            float angle = (float)Math.Sin(_animationTime * 2f) * 0.2f;
            _modelMatrix *= Matrix4.CreateRotationZ(angle);
            _modelMatrix *= Matrix4.CreateRotationX(angle * 0.5f);
        }
    }

    public void Resize(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
        GL.Viewport(0, 0, width, height);
        _projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45f), (float)width / height, 0.1f, 100f);
    }

    public void SetTexture(int textureId)
    {
        _textureId = textureId;
    }

    public void SetShape(int shape)
    {
        _currentShape = shape;
        // TODO: Build different geometry based on shape
        BuildCube(); // Placeholder - will add sphere, torus, cylinder, plane later
    }

    public void SetAnimation(int animation, float time)
    {
        _currentAnimation = animation;
        _animationTime = time;
    }

    public void SetCubePosition(Vector3 position)
    {
        _position = position;
    }

    public Vector3 GetCubePosition() => _position;

    public void MoveCube(Vector2 mouseDelta)
    {
        // Simple drag movement (placeholder)
        _position.X += mouseDelta.X * 0.01f;
        _position.Y -= mouseDelta.Y * 0.01f;
    }

    public void LoadCustomModel(string filePath)
    {
        // TODO: Use Assimp to load custom model
        Console.WriteLine($"Loading model: {filePath}");
    }

    public void Unload()
    {
        GL.DeleteBuffer(_vertexBuffer);
        GL.DeleteBuffer(_indexBuffer);
        GL.DeleteVertexArray(_vertexArray);
        GL.DeleteProgram(_shaderProgram);
    }
}
