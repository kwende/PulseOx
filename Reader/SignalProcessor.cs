using System;
using System.Collections.Generic;
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

        public static void LineLeveling(ref List<MeasureModel> ir, ref List<MeasureModel> r)
        {
            LineLeveling(ref ir);
            LineLeveling(ref r); 
        }

        private static void LineLeveling(ref List<MeasureModel> line)
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

        public static double ComputeSpo2(List<MeasureModel> ir, List<MeasureModel> r)
        {
            double average = 0.0; 
            for(int c=0;c<ir.Count;c++)
            {
                average += r[c].Value / ir[c].Value; 
            }
            average /= r.Count;

            return -45.060 * average * average + 30.354 * average + 94.845; 
        }
    }
}
