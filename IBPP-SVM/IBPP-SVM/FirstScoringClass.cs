using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IBPP_SVM
{
    class FirstScoringClass
    {
        public FileCordinator fileCordinator;
        public List<string> PatternCollection;
        string Pattern;
        string InputSeq;
        public string Sequence { get { return this.InputSeq; } }
        List<string> SoftPatterns;
        double Softness, Threshold;
        public void Initiate()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            this.SoftPatterns = new List<string>(); this.Threshold = 5; this.Softness = 0.2;//关键参数
            //读取全部pattern
            using (StreamReader sr = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), fileCordinator.PatternFile)))
            {
                string readLine = sr.ReadLine();this.PatternCollection = new List<string>();
                while (readLine!=null)
                {
                    this.PatternCollection.Add(readLine.Trim(' ').ToLower());
                    readLine = sr.ReadLine();
                }                
            }

        }

        public FirstScoringClass(FileCordinator fc)
        {
            this.fileCordinator = fc;this.PatternCollection = new List<string>();
        }
        public void AnalyzeSequence()
        {
       
            this.CreatePattern();
            //Input Sequence
            Repeat1:
            Console.WriteLine("Please make sure the sequence is present in the file of sequence.txt\n" +
                "File is OK?\tY. yes\tN. no");
            string ans = Console.ReadLine();
            switch (ans.ToLower())
            {
                case "y":
                    break;
                case "n":
                    System.Threading.Thread.Sleep(500);Console.WriteLine(); throw new Exception();
                default:
                    goto Repeat1;
            }
            try
            {
                using (StreamReader sr = new StreamReader(Path.Combine(Environment.CurrentDirectory, "sequence.txt")))
                {
                    string line = sr.ReadLine(), seq = "";
                    while (line != null)
                    {
                        if (IsDNA(line.ToLower().Trim(' ')))
                        { seq += line.ToLower().Trim(' '); }
                        line = sr.ReadLine();
                    }
                    this.InputSeq = seq;
                }
                if (this.InputSeq.Length < 1) { throw new Exception(); }
            }
            catch { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("\nSomething is wrong with the input sequence\n");Console.ForegroundColor = ConsoleColor.Green; goto Repeat1; }
            StreamWriter OutWriter = new StreamWriter(Path.Combine(Environment.CurrentDirectory, this.fileCordinator.InputSequenceFile), false);
                List<double[]> tempResult = this.AnalyzeGSeq2(this.InputSeq, this.SoftPatterns);
                int window = this.SoftPatterns[0].Length;
                foreach (double[] item in tempResult)
                { OutWriter.WriteLine("{0:0.00}\t{1:0.00}\t{2}", item[0], item[1], this.InputSeq.Substring((int)item[2], window)); }           
                OutWriter.Close();
        }
        void CreatePattern()
        {
            //重新建立pattern
            this.Pattern = this.PatternCollection[0];
            this.GenerateSoftPatterns();
        }

        List<double[]> AnalyzeGSeq2(string seq, List<string> SoftPattern)
        {
            int window = SoftPattern[0].Length, currentPosition = 0; double curretScore = 0;
            List<double[]> result = new List<double[]>();
            for (int i = 0; i <= seq.Length - window; i++)
            {
                currentPosition = i; curretScore = this.GetScore(seq.Substring(i, window), SoftPattern);
                if (curretScore > this.Threshold)
                { result.Add(new double[] { curretScore, currentPosition+ 60,currentPosition }); }
            }
            return result;
        }

        void GenerateSoftPatterns()
        {
            List<int[]> positions = new List<int[]>();
            bool NT = false; int leftPoint = 0, rightPoint = 0;
            for (int i = 0; i < this.Pattern.Length; i++)
            {
                if (i == 0)
                {
                    if (this.Pattern[i] == '-') { NT = false; leftPoint = 0; }
                    else { NT = true; }
                }
                else
                {
                    if (NT && this.Pattern[i] == '-') { leftPoint = i; NT = false; }
                    else if (!NT && this.Pattern[i] != '-') { rightPoint = i - 1; positions.Add(new int[] { leftPoint, rightPoint }); NT = true; }
                }
                if ((i == this.Pattern.Length - 1) && !NT)
                { rightPoint = this.Pattern.Length - 1; positions.Add(new int[] { leftPoint, rightPoint }); }
            }
            List<string> result = new List<string>(); int[] addGaps = new int[positions.Count];
            int gapID = -1, maxGapLength = -1;
            for (int i = 0; i < positions.Count; i++)
            {
                if (positions[i][0] > 0 && positions[i][1] < this.Pattern.Length - 1)
                {
                    int curLength = positions[i][1] - positions[i][0] + 1;
                    if (curLength > maxGapLength)
                    { gapID = i; maxGapLength = curLength; }
                }
            }
            if (gapID >= 0)
            {
                for (int i = 0; i <= maxGapLength * this.Softness; i++)
                {
                    if (i != 0)
                    { result.Add(this.Pattern.Substring(0, positions[gapID][0]) + Gaps(maxGapLength - i) + this.Pattern.Substring(positions[gapID][1] + 1, this.Pattern.Length - positions[gapID][1] - 1)); }
                    result.Add(this.Pattern.Substring(0, positions[gapID][0]) + Gaps(maxGapLength + i) + this.Pattern.Substring(positions[gapID][1] + 1, this.Pattern.Length - positions[gapID][1] - 1));
                }
            }
            this.SoftPatterns = result;
        }

        double GetScore(string Seq, List<string> SoftPattern)
        {
            double maxScore = 0; string inputSeq;
            if (this.InputSeq.Length < this.Pattern.Length + 10)
            { inputSeq = "--" + Seq + "--"; }
            else { inputSeq = Seq; }
            for (int i = 0; i < SoftPattern.Count; i++)
            {
                maxScore = PatternScorer.Align(inputSeq, SoftPattern[i], maxScore);
            }
            return maxScore;
        }

        public void Run()
        {
            this.Initiate();
            this.AnalyzeSequence();
        }

        //Tools
        public List<double[]> CondenseList(List<double[]> input)
        {
            bool FoundStrand = false; 
            int[] range = new int[2];
            List<double[]> result = new List<double[]>();
            for(int i = 0;i< input.Count;i++)
            {
                if (FoundStrand)
                {
                    if (input[i][1] - input[i - 1][1] != 1)
                    { FoundStrand = false;result.Add(new double[] {input[(range[1]+range[0])/2][0],input[(range[1]+range[0])/2][1] }); }
                    else
                    {
                        range[1] = i;
                    }
                }
                else
                {
                    FoundStrand = true;
                    range[0] = i;
                }
            }
            return result;
        }
   
        static internal bool IsDNA(string line)
        {
            bool result = true;
            if (line.Length == 0) { result = false; }
            else
            {
                for (int i = 0; i < line.Length; i++)
                {
                    switch (line[i])
                    {
                        case 'a':
                        case 't':
                        case 'c':
                        case 'g':
                            break;
                        default:
                            result = false; break;
                    }
                }
            }
            return result;
        }
        string Gaps(int n)
        {
            string result = "";
            for (int i = 0; i < n; i++)
            { result += '-'; }
            return result;
        }
    }

}
