using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reader
{
    // inspired and adapted from https://github.com/aromring/MAX30102_by_RF/blob/master/algorithm_by_RF.cpp
    public static class SignalProcessor
    {
        public static void Mean(ref List<MeasureModel> ir, ref List<MeasureModel> r)
        {
            double irAverage = ir.Average(n => n.Value); 
            double rAverage = r.Average(n=>n.Value);

            for(int c=0;c<ir.Count;c++)
            {
                ir[c].Value = ir[c].Value - irAverage;
                r[c].Value = r[c].Value - rAverage; 
            }
        }

        public static void Mean(ref List<MeasureModel> m)
        {
            double average = m.Average(n => n.Value);

            for (int c = 0; c < m.Count; c++)
            {
                m[c].Value = m[c].Value - average;
            }
        }

        public static void LineLeveling(ref List<MeasureModel> ir, ref List<MeasureModel> r)
        {
            LineLeveling(ref ir);
            LineLeveling(ref r); 
        }

        public static void LineLeveling(ref List<MeasureModel> line)
        {
            double avX = line.Average(n => n.Time);
            double avY = line.Average(n => n.Value); 

            double m = line.Sum(n=>(n.Time - avX) * (n.Value - avY)) / 
                line.Sum(n=>(n.Time - avX) * (n.Time - avX));

            double b = avY - m * avX; 

            for(int c=0;c<line.Count;c++)
            {
                double newValue = line[c].Value - (m * line[c].Time + b);
                //double newValue = (m * line[c].Time + b);
                line[c].Value = newValue; 
            }
        }

        private static List<int> FindAllPeaks(List<MeasureModel> heart)
        {
            const int Threshold = 10;

            List<int> peaks = new List<int>(); 
            for (int c = 1; c < heart.Count - 1; c++)
            {
                double v1 = heart[c - 1].Value;
                double v2 = heart[c].Value;
                double v3 = heart[c + 1].Value;

                if (v2 > Threshold &&
                    v1 < v2 && v2 > v3)
                {
                    peaks.Add(c); 
                }
            }

            return peaks; 
        }

        public static double ComputeBpm(List<MeasureModel> heart, bool doIt)
        {
            int beats = 0;
            List<double> times = new List<double>(); 

            //ZScoreOutput o = JeanPaul.StartAlgo(heart.Select(n => n.Value).ToList(), 20, 0, .5);

            // find all peaks
            List<int> allPeaks = FindAllPeaks(heart);

            // find weighted average of all peaks. 
            double average = 0;
            for (int c = 0; c < allPeaks.Count; c++)
            {
                int index = allPeaks[c];
                average += heart[index].Value;
            }

            average /= (allPeaks.Count * 1.0);
            average *= .5; 

            //File.Delete("C:/users/brush/desktop/heart.csv");

            for (int c = 0; c < allPeaks.Count; c++)
            {
                int index = allPeaks[c]; 
                MeasureModel m = heart[index];
                if(m.Value >= average)
                {
                    beats++;
                    times.Add(heart[index].Time);
                }
                //File.AppendAllText("C:/users/brush/desktop/heart.csv", $"{m.Time}, {m.Value}, {average}\n");
            }

            //int beats = 0;
            //for(int c=1;c<heart.Count-1;c++)
            //{
            //    double v1 = heart[c - 1].Value;
            //    double v2 = heart[c].Value;
            //    double v3 = heart[c +1].Value;

            //    if (v2 > Threshold && 
            //        v1 < v2 && v2 > v3)
            //    {
            //        beats++;
            //        times.Add(heart[c].Time);
            //    }
            //}
            if (times.Count > 2)
            {
                double first = times.First();
                double last = times.Last();

                double timeSpan = last - first;
                double multiple = 60.0 / timeSpan;

                double bpm = multiple * beats;
                return bpm;
            }
            else
            {
                return 0;
            }
        }

        public static double ComputeSpo2(List<MeasureModel> ir, List<MeasureModel> r)
        {
            double coeff = Correlation.Pearson(ir.Select(n => n.Value), r.Select(n => n.Value)); 
            if(coeff > .97)
            {
                double rmsR = Statistics.RootMeanSquare(r.Select(n => n.Value));
                double rmsIR = Statistics.RootMeanSquare(ir.Select(n => n.Value));

                double meanR = Statistics.Mean(r.Select(n => n.Value));
                double meanIr = Statistics.Mean(ir.Select(n => n.Value));

                double z = (rmsR / meanR) / (rmsIR / meanIr); 

                double spo2 = -45.060 * z * z + 30.354 * z + 94.845;

                return spo2;
            }
            else
            {
                return -1; 
            }
        }
    }
}
