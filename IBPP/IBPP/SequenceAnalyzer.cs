using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IBPP
{
    public class SequenceAnalyzer
    {
        string Pattern;
        List<string> SoftPatterns;
        double Softness, Threshold,Penalty;
        List<string> UsedSeqs;
        void Initiate()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            this.SoftPatterns = new List<string>(); this.Threshold = 5; this.Softness = 0.2;//关键参数
            this.UsedSeqs = new List<string>();this.Penalty = 0.4;
        }
        string InputSeq;

        public SequenceAnalyzer()
        {
        }
        void AnalyzeInput()
        {
            //Input Sequence
            Repeat1:
            Console.WriteLine("Please make sure the sequence is present in the file of sequence.txt\n" +
                "File is Ready.\tY. yes\tN. no");
            string ans = Console.ReadLine();
            switch (ans.ToLower())
            {
                case "y":
                    break;
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
            catch { Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nSomething is wrong with the input sequence, please check the file sequence.txt\n");
                Console.ForegroundColor = ConsoleColor.Green;
                goto Repeat1; }
            //Analyzing
            List<double[]> tempResult = this.AnalyzeGSeq2(this.InputSeq, this.SoftPatterns);
            using (StreamWriter sw = new StreamWriter(Path.Combine(Environment.CurrentDirectory, "Result.txt")))
            {
                sw.WriteLine("Position\tTSS\t\tScore");
                double[][] orderedResult = (from i in tempResult orderby i[0] descending select i).ToArray();
                foreach (double[] result in orderedResult)
                {
                    string targetSeq = this.InputSeq.Substring((int)result[1] - 10, 10).ToLower() + this.InputSeq.Substring((int)result[1], 1).ToUpper() + this.InputSeq.Substring((int)result[1] + 1, 5).ToLower();
                    sw.WriteLine("{0}\t{1}\t{2:0.0}",result[1]+1,targetSeq,result[0]);
                }
            }      
        }
        void CreatePattern()
        {
            Repeat1:
            this.Pattern = "";
            //重新建立pattern
            try
            {
                using (StreamReader sr = new StreamReader(Path.Combine(Environment.CurrentDirectory, "Pattern.txt")))
                {
                    string line = sr.ReadLine();
                    while (!IsPattern(line)&&line!=null)
                    {
                        line = sr.ReadLine();
                    }
                    this.Pattern = line;
                }
                if (!IsPattern(this.Pattern)) { throw new Exception("\nThe pattern.txt file is not OK, please make sure an image is present in that file.\n" +
                    "Press any key to continue. \n"); }
                this.GenerateSoftPatterns();
            }
            catch(Exception e) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine(e.Message);Console.ForegroundColor = ConsoleColor.Green; Console.ReadKey(); goto Repeat1; }            
        }
        List<double[]> AnalyzeGSeq2(string seq, List<string> SoftPattern)
        {
            int window = this.Pattern.Length, currentPosition = 0; double curretScore = 0;
            List<double[]> result = new List<double[]>();
            for (int i = 0; i <= seq.Length - window; i++)
            {
                currentPosition = i; curretScore = this.GetScore(seq.Substring(i, window), SoftPattern);
                if (curretScore > this.Threshold)
                { result.Add(new double[] { curretScore, currentPosition+61 }); }
            } 
            if (result.Count > 1)
            {
                this.CondenseByScore(ref result, 1);
                this.CondenseByScore(ref result, 5); this.CondenseByScore(ref result, 50);
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

            this.SoftPatterns.Clear();
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

            double maxScore = 0;string inputSeq;
            if (this.InputSeq.Length < this.Pattern.Length + 10)
            { inputSeq = "--" + Seq + "--"; }
            else { inputSeq = Seq; }
            for (int i = 0; i < SoftPattern.Count; i++)
            {
                maxScore = this.Align(inputSeq, SoftPattern[i], maxScore);
            }
            return maxScore;
        }
        public void Run()
        {
                this.Initiate();
            this.CreatePattern();
                this.AnalyzeInput();        
        }

        //Tools
        void CondenseByScore(ref List<double[]> input, int level)
        {

            bool FoundStrand = false;
            int[] range = new int[2];
            List<double[]> result = new List<double[]>();
            for (int i = 0; i < input.Count; i++)
            {
                if (FoundStrand)
                {
                    if (Math.Abs(input[i][1] - input[i - 1][1]) > level)
                    {
                        double MaxPosition = this.GetPositionMax(input, range);

                        result.Add(new double[] { input[(int)MaxPosition][0], input[(int)MaxPosition][1] });
                        range[0] = i; range[1] = i;
                    }
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

            double Position = this.GetPositionMax(input, range);
            result.Add(new double[] { input[(int)Position][0], input[(int)Position][1] });
            input = result;
        }
        double GetPositionMax(List<double[]> input, int[] _range)
        {
            double maxScore = 0, maxPosition = 0;
            for (int i = _range[0]; i <= _range[1]; i++)
            {

                if (input[i][0] > maxScore) { maxScore = input[i][0]; maxPosition = i; }
            }
            return maxPosition;
        }

        double Align(string query, string pattern, double maxScore)
        {
            double MaxScore = maxScore, currentScore = 0;
            for (int i = 0; i < query.Length - pattern.Length; i++)
            {
                currentScore = 0;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (pattern[j] != '-' && query[j + i] == pattern[j]) {  currentScore++;}
                    else if(pattern[j]!='-') {currentScore -= this.Penalty; }
                }
                if (currentScore > MaxScore) { MaxScore = currentScore; }
            }
            return MaxScore;
        }
        bool DirOK(string dir)
        {
            bool result = true;
            if (!Directory.Exists(dir + "\\OutputFiles"))
            { result = false; }
            else if (!File.Exists(dir + "\\OutputFiles\\传代100.txt"))
            { result = false; }
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
        static internal bool IsPattern(string line)
        {
            if (line == null) { return false; }
            if (line.Length < 5) { return false; }
            bool result = true;
            foreach (char item in line.ToLower().Trim(' '))
            {
                switch (item)
                {
                    case 'a': case't':case 'c': case 'g': case '-':
                        break;
                    default:result = false;break;
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
