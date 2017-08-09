using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace 启动子预测_整合版
{
    class EvolutionEnvironment
    {
        public List<Enzyme> AllPolymerases;
        public int Generations, SeedingSize, patternLength, MaxNTnumber, startNTnumber,InitialSeedingSize,CommonSeedingSize;
        Random dice;
        List<double> ScoreList;
        double Softness, mutantRate;
        double cutOff,Penalty;
        int OutFileID,DirID; string OutFilePath,currentDir;bool FirstRound;
        List<string> Sequences,refSeqs;string[] Dirs;
        public EvolutionEnvironment()
        {
            this.CommonSeedingSize = 2000;this.InitialSeedingSize = 8000; this.dice = new Random(); this.AllPolymerases = new List<Enzyme>();
            this.Softness = 0.2; this.ScoreList = new List<double>(); this.cutOff = 0.1; this.MaxNTnumber = 20;
            this.startNTnumber = 10; this.mutantRate = 0.2; this.patternLength = 20;
            this.Penalty = 0.4;
        }
        void FindCurrentDir()
        {
            this.SeedingSize = this.InitialSeedingSize;
            if (FirstRound)
            {
                this.Dirs = (from dir in Directory.GetDirectories(Environment.CurrentDirectory)
                             where new DirectoryInfo(dir).Name.ToLower().StartsWith("output")
                             orderby this.DirNumber(new DirectoryInfo(dir).Name) ascending
                             select dir).ToArray();
            }
            this.currentDir = Dirs[this.DirID];
            if (!Directory.Exists(Path.Combine(Dirs[this.DirID], "OutputFiles")))
            { Directory.CreateDirectory(Path.Combine(Dirs[this.DirID], "OutputFiles")); }
            this.OutFilePath = Path.Combine(Dirs[this.DirID],"OutputFiles");
            this.DirID++;
        }
        void Initiate(string _fileName,string _NFileName)
        {
            this.AllPolymerases.Clear(); this.Generations = 0;
            this.Sequences = new List<string>();this.refSeqs = new List<string>();
            Console.ForegroundColor = ConsoleColor.Green;
            OpenFile:
            string FileName = _fileName; int count = 0;
            int LastSequenceLength = 0;
            try
            {
                FileStream InputFile = new FileStream(this.currentDir + "\\" + FileName, FileMode.Open, FileAccess.Read);
                StreamReader Reader = new StreamReader(InputFile);
                string line = Reader.ReadLine(); string sequence = "";
                while (line != null)
                {
                    sequence = this.DrawSeqFromLine(line);
                    if (sequence != "")
                    {
                        this.Sequences.Add(sequence); count++;
                        if (LastSequenceLength == 0) { LastSequenceLength = sequence.Length; }
                        //else
                        //{
                        //    if (LastSequenceLength != sequence.Length) { throw new Exception(); }
                        //}
                    }

                    line = Reader.ReadLine();
                }
                this.patternLength = LastSequenceLength; this.MaxNTnumber = this.patternLength;
                Reader.Close();
            }
            catch(Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red; System.Threading.Thread.Sleep(500); Console.WriteLine(e.Message); System.Threading.Thread.Sleep(500);
                Console.ForegroundColor = ConsoleColor.Green; goto OpenFile;
            }
            FileName = _NFileName;int countN = 0;
            try
            {
                FileStream InputFile = new FileStream(this.currentDir + "\\" + FileName, FileMode.Open, FileAccess.Read);
                StreamReader Reader = new StreamReader(InputFile);
                string line = Reader.ReadLine(); string sequence = "";
                while (line != null)
                {
                    sequence = this.DrawSeqFromLine(line);
                    if (sequence != "")
                    {
                        this.refSeqs.Add(sequence); countN++;
                        //if (LastSequenceLength == 0) { LastSequenceLength = sequence.Length; }
                        //else
                        //{
                        //    if (LastSequenceLength != sequence.Length) { throw new Exception(); }
                        //}
                    }

                    line = Reader.ReadLine();
                }
                this.patternLength = LastSequenceLength; this.MaxNTnumber = this.patternLength;
                Reader.Close();
            }
            catch(Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red; System.Threading.Thread.Sleep(500); Console.WriteLine(e.Message); System.Threading.Thread.Sleep(500);
                Console.ForegroundColor = ConsoleColor.Green; goto OpenFile;
            }
            Console.WriteLine("\nGenerating new image.");
            Console.WriteLine("\n(The promoter train set contains {0} sequence{2}.{4})\n(The non-promoter train set contains {1} sequence{3})\n", count,countN,count<2?"":"s",countN<2?"":"s",
                count<50?" [The amount of promoters is too small!]":"");
            this.OutFileID = 1;
            Console.WriteLine();
            //string ans = Console.ReadLine();
        }

        void Seeding()
        {
            for (int i = 0; i < this.SeedingSize; i++)
            {
                this.AllPolymerases.Add(new Enzyme(this.GetRandomPattern(), this));
            }
            this.SeedingSize = this.CommonSeedingSize;
        }   //播种
        void Breeding()    //杂交
        {
            int Times = this.AllPolymerases.Count * 2;
            for (int i = 0; i < Times; i++)
            {
                int E1 = this.dice.Next(0, this.AllPolymerases.Count);
                int E2 = this.dice.Next(0, this.AllPolymerases.Count);
                while (E1 == E2) { E2 = this.dice.Next(0, this.AllPolymerases.Count); }
                this.AllPolymerases[E1].Recombinate(this.AllPolymerases[E2]);
            }
        }
        void Mutant()
        {
            Enzyme[] newPolymerases = (from i in this.AllPolymerases
                                       where i.IsNew == true
                                       select i).ToArray();
            for (int i = 0; i < Convert.ToInt32(this.mutantRate * newPolymerases.Length); i++)
            {
                newPolymerases[this.dice.Next(0, newPolymerases.Length)].Mutate();
            }
        }
        void GenerateScores()
        {
            this.ScoreList.Clear();
            for (int numberOfEs = 1; numberOfEs <= this.AllPolymerases.Count; numberOfEs++)
            { this.ScoreList.Add(0); }
            int count = 0;
            Parallel.For (0,this.AllPolymerases.Count,i=>
            {
                count++;
                if (count % 1000 == 0) { Console.Write("*"); }
                if (this.NTnumber(this.AllPolymerases[i]) > this.MaxNTnumber)
                { this.ScoreList[i]=0; }
                else
                { this.ScoreList[i]=(this.Score(this.AllPolymerases[i])); }
            }
            );
            Console.WriteLine();
        } //淘汰一部分
        void Screen()
        {
            for (int i = 0; i < this.ScoreList.Count; i++)
            {
                for(int j = 0;j < i; j++)
                    {
                        if (this.ScoreList[i] >= this.ScoreList[j])
                        {
                            ScoreList.Insert(j, ScoreList[i]); ScoreList.RemoveAt(i + 1);
                            this.AllPolymerases.Insert(j, this.AllPolymerases[i]); this.AllPolymerases.RemoveAt(j + 1);
                        break;
                        }
                    }
            }
            int cutPoint = Convert.ToInt32(this.ScoreList.Count * this.cutOff);
            this.ScoreList.RemoveRange(cutPoint, this.ScoreList.Count - cutPoint);
            this.AllPolymerases.RemoveRange(cutPoint, this.AllPolymerases.Count - cutPoint);
            foreach (Enzyme e in this.AllPolymerases)
            { e.IsNew = false; }
        }
        double Score(Enzyme E)
        {
            double result = 0;
            List<int[]> positions = new List<int[]>();
            bool NT = false; int leftPoint = 0, rightPoint = 0;
            for (int i = 0; i < E.Pattern.Length; i++)
            {
                if (i == 0)
                {
                    if (E.Pattern[i] == '-') { NT = false; leftPoint = 0; }
                    else { NT = true; }
                }
                else
                {
                    if (NT && E.Pattern[i] == '-') { leftPoint = i; NT = false; }
                    else if (!NT && E.Pattern[i] != '-') { rightPoint = i - 1; positions.Add(new int[] { leftPoint, rightPoint }); NT = true; }
                }
                if ((i == E.Pattern.Length - 1) && !NT)
                { rightPoint = E.Pattern.Length - 1; positions.Add(new int[] { leftPoint, rightPoint }); }
            }
            List<string> SoftPatterns = this.GetSoftPatterns(E.Pattern, positions);
            result = this.GetScoreForOneSeq(SoftPatterns);
            return result;
        }
        public void Run()
        {
            this.FirstRound = true;this.DirID = 0;
            int RepeatTimes=Directory.GetDirectories(Directory.GetCurrentDirectory()).Length;
            for (int repeat = 0; repeat < RepeatTimes; repeat++)
            {
                this.FindCurrentDir();
                this.Initiate("PSeqs.txt", "NSeqs.txt");
                this.Cycle(100);
            }
        }
        void Cycle(int numOfCycles)
        {

            this.Seeding();
            for (int i = 0; i < numOfCycles; i++)
            {
                if (this.AllPolymerases.Count < this.CommonSeedingSize * 0.2)
                { this.Seeding(); }
                this.Breeding();
                this.Mutant();
                Console.WriteLine("Scoring.");
                this.GenerateScores();
                this.Screen();
                this.Output();
                this.Generations++;
                Console.WriteLine("There has been {0} generation{1}", this.Generations,this.Generations==1?".":"s.");
            }
        }
        void Output()
        {
            string OutputFileName = this.OutFilePath + "\\Gen" + Convert.ToString(this.OutFileID) + ".txt";
            FileStream outfile = new FileStream(OutputFileName, FileMode.Create, FileAccess.Write);
            StreamWriter outWriter = new StreamWriter(outfile);
            for (int i = 0; i < this.AllPolymerases.Count; i++)
            {
                outWriter.WriteLine(this.AllPolymerases[i].Pattern);
            }
            for (int i = 0; i < this.ScoreList.Count; i++)
            { outWriter.WriteLine(this.ScoreList[i]); }
            outWriter.Close();
            this.OutFileID++;
        }
        string[] ExsistDirs;
        void SearchExsitDirs()
        {
            this.ExsistDirs = Directory.GetDirectories(Directory.GetCurrentDirectory());            
        }

        //Tool
        string GetRandomPattern()
        {
            int position = this.dice.Next(0, this.patternLength - this.startNTnumber);
            string result = new string('-', this.patternLength);
            for (int i = 0; i < this.startNTnumber; i++)
            {
                result = result.Substring(0, position + i) + Enzyme.GetNT(this.dice.Next(0, 4)) + result.Substring(position + i + 1, this.patternLength - position - 1 - i);
            }
            return result;
        }
        string DrawSeqFromLine(string line)
        {
            string result = "";
            string[] LineBreak = line.Split(' ');
            foreach (string item in LineBreak)
            {
                if (item.Length > 10 && IsDNA(item))
                { result = item; }
            }
            return result;
        }
        public static bool IsDNA(string input)
        {
            bool result = true;
            for (int i = 0; i < input.Length; i++)
            {
                if (input.ToLower()[i] != 'a' && input.ToLower()[i] != 't' && input.ToLower()[i] != 'c' && input.ToLower()[i] != 'g')
                { result = false; }
            }
            return result;
        }
        bool IsPattern(string input)
        {
            bool result = true; string line = input.ToLower();
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] != 'a' && line[i] != 't' && line[i] != 'c' && line[i] != 'g' && line[i] != '-')
                { result = false; }
            }
            return result;
        }

