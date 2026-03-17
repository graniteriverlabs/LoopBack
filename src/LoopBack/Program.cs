using GrlC2ApiLib;
namespace LoopBack
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.Title = "GRL V-DPWR EPR LoopBack Test Utility";
            using var loopBackService = new GrlC2LoopBackService();
            var cli = new LoopBack.Cli.LoopBackCli(loopBackService);
            return await cli.RunAsync();
        }
    }
}