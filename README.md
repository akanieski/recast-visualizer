# Recast Navmesh Visualizer

A simple C# console application that uses DotRecast and Silk.NET to render a 3D navmesh visualization.

## Features

- **3D Navmesh Rendering**: Visualizes navmesh geometry using OpenGL through Silk.NET
- **Automatic Camera Rotation**: The camera automatically rotates around the navmesh for better viewing
- **Wireframe Display**: Shows the navmesh structure in wireframe mode for clear visualization
- **Simple Test Scene**: Includes a basic test scene with multiple walkable areas

## Dependencies

- **DotRecast**: For navmesh generation capabilities
  - DotRecast.Core
  - DotRecast.Recast
  - DotRecast.Detour
- **Silk.NET**: For cross-platform 3D rendering and windowing

## Running the Application

```bash
cd RecastVisualizer
dotnet run
```

## Controls

- **ESC**: Exit the application
- **Mouse**: The camera automatically rotates - no manual controls needed for this demo

## Scene Description

The visualizer displays a simple navmesh consisting of:

1. **Main Ground Plane**: Large walkable area (-10 to 10 units)
2. **Raised Platform**: Elevated walkable area (1 unit high)
3. **Connecting Ramp**: Links the ground to the platform  
4. **Side Corridors**: Additional walkable paths on left and right sides

All areas are rendered in wireframe mode with green color to clearly show the navmesh structure.

## Technical Details

- **Rendering**: OpenGL 3.3 Core Profile
- **Shading**: Simple vertex/fragment shaders with MVP matrix transformations
- **Geometry**: Triangle-based mesh representation
- **Camera**: Orbiting camera with fixed target at origin

## Future Enhancements

This is a basic demonstration. Potential improvements include:

- **DotRecast Integration**: Full integration with DotRecast navmesh generation from 3D scenes. The current version includes DotRecast packages as dependencies but uses a manually created navmesh for demonstration. The DotRecast API requires further research to implement proper integration.
- Support for loading external geometry files (OBJ, FBX, etc.)
- Interactive camera controls (WASD movement, mouse look)
- Multiple rendering modes (solid, wireframe, points)
- Navmesh debugging information display
- Path visualization and pathfinding demonstrations
- Agent simulation and movement
- Real-time navmesh regeneration

## Building from Source

Requires .NET 8.0 or later:

```bash
dotnet build
```

The project uses unsafe code blocks for OpenGL buffer operations, which is enabled in the project configuration.
