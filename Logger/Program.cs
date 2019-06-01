using Reader;
using Robert;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logger
{
    class Record
    {
        public double SPO2 { get; set; }
        public double BPM { get; set; }
        public DateTime TakenAt { get; set; }
    }

    class Program
    {
        static void SaveSnippet(List<MeasureModel> gs,string outputPath)
        {
            using (StreamWriter sw = new StreamWriter(outputPath))
            {
                for (int c = 0; c < gs.Count; c++)
                {
                    sw.WriteLine($"{gs[c].Value}");
                }
                sw.Flush();
            }
        }

        static void SaveSnippet(List<MeasureModel> irs, List<MeasureModel> rs, string outputPath)
        {
            using (StreamWriter sw = new StreamWriter(outputPath))
            {
                for(int c=0;c<irs.Count;c++)
                {
                    sw.WriteLine($"{irs[c].Value},{rs[c].Value}"); 
                }
                sw.Flush(); 
            }
        }

        static void Main(string[] args)
        {
            if(args.Length == 0)
            {
                string path = "data_" + DateTime.Now.ToString().Replace("/", "_").Replace(":", "_") + ".dat";
                //using (StreamWriter sw = new StreamWriter(path))
                using (FileStream fs = File.Create(path))
                {
                    using (BinaryWriter bw = new BinaryWriter(fs))
                    {
                        Reader.DeviceReader dr = new Reader.DeviceReader();
                        dr.Start("COM3", 9600, 100);
                        dr.OnEveryLine += (r, ir, g, m, time) =>
                        {
                            //Console.Write(".");
                            bw.Write(time.ToFileTime());
                            bw.Write(r.Value);
                            bw.Write(ir.Value);
                            bw.Write(g.Value);
                            bw.Write(m.Value);
                            bw.Write((byte)0x69);
                        };
                        dr.OnBatchCompleted += (r, ir, g, m) =>
                        {
                            SignalProcessor.Mean(ref g);
                            SignalProcessor.LineLeveling(ref g);
                            List<MeasureModel> smoothed = null; 
                            double myBpm = SignalProcessor.ComputeBpm(g, out smoothed);

                            double spo2 = 0, bpm = 0, xyRatio = 0; 
                            if (Interop.Compute(ir.Select(n => n.Value).ToArray(), r.Select(n => n.Value).ToArray(), ref spo2, ref bpm, ref xyRatio) && myBpm > 0)
                            {
                                Console.Clear(); 
                                Console.WriteLine($"SPO2: {spo2}, BPM: {myBpm}, Acc: {m.Max(n=>n.Value)}");
                            }

                            //Console.Write("+");
                            bw.Flush();
                        };
                        Console.WriteLine("Press ENTER to quit.");
                        Console.ReadLine();
                    }
                }
            }
            else
            {
                bool record = false; 

                string inputFile = args[0];
                string resultsFile = Path.Combine(Path.GetDirectoryName(inputFile), Path.GetFileNameWithoutExtension(inputFile)) + ".csv";
                using (StreamWriter sw = new StreamWriter(resultsFile))
                {
                    using (FileStream fin = File.OpenRead(inputFile))
                    {
                        using (BinaryReader br = new BinaryReader(fin))
                        {
                            List<MeasureModel> irs = new List<MeasureModel>();
                            List<MeasureModel> rs = new List<MeasureModel>();
                            List<MeasureModel> gs = new List<MeasureModel>(); 

                            DateTime start = new DateTime(1, 1, 1);
                            double lastSpo2 = -1; 
                            while(br.BaseStream.Position < br.BaseStream.Length)
                            {
                                long fileTime = br.ReadInt64();
                                double r = br.ReadDouble();
                                double ir = br.ReadDouble();
                                double g = br.ReadDouble(); 
                                byte marker = br.ReadByte();

                                if (marker == 0x69)
                                {
                                    DateTime dt = DateTime.FromFileTime(fileTime);

                                    if(start.Year == 1)
                                    {
                                        start = dt; 
                                    }

                                    irs.Add(new MeasureModel
                                    {
                                        Time = (dt - start).TotalSeconds,
                                        Value = ir,
                                    });
                                    rs.Add(new MeasureModel
                                    {
                                        Time = (dt - start).TotalSeconds,
                                        Value = r,
                                    });
                                    gs.Add(new MeasureModel
                                    {
                                        Time = (dt - start).TotalSeconds,
                                        Value = g,
                                    });


                                    if(rs.Count == 100)
                                    {
                                        bool doIt = false;
                                        if (dt.Hour == 4)
                                        {
                                            //SaveSnippet(irs, rs, "C:/users/brush/desktop/lateNight.csv");
                                            doIt = true;
                                        }

                                        double spo2 = 0, bpm = 0, xyRatio = 0; 
                                        if(Robert.Interop.Compute(irs.Select(n=>n.Value).ToArray(), rs.Select(n=>n.Value).ToArray(), ref spo2, ref bpm, ref xyRatio))
                                        {
                                            SignalProcessor.Mean(ref gs);
                                            SignalProcessor.LineLeveling(ref gs);
                                            List<MeasureModel> smoothed = null; 
                                            bpm = SignalProcessor.ComputeBpm(gs, out smoothed);

           
                                            if (spo2 > 94 && spo2 < 100 && bpm > 0)
                                            {
                                                sw.WriteLine($"{dt.ToString("MM/dd/yyyy hh:mm:ss.fff tt")},{spo2}, {bpm}");
                                                sw.Flush();
                                            }
                                        }

                                        rs.Clear();
                                        irs.Clear();
                                        gs.Clear(); 
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Corrupted file! Ending");
                                    return;
                                }
                            }
                        }
                    }
                }

                Console.WriteLine("Done. Press ENTER to quit.");
                Console.ReadLine(); 
            }
        }
    }
}
