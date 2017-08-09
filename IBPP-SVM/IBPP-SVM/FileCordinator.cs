using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IBPP_SVM
{
    class FileCordinator
    {
        public string PatternFile { get; set; }
        public string InputSequenceFile { get; set; }
        public string SVMresultFile { get; set; }
        public string SVMdataFile { get; set; }
        public string SvmModelFile { get; set; }
        public string SvmEXEFile { get; set; }
        public string FinalResult { get; set; }
        public string SVMTrainEXE { get; set; }
        public string SVMTrainSet { get; set; }
        public FileCordinator()
        {
            this.PatternFile = Path.Combine(Directory.GetCurrentDirectory(), "Pattern.txt");
            this.InputSequenceFile = Path.Combine(Directory.GetCurrentDirectory(),"temp", "InputSeq.txt");
            this.SVMresultFile = "SVMResult.txt";
            this.SVMdataFile = "SVMdata.txt";
            this.SvmModelFile = "SVM.model";
            this.SvmEXEFile = "svm-predict.exe";
            this.SVMTrainEXE = "svm-train.exe";
            this.SVMTrainSet = "TrainSet.txt";
            this.FinalResult = Path.Combine(Directory.GetCurrentDirectory(), "output.xls");
        }
    }
}
