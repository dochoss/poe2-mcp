using BenchmarkDotNet.Running;
using Poe2Mcp.Benchmarks.Calculators;

namespace Poe2Mcp.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
