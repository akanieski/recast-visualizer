using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Input;
using System.Numerics;
using System.Linq;

namespace RecastVisualizer;

public class NavmeshVisualizer
{
    private IWindow? _window;
    private GL? _gl;
    private IInputContext? _inputContext;
    
    private uint _vao;
    private uint _vbo;
    private uint _ebo;
    private uint _shaderProgram;
    
    private Vector3 _cameraPos = new(0, 3, 5);  // Closer and simpler position
    private Vector3 _cameraTarget = Vector3.Zero;
    private Vector3 _cameraUp = Vector3.UnitY;
    
    private float[] _navmeshVertices = Array.Empty<float>();
    private uint[] _navmeshIndices = Array.Empty<uint>();
    
    private static bool _debugPrinted = false;
    
    public void Run()
    {
        var options = WindowOptions.Default;
        options.Size = new(1024, 768);
        options.Title = "Recast Navmesh Visualizer";
        
        _window = Window.Create(options);
        
        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.Update += OnUpdate;
        _window.Closing += OnClose;
        _window.Resize += OnResize;
        
        _window.Run();
    }
    
    private void OnLoad()
    {
        _gl = _window!.CreateOpenGL();
        _inputContext = _window!.CreateInput();
        
        // Debug OpenGL info
        Console.WriteLine($"OpenGL Version: {_gl.GetStringS(StringName.Version)}");
        Console.WriteLine($"OpenGL Renderer: {_gl.GetStringS(StringName.Renderer)}");
        Console.WriteLine($"OpenGL Vendor: {_gl.GetStringS(StringName.Vendor)}");
        
        // Set viewport
        _gl.Viewport(0, 0, (uint)_window!.Size.X, (uint)_window.Size.Y);
        Console.WriteLine($"Viewport set to: 0, 0, {_window.Size.X}, {_window.Size.Y}");
        
        // Enable depth testing
        _gl.Enable(EnableCap.DepthTest);
        _gl.Disable(EnableCap.CullFace);  // Disable face culling to see triangles from both sides
        _gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line); // Wireframe mode
        _gl.LineWidth(2.0f); // Make lines thicker for visibility
        
        Console.WriteLine("OpenGL state configured: depth test enabled, face culling disabled, wireframe mode");
        
        // Generate a simple navmesh for testing
        GenerateSimpleNavmesh();
        
        // Create OpenGL objects
        CreateShaders();
        CreateBuffers();
        
