
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IBPP_SVM
{
        class ScoringSVMUI
        {
            FileCordinator fc;
            FirstScoringClass program1;
            SequenceAnalyzer program2;
            public ScoringSVMUI()
            {
                fc = new FileCordinator();
                program1 = new FirstScoringClass(fc);
                program2 = new SequenceAnalyzer(program1);
            if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "temp")))
            { Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory,"temp")); }
            }
        void Train()
        {
            try
            {
                program2.PrepareSvmTrainSet();
            }
            catch {  return; }
            try
            {
                program2.SVMTrain();

            }
            catch { return; }
            Console.WriteLine("SVM model has been trained.\n");
        }

            void Analyze()
            {
            try
            {
                program1.Run();
                program2.Run();
                program2.TranslateSVMResult();
                Console.WriteLine("\nThe prediction is over, please find the result in \"Result.txt\"\nPress any key to continue.\n");
                Console.ReadKey();
            }
            catch
            { }
            }

            public void Start()
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n*****Welcom to use IBPP-SVM.*****\n");
                while (true)
                {
                    Console.WriteLine("Please choose the function：\n1. Train SVM model\t2. Analyze sequence\n");
                    string ans = Console.ReadLine();
                    switch (ans)
                    {
                        case "1":
                            this.Train();
                            break;
                        case "2":
                            this.Analyze(); break;
                    }
                }
            
        }
    }
}