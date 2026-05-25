using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordCountServer
{
    class Program
    {
        static void Main(string[] args)
        {
            //folderi za primere i logove
            //string rootFolder = @"D:\faks\TRECA GODINA\sistemsko programiranje\Projekat\WordCountFiles"; 
            //string logFolder = @"D:\faks\TRECA GODINA\sistemsko programiranje\Projekat\Logs";
            string rootFolder = @"C:\Users\Jetko\Desktop\FAKS\III godina\Sistemsko\SistemskoProgramiranje\WordCountFiles";
            string logFolder = @"C:\Users\Jetko\Desktop\FAKS\III godina\Sistemsko\SistemskoProgramiranje\Logs";

            Logger.Init(logFolder);
            Logger.Info("Aplikacija pokrenuta.");

            Server server = new Server("http://localhost:5050/", rootFolder);
            server.Start();

            Console.ReadLine();
            server.Stop();
            Logger.Info("Aplikacija zaustavljena.");
        }
    }
}
