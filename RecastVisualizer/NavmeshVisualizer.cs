using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Input;
using System.Numerics;

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
    
    private Vector3 _cameraPos = new(0, 5, 10);
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
        
        // Set viewport
        _gl.Viewport(0, 0, (uint)_window!.Size.X, (uint)_window.Size.Y);
        
        // Enable depth testing
        _gl.Enable(EnableCap.DepthTest);
        _gl.Disable(EnableCap.CullFace);  // Disable face culling to see triangles from both sides
        _gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line); // Wireframe mode
        
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
        // Create a simple navmesh manually for demonstration
        // This represents a simple scene with walkable areas
        
        var vertices = new List<float>();
        var indices = new List<uint>();
        uint vertexIndex = 0;
        
        // Ground plane - main walkable area
        AddQuad(vertices, indices, ref vertexIndex,
            new Vector3(-10, 0, -10),
            new Vector3(10, 0, -10),
            new Vector3(10, 0, 10),
            new Vector3(-10, 0, 10));
        
        // Raised platform
        AddQuad(vertices, indices, ref vertexIndex,
            new Vector3(-3, 1, -3),
            new Vector3(3, 1, -3),
            new Vector3(3, 1, 3),
            new Vector3(-3, 1, 3));
        
        // Connecting ramp
        AddQuad(vertices, indices, ref vertexIndex,
            new Vector3(3, 0, -1),
            new Vector3(5, 0, -1),
            new Vector3(5, 1, 1),
            new Vector3(3, 1, 1));
        
        // Additional walkable areas around obstacles
        // Left side corridor
        AddQuad(vertices, indices, ref vertexIndex,
            new Vector3(-8, 0, -2),
            new Vector3(-6, 0, -2),
            new Vector3(-6, 0, 2),
            new Vector3(-8, 0, 2));
        
        // Right side corridor  
        AddQuad(vertices, indices, ref vertexIndex,
            new Vector3(6, 0, -2),
            new Vector3(8, 0, -2),
            new Vector3(8, 0, 2),
            new Vector3(6, 0, 2));
        
        _navmeshVertices = vertices.ToArray();
        _navmeshIndices = indices.ToArray();
        
        Console.WriteLine("Generated simple test navmesh");
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
    
    private void CreateBuffers()
    {
        Console.WriteLine($"Creating buffers with {_navmeshVertices.Length} vertices and {_navmeshIndices.Length} indices");
        
        // Debug: Print some vertex and index data
        if (_navmeshVertices.Length >= 9)
        {
            Console.WriteLine($"Sample vertices: v0=({_navmeshVertices[0]}, {_navmeshVertices[1]}, {_navmeshVertices[2]}) v1=({_navmeshVertices[3]}, {_navmeshVertices[4]}, {_navmeshVertices[5]}) v2=({_navmeshVertices[6]}, {_navmeshVertices[7]}, {_navmeshVertices[8]})");
        }
        if (_navmeshIndices.Length >= 6)
        {
            Console.WriteLine($"Sample indices: triangle0=({_navmeshIndices[0]}, {_navmeshIndices[1]}, {_navmeshIndices[2]}) triangle1=({_navmeshIndices[3]}, {_navmeshIndices[4]}, {_navmeshIndices[5]})");
        }
        
        _vao = _gl!.GenVertexArray();
        _vbo = _gl.GenBuffer();
        _ebo = _gl.GenBuffer();
        
        Console.WriteLine($"Generated OpenGL objects: VAO={_vao}, VBO={_vbo}, EBO={_ebo}");
        
        _gl.BindVertexArray(_vao);
        
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        unsafe
        {
            fixed (void* v = &_navmeshVertices[0])
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(_navmeshVertices.Length * sizeof(float)), v, BufferUsageARB.StaticDraw);
            }
        }
        
        Console.WriteLine($"Uploaded {_navmeshVertices.Length * sizeof(float)} bytes of vertex data");
        
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        unsafe
        {
            fixed (void* i = &_navmeshIndices[0])
            {
                _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(_navmeshIndices.Length * sizeof(uint)), i, BufferUsageARB.StaticDraw);
            }
        }
        
        Console.WriteLine($"Uploaded {_navmeshIndices.Length * sizeof(uint)} bytes of index data");
        
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        _gl.EnableVertexAttribArray(0);
        
        Console.WriteLine("Set up vertex attribute pointer for position (location 0)");
        
        // Check for OpenGL errors
        var error = _gl.GetError();
        if (error != GLEnum.NoError)
        {
            Console.WriteLine($"OpenGL error after buffer creation: {error}");
        }
        else
        {
            Console.WriteLine("Buffers created successfully");
        }
    }
    
    private void OnUpdate(double deltaTime)
    {
        // Simple camera rotation around the navmesh
        float time = (float)_window!.Time;
        float radius = 15.0f;
        _cameraPos = new Vector3(
            (float)(Math.Sin(time * 0.3) * radius),
            8,
            (float)(Math.Cos(time * 0.3) * radius)
        );
        
        // Handle input for exit
        if (_inputContext!.Keyboards.Count > 0)
        {
            var keyboard = _inputContext.Keyboards[0];
            if (keyboard.IsKeyPressed(Key.Escape))
            {
                _window.Close();
            }
        }
    }
    
    private void OnRender(double deltaTime)
    {
        if (!_debugPrinted)
        {
            Console.WriteLine($"=== FIRST RENDER FRAME ===");
            Console.WriteLine($"Camera pos: {_cameraPos}, Target: {_cameraTarget}");
            Console.WriteLine($"Window size: {_window!.Size.X}x{_window.Size.Y}");
            Console.WriteLine($"VAO: {_vao}, VBO: {_vbo}, EBO: {_ebo}, Shader: {_shaderProgram}");
            Console.WriteLine($"Indices to render: {_navmeshIndices.Length}");
            Console.WriteLine($"Vertices array length: {_navmeshVertices.Length}");
            
            // Debug vertex data sample
            if (_navmeshVertices.Length >= 9)
            {
                Console.WriteLine($"First triangle vertices: ({_navmeshVertices[0]}, {_navmeshVertices[1]}, {_navmeshVertices[2]}), ({_navmeshVertices[3]}, {_navmeshVertices[4]}, {_navmeshVertices[5]}), ({_navmeshVertices[6]}, {_navmeshVertices[7]}, {_navmeshVertices[8]})");
            }
            if (_navmeshIndices.Length >= 3)
            {
                Console.WriteLine($"First triangle indices: {_navmeshIndices[0]}, {_navmeshIndices[1]}, {_navmeshIndices[2]}");
            }
        }

        _gl!.ClearColor(0.1f, 0.1f, 0.2f, 1.0f); // Dark blue background
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _gl.UseProgram(_shaderProgram);

        // Set up matrices
        var model = Matrix4x4.Identity;
        var view = Matrix4x4.CreateLookAt(_cameraPos, _cameraTarget, _cameraUp);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 4,
            (float)_window!.Size.X / _window.Size.Y,
            0.1f,
            100.0f
        );

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

        // Render navmesh
        _gl.BindVertexArray(_vao);
        
        // Check VAO binding
        _gl.GetInteger(GetPName.VertexArrayBinding, out int currentVAO);
        if (!_debugPrinted)
        {
            Console.WriteLine($"Current VAO binding: {currentVAO}, Expected: {_vao}");
        }
        
        // Use nint zero for DrawElements offset parameter
        nint offset = 0;
        _gl.DrawElements(PrimitiveType.Triangles, (uint)_navmeshIndices.Length, DrawElementsType.UnsignedInt, in offset);

        // Check for OpenGL errors during rendering
        var error = _gl.GetError();
        if (error != GLEnum.NoError)
        {
            Console.WriteLine($"OpenGL error during rendering: {error}");
        }
        
        if (!_debugPrinted)
        {
            Console.WriteLine($"=== END FIRST RENDER FRAME ===");
            _debugPrinted = true;
        }
    }
    
    private unsafe void SetMatrix4Uniform(int location, Matrix4x4 matrix)
    {
        if (location == -1) return; // Skip if uniform location not found
        
        // OpenGL expects column-major matrices, but .NET Matrix4x4 is row-major
        // So we need to transpose the matrix for OpenGL
        var transposed = Matrix4x4.Transpose(matrix);
        
        // Convert Matrix4x4 to float array to ensure proper memory layout
        float[] matrixArray = new float[16]
        {
            transposed.M11, transposed.M12, transposed.M13, transposed.M14,
            transposed.M21, transposed.M22, transposed.M23, transposed.M24,
            transposed.M31, transposed.M32, transposed.M33, transposed.M34,
            transposed.M41, transposed.M42, transposed.M43, transposed.M44
        };
        
        if (!_debugPrinted && location != -1)
        {
            string matrixType = location == _gl!.GetUniformLocation(_shaderProgram, "uModel") ? "Model" :
                               location == _gl.GetUniformLocation(_shaderProgram, "uView") ? "View" :
                               location == _gl.GetUniformLocation(_shaderProgram, "uProjection") ? "Projection" : "Unknown";
            Console.WriteLine($"{matrixType} matrix first row: [{matrixArray[0]:F2}, {matrixArray[1]:F2}, {matrixArray[2]:F2}, {matrixArray[3]:F2}]");
        }
        
        fixed (float* ptr = matrixArray)
        {
            _gl!.UniformMatrix4(location, 1, false, ptr);
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