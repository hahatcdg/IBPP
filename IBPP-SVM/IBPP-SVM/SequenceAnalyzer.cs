using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using LibSVMsharp.Helpers;
using LibSVMsharp.Extensions;
using LibSVMsharp.Core;
using LibSVMsharp;

namespace IBPP_SVM
{
    public struct SeqData { public double Score; public double Position; public string Seq; }

    class SequenceAnalyzer
    {

        string DNASequence;
        FirstScoringClass Program1;
        FileCordinator files; PatternScorer PScorer; SvmHelper newSVMHelper; 
        int rangeLength; public int RangeLength { get { return this.rangeLength; } set { this.rangeLength = value; } }
        int overlap; public int OverLap { get { return this.overlap; } private set { this.overlap = value; } }
        List<int[]> ranges; public List<int[]> Ranges { get { return this.ranges; } set { this.ranges = value; } }
        List<string>Patterns; List<double[]> Scores; List<SeqData> Sequences;
        double[] MeanScores;
        public SequenceAnalyzer()
        {
            this.overlap = 80; this.rangeLength = 90; this.files = new FileCordinator();
            this.Sequences = new List<SeqData>(); this.Patterns = new List<string>();
        }
        public SequenceAnalyzer(FirstScoringClass program1) : this()
        { this.Program1 = program1; }
        void Initiate()
        {
            this.PScorer = new PatternScorer();
            this.newSVMHelper = new SvmHelper(this.files); this.newSVMHelper.Initiate();
        }

        public void ReadSequence()
        {
            this.Sequences.Clear();
            StreamReader seqReader = new StreamReader(this.files.InputSequenceFile);
            string line = seqReader.ReadLine();
            while (line != null)
            {
                string[] lineSp = line.Split('\t');
                this.Sequences.Add(new SeqData() { Score = Convert.ToDouble(lineSp[0]),Position=Convert.ToDouble(lineSp[1]),Seq=lineSp[2] });
                line = seqReader.ReadLine();
            }
            seqReader.Close();
        }

        void ReadPatterns()
        {
            StreamReader patternRreader = new StreamReader(this.files.PatternFile);
            this.Patterns.Clear();
            string line = patternRreader.ReadLine();int count = 0;
            while (line != null)
            {
                try
                {
                    if (line.Length > 0&&count>0)
                    { this.Patterns.Add(line.Trim(' ').ToLower()); }
                }
                catch { }
                count++;
                line = patternRreader.ReadLine();
            }
            patternRreader.Close();
            this.PScorer.Initiate(this.Patterns);
        }

        bool IsAnalyzing;
        void ScoreShortSeqs()
        {
            this.Scores = new List<double[]>();
            this.IsAnalyzing = true;
            Console.WriteLine("Start analyzing.");          
            Task.Factory.StartNew(()=> {
                while (this.IsAnalyzing)
                {
                    System.Threading.Thread.Sleep(1000);
                    if (this.IsAnalyzing)
                    { Console.Write("* "); }
                }
            });
            this.MeanScores = new double[this.Sequences.Count];
            for (int i = 0; i < this.Sequences.Count; i++)
            {
                this.PScorer.InputSequence(this.Sequences[i]);
                this.PScorer.GetScore();
                this.Scores.Add(new double[this.PScorer.Score.Length]);
                this.PScorer.Score.CopyTo(this.Scores[this.Scores.Count - 1], 0);
                this.MeanScores[i] = Average(this.PScorer.Score);
            }
        }

        void ConvetToSVMdata()
        {
            StreamWriter SVMwriter = new StreamWriter(Path.Combine(Environment.CurrentDirectory,"temp", this.files.SVMdataFile));
            foreach (double[] scoreList in this.Scores)
            {
                SVMwriter.Write("1 ");
                for (int i = 1; i <= scoreList.Length; i++)
                {
                    SVMwriter.Write(string.Format("{0}:{1} ", i, scoreList[i - 1]));
                }
                SVMwriter.WriteLine();
           }
            SVMwriter.Close();
        }

        public void SVMTrain()
        {
            try
            {
                this.newSVMHelper = new SvmHelper(this.files);
                Console.WriteLine("Start SVM model training.");
                this.newSVMHelper.Initiate();
                this.newSVMHelper.Train();
                Console.WriteLine();
            }
            catch (Exception e){ Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("Error happened during training.\n{0}",e.ToString());Console.ForegroundColor = ConsoleColor.Green; }
        }

       public void SVMAnalyze()
        {
            this.Initiate();
            this.IsAnalyzing = false;
            this.newSVMHelper.Predict();
         }