        Console.WriteLine("Navmesh Visualizer loaded successfully!");
        Console.WriteLine($"Generated navmesh with {_navmeshVertices.Length / 3} vertices and {_navmeshIndices.Length / 3} triangles");
        Console.WriteLine("Controls: The camera will automatically rotate around the navmesh");
        Console.WriteLine("Close the window to exit");
    }
    
    private void GenerateSimpleNavmesh()
    {
        // Create a simple triangle positioned correctly for the camera
        var vertices = new List<float>();
        var indices = new List<uint>();
        
        // Camera is at (0, 3, 5) looking at (0, 0, 0)
        // Place triangle at a reasonable distance - halfway between camera and target
        vertices.AddRange(new[] { -0.5f, 0.0f, -2.5f }); // Left
        vertices.AddRange(new[] {  0.5f, 0.0f, -2.5f }); // Right  
        vertices.AddRange(new[] {  0.0f, 0.5f, -2.5f }); // Top
        
        indices.AddRange(new[] { 0u, 1u, 2u });
        
        _navmeshVertices = vertices.ToArray();
        _navmeshIndices = indices.ToArray();
        
        Console.WriteLine("Generated simple test triangle");
        Console.WriteLine($"Triangle vertices: ({vertices[0]}, {vertices[1]}, {vertices[2]}), ({vertices[3]}, {vertices[4]}, {vertices[5]}), ({vertices[6]}, {vertices[7]}, {vertices[8]})");
    }
    
    private void AddQuad(List<float> vertices, List<uint> indices, ref uint vertexIndex,
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        // Add vertices
        vertices.AddRange(new[] { v1.X, v1.Y, v1.Z });
        vertices.AddRange(new[] { v2.X, v2.Y, v2.Z });
        vertices.AddRange(new[] { v3.X, v3.Y, v3.Z });
        vertices.AddRange(new[] { v4.X, v4.Y, v4.Z });
        
        // Add triangles (two triangles per quad)
        indices.AddRange(new[] { vertexIndex, vertexIndex + 1, vertexIndex + 2 });
        indices.AddRange(new[] { vertexIndex, vertexIndex + 2, vertexIndex + 3 });
        
        vertexIndex += 4;
    }
    
    private void CreateShaders()
    {
        var vertexShaderSource = @"
            #version 330 core
            
            layout (location = 0) in vec3 aPosition;
            
            uniform mat4 uModel;
            uniform mat4 uView;
            uniform mat4 uProjection;
            
            void main()
            {
                gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
            }
        ";
        
        var fragmentShaderSource = @"
            #version 330 core
            
            out vec4 FragColor;
            
            void main()
            {
                FragColor = vec4(0.3, 0.8, 0.3, 1.0); // Green color for navmesh
            }
        ";
        
        uint vertexShader = _gl!.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(vertexShader, vertexShaderSource);
        _gl.CompileShader(vertexShader);
        CheckShaderCompileErrors(vertexShader, "VERTEX");
        
        uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
        _gl.ShaderSource(fragmentShader, fragmentShaderSource);
        _gl.CompileShader(fragmentShader);
        CheckShaderCompileErrors(fragmentShader, "FRAGMENT");
        
        _shaderProgram = _gl.CreateProgram();
        _gl.AttachShader(_shaderProgram, vertexShader);
        _gl.AttachShader(_shaderProgram, fragmentShader);
        _gl.LinkProgram(_shaderProgram);
        CheckProgramLinkErrors(_shaderProgram);
        
        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);
    }
    
    private void CheckShaderCompileErrors(uint shader, string type)
    {
        _gl!.GetShader(shader, ShaderParameterName.CompileStatus, out int success);
        if (success == 0)
        {
            string infoLog = _gl.GetShaderInfoLog(shader);
            Console.WriteLine($"ERROR::SHADER::{type}::COMPILATION_FAILED\n{infoLog}");
        }
    }
    
    private void CheckProgramLinkErrors(uint program)
    {
        _gl!.GetProgram(program, ProgramPropertyARB.LinkStatus, out int success);
        if (success == 0)
        {
            string infoLog = _gl.GetProgramInfoLog(program);
            Console.WriteLine($"ERROR::PROGRAM::LINKING_FAILED\n{infoLog}");
        }
    }
    
    private unsafe void CreateBuffers()
    {
        Console.WriteLine($"Creating buffers with {_navmeshVertices.Length} vertices and {_navmeshIndices.Length} indices");
        
        // Debug: Print all vertex and index data for simple triangle
        Console.WriteLine($"Vertex data: [{string.Join(", ", _navmeshVertices.Select(f => f.ToString("F2")))}]");
        Console.WriteLine($"Index data: [{string.Join(", ", _navmeshIndices)}]");
        
        _vao = _gl!.GenVertexArray();
        _vbo = _gl.GenBuffer();
        _ebo = _gl.GenBuffer();
        
        Console.WriteLine($"Generated OpenGL objects: VAO={_vao}, VBO={_vbo}, EBO={_ebo}");
        CheckGLError("After generating buffers");
        
        _gl.BindVertexArray(_vao);
        CheckGLError("After binding VAO");
        
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        CheckGLError("After binding VBO");
        
        unsafe
        {
            fixed (void* v = &_navmeshVertices[0])
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(_navmeshVertices.Length * sizeof(float)), v, BufferUsageARB.StaticDraw);
            }
        }
        CheckGLError("After uploading vertex data");
        Console.WriteLine($"Uploaded {_navmeshVertices.Length * sizeof(float)} bytes of vertex data");
        
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        CheckGLError("After binding EBO");
        
        unsafe
        {
            fixed (void* i = &_navmeshIndices[0])
            {
                _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(_navmeshIndices.Length * sizeof(uint)), i, BufferUsageARB.StaticDraw);
            }
        }
        CheckGLError("After uploading index data");
        Console.WriteLine($"Uploaded {_navmeshIndices.Length * sizeof(uint)} bytes of index data");
        
        // Set up vertex attributes with proper stride and offset
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
        CheckGLError("After setting vertex attribute pointer");
        
        _gl.EnableVertexAttribArray(0);
        CheckGLError("After enabling vertex attribute array");
        Console.WriteLine("Set up vertex attribute pointer for position (location 0)");
        
        // Keep VAO bound, only unbind array buffer - EBO stays bound to VAO
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        CheckGLError("After unbinding array buffer");
        
        // Don't unbind VAO here - keep it bound or rebind during rendering
        // _gl.BindVertexArray(0);
        
        Console.WriteLine("Buffers created successfully");
    }
    
    private void CheckGLError(string context)
    {
        var error = _gl!.GetError();
        if (error != GLEnum.NoError)
        {
            Console.WriteLine($"OpenGL error {context}: {error}");
        }
    }
    
    private void OnUpdate(double deltaTime)
    {
        // Keep camera static for now to debug
        // _cameraPos stays at (0, 3, 5) looking at (0, 0, 0)
        
        // Handle input for exit
        if (_inputContext!.Keyboards.Count > 0)
        {
            var keyboard = _inputContext.Keyboards[0];
            if (keyboard.IsKeyPressed(Key.Escape))
            {
                _window!.Close();
            }
        }
    }
    
    private unsafe void OnRender(double deltaTime)
    {
        if (!_debugPrinted)
        {
            Console.WriteLine($"=== FIRST RENDER FRAME ===");
            Console.WriteLine($"Camera pos: ({_cameraPos.X:F2}, {_cameraPos.Y:F2}, {_cameraPos.Z:F2}), Target: ({_cameraTarget.X:F2}, {_cameraTarget.Y:F2}, {_cameraTarget.Z:F2})");
            Console.WriteLine($"Window size: {_window!.Size.X}x{_window.Size.Y}");
            Console.WriteLine($"VAO: {_vao}, VBO: {_vbo}, EBO: {_ebo}, Shader: {_shaderProgram}");
            Console.WriteLine($"Indices to render: {_navmeshIndices.Length}");
            Console.WriteLine($"Vertices array length: {_navmeshVertices.Length}");
            
            // Debug all vertex data for the simple triangle
            Console.WriteLine($"All vertices: [{string.Join(", ", _navmeshVertices.Select(f => f.ToString("F2")))}]");
            Console.WriteLine($"All indices: [{string.Join(", ", _navmeshIndices)}]");
        }

        _gl!.ClearColor(0.1f, 0.1f, 0.2f, 1.0f); // Dark blue background
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _gl.UseProgram(_shaderProgram);

        // Set up matrices
        var model = Matrix4x4.Identity;
        var view = Matrix4x4.CreateLookAt(_cameraPos, _cameraTarget, _cameraUp);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 4,  // 45 degrees
            (float)_window!.Size.X / _window.Size.Y,
            0.1f,   // Near plane
            100.0f  // Far plane
        );

        if (!_debugPrinted)
        {
            Console.WriteLine($"Field of view: {MathF.PI / 4 * 180 / MathF.PI:F1} degrees");
            Console.WriteLine($"Aspect ratio: {(float)_window.Size.X / _window.Size.Y:F2}");
            Console.WriteLine($"Near plane: 0.1, Far plane: 100.0");
        }

        // Set uniforms
        int modelLoc = _gl.GetUniformLocation(_shaderProgram, "uModel");
        int viewLoc = _gl.GetUniformLocation(_shaderProgram, "uView");
        int projLoc = _gl.GetUniformLocation(_shaderProgram, "uProjection");

        // Check if uniforms were found
        if (modelLoc == -1) Console.WriteLine("WARNING: uModel uniform not found");
        if (viewLoc == -1) Console.WriteLine("WARNING: uView uniform not found");
        if (projLoc == -1) Console.WriteLine("WARNING: uProjection uniform not found");

        if (!_debugPrinted)
        {
            Console.WriteLine($"Uniform locations - Model: {modelLoc}, View: {viewLoc}, Projection: {projLoc}");
        }

        SetMatrix4Uniform(modelLoc, model);
        SetMatrix4Uniform(viewLoc, view);
        SetMatrix4Uniform(projLoc, projection);

        // Debug: manually calculate where the triangle vertices should end up
        if (!_debugPrinted)
        {
            var mvp = view * projection;  // Fixed matrix order for row-major matrices
            for (int i = 0; i < _navmeshVertices.Length; i += 3)
            {
                var worldPos = new Vector4(_navmeshVertices[i], _navmeshVertices[i+1], _navmeshVertices[i+2], 1.0f);
                var clipPos = Vector4.Transform(worldPos, mvp);
                if (clipPos.W != 0)
                {
                    var ndcPos = clipPos / clipPos.W;
                    Console.WriteLine($"Vertex {i/3}: World({worldPos.X:F2}, {worldPos.Y:F2}, {worldPos.Z:F2}) -> NDC({ndcPos.X:F2}, {ndcPos.Y:F2}, {ndcPos.Z:F2})");
                }
                else
                {
                    Console.WriteLine($"Vertex {i/3}: ERROR - W component is 0!");
                }
            }
        }

        // Render navmesh
        _gl.BindVertexArray(_vao);
        CheckGLError("After binding VAO for rendering");
        
        // Check VAO binding
        _gl.GetInteger(GetPName.VertexArrayBinding, out int currentVAO);
        if (!_debugPrinted)
        {
            Console.WriteLine($"Current VAO binding: {currentVAO}, Expected: {_vao}");
        }
        
        // Draw the triangles - offset should be 0 for start of buffer
        _gl.DrawElements(PrimitiveType.Triangles, (uint)_navmeshIndices.Length, DrawElementsType.UnsignedInt, (void*)0);
        CheckGLError("After DrawElements call");
        
        if (!_debugPrinted)
        {
            Console.WriteLine($"DrawElements called with {_navmeshIndices.Length} indices");
            Console.WriteLine($"=== END FIRST RENDER FRAME ===");
            _debugPrinted = true;
        }
    }
    
    private unsafe void SetMatrix4Uniform(int location, Matrix4x4 matrix)
    {
        if (location == -1) return; // Skip if uniform location not found
        
        // Convert Matrix4x4 to float array in row-major order
        float[] matrixArray = new float[16]
        {
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44
        };
        
        if (!_debugPrinted && location != -1)
        {
            string matrixType = location == _gl!.GetUniformLocation(_shaderProgram, "uModel") ? "Model" :
                               location == _gl.GetUniformLocation(_shaderProgram, "uView") ? "View" :
                               location == _gl.GetUniformLocation(_shaderProgram, "uProjection") ? "Projection" : "Unknown";
            Console.WriteLine($"{matrixType} matrix first row: [{matrixArray[0]:F2}, {matrixArray[1]:F2}, {matrixArray[2]:F2}, {matrixArray[3]:F2}]");
            Console.WriteLine($"{matrixType} matrix last row:  [{matrixArray[12]:F2}, {matrixArray[13]:F2}, {matrixArray[14]:F2}, {matrixArray[15]:F2}]");
        }
        
        fixed (float* ptr = matrixArray)
        {
            // Use transpose=true to convert from row-major (.NET) to column-major (OpenGL)
            _gl!.UniformMatrix4(location, 1, true, ptr);
        }
    }
    
    private void OnResize(Silk.NET.Maths.Vector2D<int> size)
    {
        _gl?.Viewport(0, 0, (uint)size.X, (uint)size.Y);
    }
    
    private void OnClose()
    {
        _gl?.DeleteVertexArray(_vao);
        _gl?.DeleteBuffer(_vbo);
        _gl?.DeleteBuffer(_ebo);
        _gl?.DeleteProgram(_shaderProgram);
        
        Console.WriteLine("Navmesh Visualizer closed");
    }
}