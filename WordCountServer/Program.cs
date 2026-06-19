using System;
using System.Threading.Tasks;

namespace WordCountServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //folderi za primere i logove
            string rootFolder = @"D:\faks\TRECA GODINA\sistemsko programiranje\Projekat\WordCountFiles";
            string logFolder = @"D:\faks\TRECA GODINA\sistemsko programiranje\Projekat\Logs";

            Logger.Init(logFolder);
            Logger.Info("Aplikacija pokrenuta.");

            Server server = new Server("http://localhost:5050/", rootFolder);

            Task serverTask = server.Start();

            Console.ReadLine(); //shutdown kad se pritisne ENTER

            await server.Stop();

            await serverTask;

            Logger.Info("Aplikacija zaustavljena.");
        }
    }
}