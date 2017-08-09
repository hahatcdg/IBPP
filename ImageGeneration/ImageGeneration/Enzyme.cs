using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 启动子预测_整合版
{
    class Enzyme
    {
        int lengthPattern;
        public bool IsNew;
        string _pattern; EvolutionEnvironment World;
        Random dice;
        public string Pattern
        {
            get { return this._pattern; }
            set
            {
                if (value.Length != lengthPattern) { return; }
                this._pattern = value;
            }
        }
        public Enzyme()
        { this.dice = new Random(); this.IsNew = true; }
        public Enzyme(string initialPattern, EvolutionEnvironment world) : this()
        { this._pattern = initialPattern; this.World = world; this.lengthPattern = this.World.patternLength; }
        public void Die()
        { this.World.AllPolymerases.Remove(this); }
        public void Mutate()
        {
            int choice = this.dice.Next(0, this.lengthPattern);
            char newNT = Enzyme.GetNT(this.dice.Next(0, 5));
            while (newNT == this.Pattern[choice])
            { newNT = Enzyme.GetNT(this.dice.Next(0, 5)); }
            this.Pattern = this.Pattern.Substring(0, choice) + newNT + this.Pattern.Substring(choice + 1, this.lengthPattern - choice - 1);
        }
        public void Recombinate(Enzyme parterner)
        {
            string newPattern;
            int Conserve = this.dice.Next(0, 10);
            int point1 = -1, point2 = -1, point3 = -1, point4 = -1;
            point1 = this.dice.Next(0, this.Pattern.Length);
            point2 = this.dice.Next(0, this.Pattern.Length);
            while (point1 == point2) { point2 = this.dice.Next(0, this.Pattern.Length - 1); }
            if (Conserve ==0)
            {
                point3 = Math.Min(point1, point2) + 1;
                point4 = Math.Max(point1, point2) + 1;
                if (point4 >= parterner.Pattern.Length - 1)
                { point3--; point4--; }
            }
            else if (Conserve == 1)
            {
                point3 = Math.Min(point1, point2) - 1;
                point4 = Math.Max(point1, point2) - 1;
                if (point3 < 0)
                { point3++; point4++; }
            }
            else
            { point3 = Math.Min(point1, point2); point4 = Math.Max(point1, point2); }
            newPattern = parterner.Pattern.Substring(0, point3) + this.Pattern.Substring(Math.Min(point1, point2), Math.Abs(point2 - point1)) + parterner.Pattern.Substring(point4, parterner.Pattern.Length - point4);
            this.World.AllPolymerases.Add(new Enzyme(newPattern, this.World));
        }
        //Tool
        static public char GetNT(int choice)
        {
            switch (choice)
            {
                case 0:
                    return 'a';
                case 1:
                    return 't';
                case 2:
                    return 'c';
                case 3:
                    return 'g';
                default:
                    return '-';
            }
        }

    }
}
