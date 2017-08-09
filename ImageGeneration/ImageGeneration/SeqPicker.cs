using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace 启动子预测_整合版
{
    class SeqPicker
    {
        string PositiveFileName, NegativeFileName,currentDirectory;List<int> Choices;Random dice;
        StreamWriter OutWriter;
        List<string> Sequences;
        int maxPNumberToPick,maxRefNumberToPick;
        public SeqPicker()
        { this.Choices = new List<int>(); this.dice = new Random(); this.Sequences = new List<string>();this.maxPNumberToPick=this.maxRefNumberToPick = 500; }
        public SeqPicker(string _pName,string _nName):this()
        { this.PositiveFileName = _pName;this.NegativeFileName = _nName; }
        void CountMaxNumber()
        {
            string name = "";
            for (int i = 0; i < 2; i++)
            {
                name = i == 0 ? this.PositiveFileName : this.NegativeFileName;
                FileStream InputFile = new FileStream(Directory.GetCurrentDirectory() + "\\" + name, FileMode.Open, FileAccess.Read);
                StreamReader InputReader = new StreamReader(InputFile);
                string line = InputReader.ReadLine(); int count = 1;
                while (line != null)
                {
                    line = InputReader.ReadLine();
                    if (line != null)
                    { count++; }
                }
                if (i == 0)
                { this.maxPNumberToPick = count; }
                else
                { this.maxRefNumberToPick = count; }
                InputReader.Close();
            }
        }

        public void CleanDir()
        {
            string[] leftDirs = (from dir in Directory.GetDirectories(Environment.CurrentDirectory)
                                 where new DirectoryInfo(dir).Name.StartsWith("Output")
                                 select dir).ToArray();
            foreach (string dir in leftDirs)
            { Directory.Delete(dir,true); }
        }

        void CreateOutputDir()
        {
            string[] Dirs = Directory.GetDirectories(Directory.GetCurrentDirectory());
            string NewDirName = string.Format("Output{0}",Dirs.Length+1);
            this.currentDirectory = Directory.GetCurrentDirectory() + "\\" + NewDirName;
            Directory.CreateDirectory(this.currentDirectory);
        }
        bool NSeqTime;
        void MainProject()
        {
            {
                this.NSeqTime = false;
                FileStream InputFile = new FileStream(Directory.GetCurrentDirectory() + "\\" + this.PositiveFileName, FileMode.Open, FileAccess.Read);
                StreamReader InputReader = new StreamReader(InputFile);
                FileStream OutputFile = new FileStream(this.currentDirectory + "\\PSeqs.txt", FileMode.Create, FileAccess.Write);
                this.OutWriter = new StreamWriter(OutputFile);
                this.GetOriSeqs(InputReader);
                this.PickSeqs();
                InputReader.Close();
                this.WriteChoices(); this.NSeqTime = true; this.OutWriter.Close();
                InputFile = new FileStream(Directory.GetCurrentDirectory() + "\\" + this.NegativeFileName, FileMode.Open, FileAccess.Read);
                InputReader = new StreamReader(InputFile);
                OutputFile = new FileStream(this.currentDirectory + "\\NSeqs.txt", FileMode.Create, FileAccess.Write);
                this.OutWriter = new StreamWriter(OutputFile);
                this.GetOriSeqs(InputReader); this.PickSeqs(); this.WriteChoices(); InputReader.Close(); this.OutWriter.Close(); this.NSeqTime = false;
            }
        }
        void WriteChoices()
        {
            for (int i = 0; i < this.Choices.Count; i++)
            {
                this.OutWriter.Write(this.Choices[i]);
                if (i != this.Choices.Count - 1)
                { this.OutWriter.Write(" "); }
            }
        }
        void GetOriSeqs(StreamReader InputReader)
        {
            this.Sequences.Clear();
            string line = InputReader.ReadLine();
            while (line != null)
            {
                if (line.Length > 0)
                { this.Sequences.Add(line.Trim(' ')); }
                line = InputReader.ReadLine();
            }
        }
        void PickSeqs()
        {
            this.Choices.Clear(); int times = 0;
            if (!this.NSeqTime) { times = this.maxPNumberToPick; }
            else { times = this.maxRefNumberToPick; }
            for (int i = 0; i < Math.Min(times,this.Sequences.Count); i++)
            { this.PickOneSeq(); }
        }
        void PickOneSeq()
        {
            int choice = this.dice.Next(0,this.Sequences.Count);
            int repeats = 0;
            while (this.Choices.Contains(choice)&&repeats<100)
            {choice= this.dice.Next(0, this.Sequences.Count);
                repeats++;
            }
            if (!this.Choices.Contains(choice))
            {
                this.Choices.Add(choice);
                this.OutWriter.WriteLine(Sequences[choice]);
            }
        }
        public void Run()
        {
            this.CreateOutputDir();
            this.MainProject();
        }

        public void ReadPatterns()
        {
            string[] Files = (from dir in Directory.GetDirectories(Environment.CurrentDirectory)
                              where File.Exists(Path.Combine(dir, "OutputFiles", "Gen100.txt"))
                              select Path.Combine(dir, "OutputFiles", "Gen100.txt")
                           ).ToArray();
            if (Files.Length > 0)
            {
                using (StreamWriter sw = new StreamWriter(Path.Combine(Environment.CurrentDirectory, "Pattern.txt")))
                {
                    foreach (string f in Files)
                    {
                        using (StreamReader sr = new StreamReader(f))
                        {
                            string pattern = sr.ReadLine();
                            sw.WriteLine(pattern);
                        }
                    }
                }
            }
        }

        public static Exception CheckEnvironment()
        {
            bool result = true;
            //check P.txt
            if (!File.Exists(Path.Combine(Environment.CurrentDirectory, "P.txt")))
            { return new FileNotFoundException("Promoter sequences not found.","P.txt"); }
            if (!File.Exists(Path.Combine(Environment.CurrentDirectory, "Ref.txt")))
            { return new FileNotFoundException("Non-promoter sequences not found.","Ref.txt"); }
            using (StreamReader sr = new StreamReader(Path.Combine(Environment.CurrentDirectory, "P.txt")))
            {
                int count = 0;
                string line = sr.ReadLine();
                while (line != null)
                {
                    if (EvolutionEnvironment.IsDNA(line.Trim(' '))) { count++; }
                    else { result = false; }
                    line = sr.ReadLine();
                }
                if (count < 1) { result = false; }
            }
            using (StreamReader sr = new StreamReader(Path.Combine(Environment.CurrentDirectory, "Ref.txt")))
            {
                int count = 0;
                string line = sr.ReadLine();
                while (line != null)
                {
                    if (EvolutionEnvironment.IsDNA(line.Trim(' '))) { count++; }
                    else { result = false; }
                    line = sr.ReadLine();
                }
                if (count < 1) { result = false; }
            }
            if (result == false)
            { return new Exception("the training sets are not OK."); }
            return null;
        }
    }
}
