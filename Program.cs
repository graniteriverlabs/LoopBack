using GRL.Logging;
using GRL.VDPWR.LoopBackService.Models;
using GrlC2ApiLib;
using Serilog;
using System;
using System.Collections.Generic;
namespace LoopBack
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            ConfigureLogging();
            using var loopBackService = new GrlC2LoopBackService(new LoopBack.Services.SerilogLoggerService());
            var cli = new LoopBack.Cli.LoopBackCli(loopBackService);
            int code = await cli.RunAsync();
            Log.CloseAndFlush();
            return code;
        }

        private static void ConfigureLogging()
        {
            if (Log.Logger == Serilog.Core.Logger.None)
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .CreateLogger();
            }
        }
    }
}
