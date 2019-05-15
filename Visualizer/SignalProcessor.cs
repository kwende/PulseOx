using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Visualizer
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
    }
}
