using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 启动子预测_整合版
{
    public static class StringExtension
    {
        public static bool IsNumber(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) { return false; }
            bool result = true;
            foreach (char item in input)
            {
                if (!char.IsNumber(item)) { result = false; }
            }
            return result;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            SeqPicker Program1 = new SeqPicker("P.txt", "Ref.txt");
            Console.WriteLine("************************************************************************\n");
            Console.WriteLine("How many images do you want to generate？\n(input a number)");
            Console.ForegroundColor = ConsoleColor.Red;
            string ans = "";
            while (!ans.IsNumber())
            { ans = Console.ReadLine(); }
            Console.ForegroundColor = ConsoleColor.Green;

            Program1.CleanDir(); Program1.CleanDir();
            Exception ex= SeqPicker.CheckEnvironment();
            if (ex == null)
            {
                for (int i = 0; i < int.Parse(ans); i++)
                { Program1.Run(); }
            }
            else
            { Console.WriteLine("\n{0}\n",ex.Message); }
            EvolutionEnvironment Program2 = new EvolutionEnvironment(); Program2.Run();
            Program1.ReadPatterns();
            Console.WriteLine("\nThe images has been generated, please find in Pattern.txt\n\nPress any key to quit.\n");
            Console.ReadKey();
        }
    }
}