/// <summary>
/// Build gap varied patterns
/// </summary>
/// <param name="input"></param>
/// <param name="AllGaps"></param>
/// <returns></returns>
        List<string> GetSoftPatterns(string input, List<int[]> AllGaps)
        {
            List<string> result = new List<string>(); int[] addGaps = new int[AllGaps.Count];
            int gapID = -1,maxGapLength=-1;
            for (int i = 0; i < AllGaps.Count; i++)
            {
                if (AllGaps[i][0] > 0 && AllGaps[i][1] < input.Length - 1)
                {
                    int curLength = AllGaps[i][1] - AllGaps[i][0] + 1;
                    if (curLength > maxGapLength)
                    { gapID = i;maxGapLength = curLength; }
                }
            }
            if (gapID >= 0)
            {
                for (int i = 0; i <= maxGapLength * this.Softness; i++)
                {
                    if (i != 0)
                    { result.Add(input.Substring(0,AllGaps[gapID][0])+Gaps(maxGapLength-i)+input.Substring(AllGaps[gapID][1]+1,input.Length-AllGaps[gapID][1]-1)); }
                    result.Add(input.Substring(0, AllGaps[gapID][0]) + Gaps(maxGapLength + i) + input.Substring(AllGaps[gapID][1] + 1, input.Length - AllGaps[gapID][1] - 1));
                }
            }
            return result;
        }
        double GetAlignScore(string seq1, string seq2)
        {
            double MaxScore = 0, currentScore = 0;
            for (int i = 0; i < seq1.Length - seq2.Length; i++)
            {
                currentScore = 0;
                for (int j = 0; j < seq2.Length; j++)
                {
                    if (seq2[j] != '-' && seq1[j + i] == seq2[j]) { currentScore++; }
                    else if (seq2[j]!='-') { currentScore -=this.Penalty; }
                }
                if (currentScore > MaxScore) { MaxScore = currentScore; }
            }
            return MaxScore;
        }
        double GetScoreForOneSeq(List<string> softSeqs)
        {
            double result = 0, currentScore = 0,resultP=0,resultN=0;
            List<double> results = new List<double>(); 
            foreach (string seq1 in this.Sequences)
            {
                result = 0;
                foreach (string seq2 in softSeqs)
                {
                    currentScore = this.GetAlignScore("---" + seq1+"---", seq2);
                    if (currentScore > result) { result = currentScore; }
                }
                results.Add(result); 
            }
            resultP= results.Average();
            results.Clear();
            foreach (string seq1 in this.refSeqs)
            {
                result = 0;
                foreach (string seq2 in softSeqs)
                {
                    currentScore = this.GetAlignScore("--" + seq1+"--", seq2);
                    if (currentScore > result) { result = currentScore; }
                }
                results.Add(result);
            }
            resultN = results.Average();
            return resultP - resultN;
        }
        int NTnumber(Enzyme E)
        {
            int result = 0;
            foreach (char item in E.Pattern)
            {
                if (item != '-')
                { result++; }
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

        int DirNumber(string name)
        {
            if (!name.StartsWith("Output")) { return 9999; }
            string resultSTR = "";
            foreach (char item in name)
            {
                if (char.IsNumber(item))
                { resultSTR += item; }
            }
            return Convert.ToInt32(resultSTR);
        }
    }
}