        public void TranslateSVMResult()
        {
            List<double[]> RawList = new List<double[]>();
            using (StreamReader sr = new StreamReader(Path.Combine(Environment.CurrentDirectory,"temp", this.files.SVMresultFile)))
            {
                string line = sr.ReadLine(); int count = 0;
                while (line != null)
                {
                    if (line == "1") { RawList.Add(new double[] { this.MeanScores[count], this.Sequences[count].Position }); }
                    count++;
                    line = sr.ReadLine();
                }
            }
            if (RawList.Count > 1)
            {
                //RawList = this.CondenseList(RawList, 1);
                //RawList = this.CondenseList(RawList, 5);
                //RawList = this.CondenseList(RawList, 50);
                this.CondenseByScore(ref RawList, 1);
                this.CondenseByScore(ref RawList, 5);
                this.CondenseByScore(ref RawList, 50);
            }
            double[][] orderedList = (from i in RawList orderby i[0] descending select i).ToArray();
            using (StreamWriter sw = new StreamWriter(Path.Combine(Environment.CurrentDirectory, "Result.txt")))
            {
                this.DNASequence = Program1.Sequence;
                sw.WriteLine("Position\tTSS\t\tScore");
                foreach (double[] item in orderedList)
                {
                    sw.WriteLine("{0}\t{1}\t{2:0.0}", item[1],this.DNASequence.Substring((int)item[1]-10,10)+this.DNASequence.Substring((int)item[1],1).ToUpper()+this.DNASequence.Substring((int)item[1]+1,5), item[0]); }
            }
        }

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

        List<double[]> OutPutFinalResultAsPositions()
        {
            List<double[]> result = new List<double[]>();
            StreamReader svmResultReader = new StreamReader(this.files.SVMresultFile);
            string line = svmResultReader.ReadLine();int count = 0;
            while (line != null)
            {
                if (line == "1") {
                    result.Add(new double[] {this.Sequences[count].Score, this.Sequences[count].Position });
                }
                count++;
                line = svmResultReader.ReadLine();
            }
            svmResultReader.Close();
            return result;
        }

        public void Run()
        {
            if (!File.Exists(Path.Combine(Environment.CurrentDirectory, "SVM.model")))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nThe SVM model is missing, please use training funciton to generate the model (SVM.model).\n");
                Console.ForegroundColor = ConsoleColor.Green;
                throw new Exception();
            }
                this.Initiate();
                this.ReadSequence();
                this.ReadPatterns();
                this.ScoreShortSeqs();
                this.ConvetToSVMdata();
                this.SVMAnalyze();                
        }

        public void PrepareSvmTrainSet()
        {
            this.Initiate();
            this.Patterns.Clear();
            using (StreamReader sr = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), "Pattern.txt")))
            { string line = sr.ReadLine();
                while (line != null)
                { this.Patterns.Add(line.Trim(' ').ToLower()); line = sr.ReadLine(); } }
            this.PScorer.Initiate(this.Patterns);
            this.newSVMHelper.Initiate();
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(),"TrainSet", "P.txt")) || !File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "TrainSet", "Ref.txt")))
                {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Please make sure the promoter training set (P.txt) and non-promoter training set (Ref.txt) are present in the directory \"TrainSet\"\n");
                Console.ForegroundColor = ConsoleColor.Green;
                throw new Exception();
                }
            List<string> Promoters = new List<string>();
            List<string> References = new List<string>();
            using (StreamWriter sw = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(),"temp", "TrainSet.txt")))
            {
                using (StreamReader sr = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), "TrainSet", "P.txt")))
                {
                    string line = sr.ReadLine();
                    while (line != null) {
                        string cleanLine = line.ToLower().Trim(' ');
                        if (FirstScoringClass.IsDNA(cleanLine)) { Promoters.Add(cleanLine); } line = sr.ReadLine();
                    }
                }
                using (StreamReader sr = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), "TrainSet", "Ref.txt")))
                {
                    string line = sr.ReadLine();
                    while (line != null) {
                        string cleanLine = line.ToLower().Trim(' ');
                        if (FirstScoringClass.IsDNA(cleanLine))
                        { References.Add(cleanLine); }
                        line = sr.ReadLine(); }
                }
                if (Promoters.Count < 1 || References.Count < 1) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Please check the Training sequences.\n");
                    Console.ForegroundColor = ConsoleColor.Green;
                    System.Threading.Thread.Sleep(500);
                    throw new Exception(); }
                Console.WriteLine("Scoring the promoter and non-promoter sequences.\n");
                int count = 0;
                foreach (string seq in Promoters)
                {
                    count++;
                    if (count % 1000 == 0) { Console.Write("* "); }
                    this.PScorer.InputSequence(new SeqData() { Score = 0, Position = 0, Seq = seq });
                    this.PScorer.GetScore();
                    string output = "";
                    output += "1 ";
                    for (int i = 2; i <= this.PScorer.Score.Length; i++)
                    { output += string.Format("{0}:{1} ", i-1,this.PScorer.Score[i - 1]); }
                    sw.WriteLine(output);
                }

                foreach (string seq in References)
                {
                    count++;
                    if (count % 1000 == 0) { Console.Write("* "); }
                    this.PScorer.InputSequence(new SeqData() {Score=0,Position=0,Seq=seq });
                    this.PScorer.GetScore();
                    string output = "";
                    output += "-1 ";
                    for (int i = 2; i <= this.PScorer.Score.Length; i++)
                    { output += string.Format("{0}:{1} ", i-1, this.PScorer.Score[i - 1]); }
                    sw.WriteLine(output);
                }
                Console.WriteLine();
            }
        }
        static double Average(double[] input)
        {
            double result = 0;
            foreach (double item in input)
            {
                result += item;
            }
            return result / input.Length;
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
    }
}
