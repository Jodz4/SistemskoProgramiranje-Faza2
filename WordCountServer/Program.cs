using System;
using System.Threading.Tasks;

namespace WordCountServer
{
    class Program
    {
        // async Task Main omogucava await unutar Main metode
        static async Task Main(string[] args)
        {
            //folderi za primere i logove
            string rootFolder = @"D:\faks\TRECA GODINA\sistemsko programiranje\Projekat\WordCountFiles";
            string logFolder = @"D:\faks\TRECA GODINA\sistemsko programiranje\Projekat\Logs";

            Logger.Init(logFolder);
            Logger.Info("Aplikacija pokrenuta.");

            Server server = new Server("http://localhost:5050/", rootFolder);

            // Start je sada async Task - koristimo await
            Task serverTask = server.Start();

            Console.ReadLine(); //shutdown kad se pritisne ENTER

            // Stop je sada async Task - koristimo await
            await server.Stop();

            // cekamo da serverTask zavrsi (izadje iz while petlje)
            await serverTask;

            Logger.Info("Aplikacija zaustavljena.");
        }
    }
}