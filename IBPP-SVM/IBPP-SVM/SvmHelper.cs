using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using LibSVMsharp;
using LibSVMsharp.Extensions;
using LibSVMsharp.Helpers;
using System.IO;

namespace IBPP_SVM
{

    class SvmHelper
    {
        FileCordinator files; ProcessStartInfo svmStartInfo,svmTrainInfo;
        public SvmHelper()
        { }
        public SvmHelper(FileCordinator _files)
        {
            this.files = _files;
        }
        public void Update(FileCordinator newFiles)
        {
            this.files = newFiles;
        }
        public void Initiate()
        {
            this.svmStartInfo = new ProcessStartInfo(files.SvmEXEFile, string.Format("{0} {1} {2}", files.SVMdataFile, files.SvmModelFile, files.SVMresultFile));
            this.svmTrainInfo = new ProcessStartInfo(files.SVMTrainEXE, string.Format("{0}",files.SVMTrainSet));
        }

        public void Train()
        {
            SVMProblem problem = SVMProblemHelper.Load(Path.Combine(Environment.CurrentDirectory,"temp",this.files.SVMTrainSet));
            SVMModel model = SVM.Train(problem,new SVMParameter() {Gamma=0.1 });
            SVM.SaveModel(model, Path.Combine(Environment.CurrentDirectory, this.files.SvmModelFile));
        }

        public void Predict()
        {
            SVMModel model = SVM.LoadModel(Path.Combine(Environment.CurrentDirectory,this.files.SvmModelFile));
            SVMProblem problem = SVMProblemHelper.Load(Path.Combine(Environment.CurrentDirectory,"temp",this.files.SVMdataFile));
            using (StreamWriter outWriter = new StreamWriter(Path.Combine(Environment.CurrentDirectory,"temp", this.files.SVMresultFile)))
            {
                Stopwatch sw = new Stopwatch();
                double[] result = new double[problem.X.Count];
                sw.Start();
                Parallel.For(0, result.Length, (i) =>
                {
                    result[i] = SVM.Predict(model, problem.X[i]);
                });
                sw.Stop();
                Console.WriteLine("\nCost：{0} second{1}",sw.ElapsedMilliseconds/1000, sw.ElapsedMilliseconds / 1000<2?"":"s");
                foreach (double num in result) { outWriter.WriteLine(num); }
                
            }
        }
    }
}
