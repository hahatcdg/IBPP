using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IBPP_SVM
{
    class PatternScorer
    {
        List<string> Patterns, SoftPatterns; string Pattern;
        List<List<string>> SoftPatternCollection;
        SeqData Sequence;
        int N; double Softness;
        const double Penalty = 0.4;
        public double[] Score
        { get; private set; }
        public PatternScorer()
        {
            this.Patterns = new List<string>(); this.SoftPatterns = new List<string>();
            this.SoftPatternCollection = new List<List<string>>();this.Softness = 0.2;
        }
        internal PatternScorer(int n)
        { this.N = n; this.Score = new double[n]; }
        public void Initiate(List<string> _patterns)
        {
            this.Patterns = _patterns; this.N = this.Patterns.Count;
            this.Score = new double[this.N + 1];
            this.GenerateSoftPatternCollection();
        }
        internal void InputSequence(SeqData seq)
        {
            this.Sequence = seq;
        }
        internal void GetScore()
        {
            this.Score[0] = this.Sequence.Score;
            for (int i = 0; i < this.N; i++)
            {
                this.ScoreOnePattern(i);
            }

        }
        void ScoreOnePattern(int id)
        {
            this.SoftPatterns = this.SoftPatternCollection[id];
            this.Score[id + 1] = this.GetOneScore(this.Sequence.Seq, this.SoftPatterns);
        }
        double GetOneScore(string Seq, List<string> SoftPattern)
        {
            double maxScore = 0; string inputSeq;
            if (Seq.Length < this.Pattern.Length + 10)
            { inputSeq = "--" + Seq + "--"; }
            else { inputSeq = Seq; }
            for (int i = 0; i < SoftPattern.Count; i++)
            {
                maxScore = Align(inputSeq, SoftPattern[i], maxScore);
            }
            return maxScore;
        }

        public static double Align(string query, string pattern, double maxScore)
        {
            double MaxScore = maxScore, currentScore = 0;
            for (int i = 0; i < query.Length - pattern.Length; i++)
            {
                currentScore = 0; 
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (pattern[j] != '-' && query[j + i] == pattern[j]) { currentScore++;  }
                    else if (pattern[j] != '-') { currentScore -= Penalty;  }
                }
                //Console.WriteLine("{0}\t{1}", ok, pen);
                //Console.WriteLine(seq1); Console.WriteLine(seq2); Console.WriteLine();
                if (currentScore > MaxScore) { MaxScore = currentScore; }
            }
            return MaxScore;
        }
        void GenerateSoftPatternCollection()
        {
            this.SoftPatternCollection.Clear();
            for (int i = 0; i < this.Patterns.Count; i++)
            { this.GenerateSoftPatterns(this.Patterns[i]); }
        }

        void GenerateSoftPatterns(string pattern)
        {
            this.Pattern = pattern;
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
            this.SoftPatternCollection.Add(result);
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
