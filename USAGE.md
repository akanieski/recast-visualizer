# Usage Examples

## Basic Usage

To run the navmesh visualizer:

```bash
# Clone the repository
git clone https://github.com/akanieski/recast-visualizer.git
cd recast-visualizer

# Build and run
cd RecastVisualizer
dotnet build
dotnet run
```

## Expected Output

When you run the application, you should see:

```
Recast Navmesh Visualizer
Generated simple test navmesh
Navmesh Visualizer loaded successfully!
Generated navmesh with 20 vertices and 10 triangles
Controls: The camera will automatically rotate around the navmesh
Close the window to exit
```

A window will open showing a 3D wireframe view of the navmesh with:
- Green wireframe lines representing walkable surfaces
- Automatic camera rotation around the scene
- Multiple connected walkable areas including ramps and platforms

## Controls

- **ESC Key**: Exit the application
- **Close Window**: Exit the application
- **Camera**: Automatically rotates (no manual control in this version)

## Troubleshooting

### Display Issues
If you get GLFW errors about display initialization, you're likely running in a headless environment (like a server or CI system). The application requires a graphics display to run.

### Missing Dependencies
If you get errors about missing packages, run:
```bash
dotnet restore
```

### Build Errors
Make sure you have .NET 8.0 SDK installed:
```bash
dotnet --version
```