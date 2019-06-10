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
        public class AnalyzedResult
        {
            public List<MeasureModel> BpmList = new List<MeasureModel>();
            public List<MeasureModel> Spo2List = new List<MeasureModel>();
            public List<MeasureModel> AccelList = new List<MeasureModel>();

            public int BpmReadSuccessCount { get; set; }
            public int Spo2ReadSuccessCount { get; set; }
            public int TotalRecordGroupsAnalyzed { get; set; }
        }

        public class RawResult
        {
            public List<double> Green;
            public List<double> Ir;
            public List<double> Red;
            public List<double> Accel;
        }

        private string _fileName = ""; 
        public FileReader(string fileName)
        {
            _fileName = fileName; 
        }

        public RawResult GetResultAtPosition(long filePosition)
        {
            RawResult result = new RawResult();
            result.Accel = new List<double>();
            result.Green = new List<double>();
            result.Ir = new List<double>();
            result.Red = new List<double>(); 

            using (FileStream fin = File.OpenRead(_fileName))
            {
                using (BinaryReader br = new BinaryReader(fin))
                {
                    br.BaseStream.Seek(filePosition, SeekOrigin.Begin);

                    for(int c=0;c<100;c++)
                    {
                        long fileTime = br.ReadInt64();
                        double r = br.ReadDouble();
                        double ir = br.ReadDouble();
                        double g = br.ReadDouble();
                        double m = 0;
                        byte marker = br.ReadByte();
                        if (marker != 0x69)
                        {
                            br.BaseStream.Seek(-1, SeekOrigin.Current);
                            m = br.ReadDouble();

                            marker = br.ReadByte();
                        }

                        if (marker == 0x69)
                        {
                            result.Accel.Add(m);
                            result.Green.Add(g);
                            result.Ir.Add(ir);
                            result.Red.Add(r); 
                        }
                    }
                }
            }

            return result; 
        }

        public AnalyzedResult Read(bool fillInZeros)
        {
            AnalyzedResult result = new AnalyzedResult(); 

            using (FileStream fin = File.OpenRead(_fileName))
            {
                using (BinaryReader br = new BinaryReader(fin))
                {
                    List<MeasureModel> irs = new List<MeasureModel>();
                    List<MeasureModel> rs = new List<MeasureModel>();
                    List<MeasureModel> gs = new List<MeasureModel>();
                    List<MeasureModel> ms = new List<MeasureModel>();

                    MeasureModel lastGoodBpm = null, lastGoodSpo2 = null, lastGoodAccel = null;
                    bool hasAccel = false; 

                    DateTime start = new DateTime(1, 1, 1);
                    try
                    {
                        while (br.BaseStream.Position < br.BaseStream.Length)
                        {
                            long filePosition = br.BaseStream.Position; 
                            long fileTime = br.ReadInt64();
                            double r = br.ReadDouble();
                            double ir = br.ReadDouble();
                            double g = br.ReadDouble();
                            double m = 0;
                            byte marker = br.ReadByte();
                            if (marker != 0x69)
                            {
                                br.BaseStream.Seek(-1, SeekOrigin.Current);
                                m = br.ReadDouble();
                                
                                hasAccel = true;
                                marker = br.ReadByte();
                            }

                            if (marker == 0x69)
                            {
                                DateTime dt = DateTime.FromFileTime(fileTime);
                                //File.AppendAllText("C:/users/ben/desktop/accelRaw.csv", $"{dt}, {m}\n"); 

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
                                    FilePosition = filePosition,
                                });
                                rs.Add(new MeasureModel
                                {
                                    Time = seconds,
                                    DateTime = dt,
                                    Value = r,
                                    FilePosition  = filePosition,
                                });
                                gs.Add(new MeasureModel
                                {
                                    Time = seconds,
                                    DateTime = dt,
                                    Value = g,
                                    FilePosition = filePosition,
                                });
                                if (hasAccel)
                                {
                                    ms.Add(new MeasureModel
                                    {
                                        Time = seconds,
                                        DateTime = dt,
                                        Value = m,
                                        FilePosition = filePosition,
                                    });
                                }

                                if (rs.Count == 100)
                                {
                                    result.TotalRecordGroupsAnalyzed++;

                                    double spo2 = 0, bpm = 0, xyRatio = 0;
                                    if (Robert.Interop.Compute(irs.Select(n => n.Value).ToArray(), rs.Select(n => n.Value).ToArray(), ref spo2, ref bpm, ref xyRatio) &&
                                        spo2 > 90 && spo2 < 100)
                                    {
                                        lastGoodSpo2 = new MeasureModel
                                        {
                                            Time = seconds,
                                            DateTime = dt,
                                            Value = spo2,
                                            FilePosition = filePosition,
                                        };
                                        result.Spo2ReadSuccessCount++; 
                                        result.Spo2List.Add(lastGoodSpo2);
                                    }
                                    else if (fillInZeros && lastGoodSpo2 != null)
                                    {
                                        lastGoodSpo2.DateTime = dt; 
                                        lastGoodSpo2.Time = seconds;
                                        result.Spo2List.Add(lastGoodSpo2);
                                    }
                                    else
                                    {
                                        result.Spo2List.Add(new MeasureModel
                                        {
                                            Time = seconds,
                                            DateTime = dt,
                                            Value = 95,
                                            FilePosition = filePosition,
                                        });
                                    }

                                    SignalProcessor.Mean(ref gs);
                                    SignalProcessor.LineLeveling(ref gs);
                                    List<MeasureModel> smoothed = null;
                                    bpm = SignalProcessor.ComputeBpm(gs, out smoothed);

                                    if (bpm > 0)
                                    {
                                        lastGoodBpm = new MeasureModel
                                        {
                                            Time = seconds,
                                            DateTime = dt,
                                            Value = bpm,
                                            FilePosition = filePosition,
                                        };

                                        result.BpmReadSuccessCount++; 
                                        result.BpmList.Add(lastGoodBpm);
                                    }
                                    else if (fillInZeros && lastGoodBpm != null)
                                    {
                                        lastGoodBpm.DateTime = dt;
                                        lastGoodBpm.Time = seconds;
                                        result.BpmList.Add(lastGoodBpm);
                                    }

                                    if (hasAccel)
                                    {
                                        result.AccelList.Add(new MeasureModel
                                        {
                                            Time = seconds,
                                            DateTime = dt,
                                            Value = ms.Average(n => n.Value),
                                            FilePosition = filePosition,
                                        });
                                    }

                                    rs.Clear();
                                    irs.Clear();
                                    gs.Clear();
                                    ms.Clear();
                                }
                            }
                            else
                            {
                                //throw new FormatException(); 
                            }
                        }

                    }
                    catch (EndOfStreamException)
                    {
                        // just move along, sometimes we dont' flush all the way
                    }
                }
            }

            return result; 
        }
    }
}
