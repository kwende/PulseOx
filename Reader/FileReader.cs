using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reader
{
    public class FileReader
    {
        public class Result
        {
            public List<MeasureModel> BpmList = new List<MeasureModel>();
            public List<MeasureModel> Spo2List = new List<MeasureModel>();
            public List<MeasureModel> AccelList = new List<MeasureModel>();
        }

        private string _fileName = ""; 
        public FileReader(string fileName)
        {
            _fileName = fileName; 
        }

        public Result Read()
        {
            Result result = new Result(); 

            using (FileStream fin = File.OpenRead(_fileName))
            {
                using (BinaryReader br = new BinaryReader(fin))
                {
                    List<MeasureModel> irs = new List<MeasureModel>();
                    List<MeasureModel> rs = new List<MeasureModel>();
                    List<MeasureModel> gs = new List<MeasureModel>();
                    List<MeasureModel> ms = new List<MeasureModel>();

                    DateTime start = new DateTime(1, 1, 1);
                    while (br.BaseStream.Position < br.BaseStream.Length)
                    {
                        long fileTime = br.ReadInt64();
                        double r = br.ReadDouble();
                        double ir = br.ReadDouble();
                        double g = br.ReadDouble();
                        double m = br.ReadDouble();
                        byte marker = br.ReadByte();

                        if (marker == 0x69)
                        {
                            DateTime dt = DateTime.FromFileTime(fileTime);

                            if (start.Year == 1)
                            {
                                start = dt;
                            }

                            double seconds = (dt - start).TotalSeconds;

                            irs.Add(new MeasureModel
                            {
                                Time = seconds,
                                DateTime = dt,
                                Value = ir,
                            });
                            rs.Add(new MeasureModel
                            {
                                Time = seconds,
                                DateTime = dt,
                                Value = r,
                            });
                            gs.Add(new MeasureModel
                            {
                                Time = seconds,
                                DateTime = dt,
                                Value = g,
                            });
                            ms.Add(new MeasureModel
                            {
                                Time =seconds,
                                DateTime = dt,
                                Value = m,
                            });


                            if (rs.Count == 100)
                            {
                                double spo2 = 0, bpm = 0, xyRatio = 0;
                                if (Robert.Interop.Compute(irs.Select(n => n.Value).ToArray(), rs.Select(n => n.Value).ToArray(), ref spo2, ref bpm, ref xyRatio))
                                {
                                    if (spo2 > 94 && spo2 < 100)
                                    {
                                        result.Spo2List.Add(new MeasureModel
                                        {
                                            Time = seconds,
                                            DateTime = dt,
                                            Value = spo2,
                                        }); 
                                    }
                                }

                                SignalProcessor.Mean(ref gs);
                                SignalProcessor.LineLeveling(ref gs);
                                List<MeasureModel> smoothed = null;
                                bpm = SignalProcessor.ComputeBpm(gs, out smoothed);

                                if(bpm > 0)
                                {
                                    result.BpmList.Add(new MeasureModel
                                    {
                                        Time = seconds,
                                        DateTime = dt,
                                        Value = bpm,
                                    }); 
                                }

                                result.AccelList.Add(new MeasureModel
                                {
                                    Time = seconds,
                                    DateTime = dt,
                                    Value = ms.Max(n=>n.Value),
                                }); 


                                rs.Clear();
                                irs.Clear();
                                gs.Clear();
                                ms.Clear();
                            }
                        }
                        else
                        {
                            throw new FormatException(); 
                        }
                    }
                }
            }

            return result; 
        }
    }
}
