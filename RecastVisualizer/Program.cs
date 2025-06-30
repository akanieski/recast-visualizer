using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Input;
using System.Numerics;
using DotRecast.Core.Numerics;
using DotRecast.Recast;
using DotRecast.Detour;

namespace RecastVisualizer;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Recast Navmesh Visualizer");
        
        var app = new NavmeshVisualizer();
        app.Run();
    }
}
