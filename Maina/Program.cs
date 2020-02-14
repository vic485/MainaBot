using System;
using System.Threading.Tasks;

namespace Maina
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            
            // Keep bot alive
            await Task.Delay(-1);
        }
    }
}