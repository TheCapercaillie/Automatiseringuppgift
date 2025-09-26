using System;
using OTSystem.OTSystem;

namespace OTSystem
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IndustrialControlSystem industrialControlSystem = new IndustrialControlSystem();
            industrialControlSystem.Run();
        }
    }
}
