using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Assimp;
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
    
    private int _currentShape = 0;
    private int _currentAnimation = 0;
    private float _animationTime = 0f;
    private Vector3 _position = Vector3.Zero;
    
    private List<float> _vertices = new();
    private List<uint> _indices = new();
    
    // Custom model data
    private List<float> _customVertices = new();
    private List<uint> _customIndices = new();
    private bool _usingCustomModel = false;

    public CubeRenderer()
    {
        BuildCube();
    }

    public void Load()
    {
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
        
        _uniformModelView = GL.GetUniformLocation(_shaderProgram, "uModelView");
        _uniformProjection = GL.GetUniformLocation(_shaderProgram, "uProjection");
        
        _vertexArray = GL.GenVertexArray();
        _vertexBuffer = GL.GenBuffer();
        _indexBuffer = GL.GenBuffer();
        
        UploadGeometry();
        
        _viewMatrix = Matrix4.LookAt(new Vector3(3, 3, 5), Vector3.Zero, Vector3.UnitY);
        _projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45f), (float)_windowWidth / _windowHeight, 0.1f, 100f);
    }

    private void UploadGeometry()
    {
        var vertices = _usingCustomModel ? _customVertices : _vertices;
        var indices = _usingCustomModel ? _customIndices : _indices;
        
        if (vertices.Count == 0) return;
        
        GL.BindVertexArray(_vertexArray);
        
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.StaticDraw);
        
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.StaticDraw);
        
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        
        GL.BindVertexArray(0);
    }

    #region Shape Generation

    private void BuildCube()
    {
        _vertices.Clear();
        _indices.Clear();
        _usingCustomModel = false;
        
        AddQuad(new Vector3(-0.5f, -0.5f,  0.5f), new Vector3( 0.5f,  0.5f,  0.5f), new Vector2(0,0), new Vector2(1,1));
        AddQuad(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3( 0.5f,  0.5f, -0.5f), new Vector2(0,0), new Vector2(1,1));
        AddQuad(new Vector3(-0.5f, -0.5f,  0.5f), new Vector3(-0.5f,  0.5f, -0.5f), new Vector2(0,0), new Vector2(1,1));
        AddQuad(new Vector3( 0.5f, -0.5f, -0.5f), new Vector3( 0.5f,  0.5f,  0.5f), new Vector2(0,0), new Vector2(1,1));
        AddQuad(new Vector3(-0.5f,  0.5f,  0.5f), new Vector3( 0.5f,  0.5f, -0.5f), new Vector2(0,0), new Vector2(1,1));
        AddQuad(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3( 0.5f, -0.5f,  0.5f), new Vector2(0,0), new Vector2(1,1));
        
        if (_vertexArray != 0) UploadGeometry();
    }

    private void AddQuad(Vector3 min, Vector3 max, Vector2 uvMin, Vector2 uvMax)
    {
        uint startIndex = (uint)_vertices.Count / 5;
        
        _vertices.AddRange(new float[] { min.X, min.Y, max.Z, uvMin.X, uvMin.Y });
        _vertices.AddRange(new float[] { max.X, min.Y, max.Z, uvMax.X, uvMin.Y });
        _vertices.AddRange(new float[] { max.X, max.Y, max.Z, uvMax.X, uvMax.Y });
        _vertices.AddRange(new float[] { min.X, min.Y, max.Z, uvMin.X, uvMin.Y });
        _vertices.AddRange(new float[] { max.X, max.Y, max.Z, uvMax.X, uvMax.Y });
        _vertices.AddRange(new float[] { min.X, max.Y, max.Z, uvMin.X, uvMax.Y });
        
        for (uint i = 0; i < 6; i++)
            _indices.Add(startIndex + i);
    }

    private void BuildSphere(float radius, int segments)
    {
        _vertices.Clear();
        _indices.Clear();
        _usingCustomModel = false;
        
        for (int lat = 0; lat <= segments; lat++)
        {
            float theta = lat * MathF.PI / segments;
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);
            
            for (int lon = 0; lon <= segments; lon++)
            {
                float phi = lon * 2 * MathF.PI / segments;
                float sinPhi = MathF.Sin(phi);
                float cosPhi = MathF.Cos(phi);
                
                float x = radius * cosPhi * sinTheta;
                float y = radius * cosTheta;
                float z = radius * sinPhi * sinTheta;
                
                float u = (float)lon / segments;
                float v = (float)lat / segments;
                
                _vertices.Add(x);
                _vertices.Add(y);
                _vertices.Add(z);
                _vertices.Add(u);
                _vertices.Add(v);
            }
        }
        
        for (int lat = 0; lat < segments; lat++)
        {
            for (int lon = 0; lon < segments; lon++)
            {
                int first = lat * (segments + 1) + lon;
                int second = first + segments + 1;
                
                _indices.Add((uint)first);
                _indices.Add((uint)second);
                _indices.Add((uint)(first + 1));
                
                _indices.Add((uint)second);
                _indices.Add((uint)(second + 1));
                _indices.Add((uint)(first + 1));
            }
        }
        
        if (_vertexArray != 0) UploadGeometry();
    }

    private void BuildTorus(float radius, float tubeRadius, int radialSegments, int tubularSegments)
    {
        _vertices.Clear();
        _indices.Clear();
        _usingCustomModel = false;
        
        for (int i = 0; i <= radialSegments; i++)
        {
            float u = (float)i / radialSegments;
            float ringAngle = u * 2 * MathF.PI;
            
            for (int j = 0; j <= tubularSegments; j++)
            {
                float v = (float)j / tubularSegments;
                float tubeAngle = v * 2 * MathF.PI;
                
                float x = (radius + tubeRadius * MathF.Cos(tubeAngle)) * MathF.Cos(ringAngle);
                float y = (radius + tubeRadius * MathF.Cos(tubeAngle)) * MathF.Sin(ringAngle);
                float z = tubeRadius * MathF.Sin(tubeAngle);
                
                _vertices.Add(x);
                _vertices.Add(y);
                _vertices.Add(z);
                _vertices.Add(u);
                _vertices.Add(v);
            }
        }
        
        for (int i = 0; i < radialSegments; i++)
        {
            for (int j = 0; j < tubularSegments; j++)
            {
                int first = i * (tubularSegments + 1) + j;
                int second = first + tubularSegments + 1;
                
                _indices.Add((uint)first);
                _indices.Add((uint)second);
                _indices.Add((uint)(first + 1));
                
                _indices.Add((uint)second);
                _indices.Add((uint)(second + 1));
                _indices.Add((uint)(first + 1));
            }
        }
        
        if (_vertexArray != 0) UploadGeometry();
    }

    private void BuildCylinder(float radius, float height, int segments)
    {
        _vertices.Clear();
        _indices.Clear();
        _usingCustomModel = false;
        
        float halfHeight = height / 2;
        
        // Top and bottom caps
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * 2 * MathF.PI / segments;
            float x = radius * MathF.Cos(angle);
            float z = radius * MathF.Sin(angle);
            float u = (float)i / segments;
            
            // Top cap vertices
            _vertices.Add(x);
            _vertices.Add(halfHeight);
            _vertices.Add(z);
            _vertices.Add(u);
            _vertices.Add(1f);
            
            // Bottom cap vertices
            _vertices.Add(x);
            _vertices.Add(-halfHeight);
            _vertices.Add(z);
            _vertices.Add(u);
            _vertices.Add(0f);
        }
        
        // Side vertices (duplicate for proper UV mapping)
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * 2 * MathF.PI / segments;
            float x = radius * MathF.Cos(angle);
            float z = radius * MathF.Sin(angle);
            float u = (float)i / segments;
            
            _vertices.Add(x);
            _vertices.Add(halfHeight);
            _vertices.Add(z);
            _vertices.Add(u);
            _vertices.Add(1f);
            
            _vertices.Add(x);
            _vertices.Add(-halfHeight);
            _vertices.Add(z);
            _vertices.Add(u);
            _vertices.Add(0f);
        }
        
        int topStart = 0;
        int bottomStart = (segments + 1);
        int sideStart = (segments + 1) * 2;
        
        // Top cap indices
        for (int i = 0; i < segments; i++)
        {
            _indices.Add((uint)(topStart + i));
            _indices.Add((uint)(topStart + i + 1));
            _indices.Add((uint)(topStart + segments + 1));
        }
        
        // Bottom cap indices
        for (int i = 0; i < segments; i++)
        {
            _indices.Add((uint)(bottomStart + i));
            _indices.Add((uint)(bottomStart + i + 1));
            _indices.Add((uint)(bottomStart + segments + 1));
        }
        
        // Side indices
        for (int i = 0; i < segments; i++)
        {
            int idx = sideStart + i * 2;
            _indices.Add((uint)idx);
            _indices.Add((uint)(idx + 1));
            _indices.Add((uint)(idx + 2));
            
            _indices.Add((uint)(idx + 1));
            _indices.Add((uint)(idx + 3));
            _indices.Add((uint)(idx + 2));
        }
        
        if (_vertexArray != 0) UploadGeometry();
    }

    private void BuildPlane(float size)
    {
        _vertices.Clear();
        _indices.Clear();
        _usingCustomModel = false;
        
        float half = size / 2;
        
        _vertices.AddRange(new float[] { -half, 0, -half, 0, 0 });
        _vertices.AddRange(new float[] {  half, 0, -half, 1, 0 });
        _vertices.AddRange(new float[] {  half, 0,  half, 1, 1 });
        _vertices.AddRange(new float[] { -half, 0,  half, 0, 1 });
        
        _indices.Add(0); _indices.Add(1); _indices.Add(2);
        _indices.Add(0); _indices.Add(2); _indices.Add(3);
        
        if (_vertexArray != 0) UploadGeometry();
    }

    #endregion

    public void SetShape(int shape)
    {
        _currentShape = shape;
        switch (shape)
        {
            case 0: BuildCube(); break;
            case 1: BuildSphere(0.6f, 32); break;
            case 2: BuildTorus(0.7f, 0.2f, 48, 24); break;
            case 3: BuildCylinder(0.5f, 1.0f, 32); break;
            case 4: BuildPlane(1.5f); break;
        }
    }

    public void LoadCustomModel(string filePath)
    {
        try
        {
            using var importer = new AssimpContext();
            var scene = importer.ImportFile(filePath, PostProcessSteps.Triangulate | PostProcessSteps.FlipUVs);
            
            if (scene == null || scene.MeshCount == 0)
                throw new Exception("No meshes found in file");
            
            _customVertices.Clear();
            _customIndices.Clear();
            
            foreach (var mesh in scene.Meshes)
            {
                uint vertexOffset = (uint)_customVertices.Count / 5;
                
                for (int i = 0; i < mesh.VertexCount; i++)
                {
                    var v = mesh.Vertices[i];
                    var uv = mesh.HasTextureCoords(0) ? mesh.TextureCoordinateChannels[0][i] : new Vector3D(0, 0, 0);
                    
                    _customVertices.Add(v.X);
                    _customVertices.Add(v.Y);
                    _customVertices.Add(v.Z);
                    _customVertices.Add(uv.X);
                    _customVertices.Add(uv.Y);
                }
                
                foreach (var face in mesh.Faces)
                {
                    for (int i = 0; i < face.IndexCount; i++)
                    {
                        _customIndices.Add(vertexOffset + (uint)face.Indices[i]);
                    }
                }
            }
            
            _usingCustomModel = true;
            UploadGeometry();
            
            // Center the model
            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);
            for (int i = 0; i < _customVertices.Count / 5; i++)
            {
                float x = _customVertices[i*5];
                float y = _customVertices[i*5+1];
                float z = _customVertices[i*5+2];
                min.X = Math.Min(min.X, x);
                min.Y = Math.Min(min.Y, y);
                min.Z = Math.Min(min.Z, z);
                max.X = Math.Max(max.X, x);
                max.Y = Math.Max(max.Y, y);
                max.Z = Math.Max(max.Z, z);
            }
            
            Vector3 center = (min + max) / 2;
            float maxDim = Math.Max(max.X - min.X, Math.Max(max.Y - min.Y, max.Z - min.Z));
            float scale = 1.0f / maxDim;
            
            // Apply centering and scaling by adjusting vertex positions
            for (int i = 0; i < _customVertices.Count / 5; i++)
            {
                _customVertices[i*5] = (_customVertices[i*5] - center.X) * scale;
                _customVertices[i*5+1] = (_customVertices[i*5+1] - center.Y) * scale;
                _customVertices[i*5+2] = (_customVertices[i*5+2] - center.Z) * scale;
            }
            
            UploadGeometry();
            Console.WriteLine($"Loaded custom model: {System.IO.Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load model: {ex.Message}");
        }
    }

    public void Render()
    {
        GL.Enable(EnableCap.DepthTest);
        GL.ClearColor(0.1f, 0.1f, 0.15f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        GL.UseProgram(_shaderProgram);
        
        UpdateModelMatrix();
        var modelView = _viewMatrix * _modelMatrix;
        GL.UniformMatrix4(_uniformModelView, false, ref modelView);
        GL.UniformMatrix4(_uniformProjection, false, ref _projectionMatrix);
        
        if (_textureId > 0)
        {
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _textureId);
            GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "uTexture"), 0);
        }
        
        GL.BindVertexArray(_vertexArray);
        int indexCount = _usingCustomModel ? _customIndices.Count : _indices.Count;
        if (indexCount > 0)
            GL.DrawElements(PrimitiveType.Triangles, indexCount, DrawElementsType.UnsignedInt, 0);
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

    public void SetTexture(int textureId) => _textureId = textureId;
    public void SetAnimation(int animation, float time) { _currentAnimation = animation; _animationTime = time; }
    public void SetCubePosition(Vector3 position) => _position = position;
    public Vector3 GetCubePosition() => _position;
    public void MoveCube(Vector2 mouseDelta) => _position += new Vector3(mouseDelta.X * 0.01f, -mouseDelta.Y * 0.01f, 0);

    public void Unload()
    {
        GL.DeleteBuffer(_vertexBuffer);
        GL.DeleteBuffer(_indexBuffer);
        GL.DeleteVertexArray(_vertexArray);
        GL.DeleteProgram(_shaderProgram);
    }
}
