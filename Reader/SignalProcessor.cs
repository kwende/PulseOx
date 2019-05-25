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

        private static List<int> FindExtrema(List<MeasureModel> data, bool findPeaks)
        {
            const int Lookahead = 2;

            List<int> points = new List<int>();
            for (int c = Lookahead; c < data.Count - Lookahead; c++)
            {
                double v1 = data[c - 1].Value;
                double v2 = data[c].Value;
                double v3 = data[c + 1].Value;

                if ((findPeaks && v1 < v2 && v2 > v3) || 
                    (!findPeaks && v1 > v2 && v2 < v3))
                {
                    points.Add(c);
                }
            }

            return points; 
        }

        private static List<MeasureModel> SmoothData(List<MeasureModel> m, int window)
        {
            List<MeasureModel> ret = new List<MeasureModel>(); 
            for(int i=window;i<m.Count-window;i++)
            {
                MeasureModel average = new MeasureModel();
                int a = 0;
                for (int d = i - window; d <= i + window; d++)
                {
                    average.Value += m[d].Value;
                    average.Time += m[d].Time;
                    a++; 
                }
                average.Value /= (window * 2.0 + 1);
                average.Time /= (window * 2.0 + 1);

                ret.Add(average); 
            }

            return ret; 
        }

        public static double ComputeBpm(List<MeasureModel> heart, out List<MeasureModel> smoothedHeart)
        {
            List<double> times = new List<double>();

            smoothedHeart = SmoothData(heart, 1);

            //File.Delete("C:/users/ben/desktop/bpm3.csv");
            //foreach (MeasureModel m in smoothedHeart)
            //{
            //    File.AppendAllText("C:/users/ben/desktop/bpm3.csv", $"{m.Value}\n");
            //}

            // find all peaks
            List<int> allValleys = FindExtrema(smoothedHeart, false);
            List<int> allPeaks = FindExtrema(smoothedHeart, true);

            //double std = MathNet.Numerics.Statistics.Statistics.StandardDeviation(peakValues);

                // iterate through each peak
            for (int c = 0, d=0; c < allPeaks.Count; c++)
            {
                int peakIndex = allPeaks[c]; 
                // find corresponding valley for disatole
                for(;d<allValleys.Count;d++)
                {
                    int valleyIndex = allValleys[d]; 

                    if(valleyIndex > peakIndex)
                    {
                        double peakValue = smoothedHeart[peakIndex].Value;
                        double valleyValue = smoothedHeart[valleyIndex].Value; 

                        if(peakValue - valleyValue > 50)
                        {
                            times.Add(smoothedHeart[valleyIndex].Time);
                        }
                        d++;
                        break;
                    }
                }
            }

            if (times.Count > 2)
            {
                double timeBetweenBeats = 0.0;
                for (int c = 1; c < times.Count; c++)
                {
                    timeBetweenBeats += times[c] - times[c - 1];
                }
                timeBetweenBeats /= (times.Count - 1 * 1.0);

                return 60.0 / timeBetweenBeats;

                //double first = times.First();
                //double last = times.Last();

                //double timeSpan = last - first;
                //double multiple = 60.0 / timeSpan;

                //double bpm = multiple * beats;
                //return bpm;
            }

            //// find weighted average of all peaks. 
            //double average = 0;
            //List<double> peakValues = new List<double>(); 
            //for (int c = 0; c < allValleys.Count; c++)
            //{
            //    int index = allValleys[c];
            //    double v = smoothedHeart[index].Value;
            //    peakValues.Add(v); 
            //    average += v;
            //}

            //double std = MathNet.Numerics.Statistics.Statistics.StandardDeviation(peakValues); 

            //if(std < 65)
            //{
            //    average /= (allValleys.Count * 1.0);
            //    average *= .5;

            //    for (int c = 0; c < allValleys.Count; c++)
            //    {
            //        int index = allValleys[c];
            //        MeasureModel m = smoothedHeart[index];
            //        //if (m.Value <= average)
            //        {
            //            beats++;
            //            times.Add(smoothedHeart[index].Time);
            //        }
            //    }

            //    if (times.Count > 2)
            //    {
            //        double timeBetweenBeats = 0.0;
            //        for (int c = 1; c < times.Count; c++)
            //        {
            //            timeBetweenBeats += times[c] - times[c - 1];
            //        }
            //        timeBetweenBeats /= (times.Count - 1 * 1.0);

            //        return 60.0 / timeBetweenBeats;

            //        //double first = times.First();
            //        //double last = times.Last();

            //        //double timeSpan = last - first;
            //        //double multiple = 60.0 / timeSpan;

            //        //double bpm = multiple * beats;
            //        //return bpm;
            //    }
            //}
            return 0; 
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
