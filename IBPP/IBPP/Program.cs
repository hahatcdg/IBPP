using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Excel = Microsoft.Office.Interop.Excel;

namespace IBPP
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            SequenceAnalyzer sa = new SequenceAnalyzer();
            Console.WriteLine("\n********Welcome to use IBPP********\n\n");
            while (true)
            {
                sa.Run();
                Console.WriteLine("The prediction is over, please find the result in \"Result.txt\"\nPress any key to continue.\n");
                Console.ReadKey();
            }

        }
    }
}
