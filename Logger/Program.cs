using Reader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logger
{
    class Program
    {
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
                        dr.OnEveryLine += (r, ir, g, time) =>
                        {
                            //Console.Write(".");
                            bw.Write(time.ToFileTime());
                            bw.Write(r.Value);
                            bw.Write(ir.Value);
                            bw.Write(g.Value); 
                            bw.Write((byte)0x69);
                        };
                        dr.OnBatchCompleted += (r, ir, g) =>
                        {
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
                                Console.Write("."); 
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
                                        //SignalProcessor.Mean(ref irs, ref rs);
                                        //SignalProcessor.LineLeveling(ref irs, ref rs);
                                        //double spo2 = SignalProcessor.ComputeSpo2(irs, rs);
                                        double spo2 = 0, bpm = 0; 
                                        if(Robert.Interop.Compute(irs.Select(n=>n.Value).ToArray(), rs.Select(n=>n.Value).ToArray(), ref spo2, ref bpm))
                                        {
                                            if (spo2 > 90 && spo2 < 100)
                                            {
                                                lastSpo2 = spo2; 
                                            }
                                        }

                                        SignalProcessor.Mean(ref gs);
                                        SignalProcessor.LineLeveling(ref gs);
                                        bpm = SignalProcessor.ComputeBpm(gs);

                                        //foreach(double v in gs.Select(n=>n.Value))
                                        //{
                                        //    File.AppendAllText("C:/users/ben/desktop/turd.csv", $"{v}\n");
                                        //}

                                        sw.WriteLine($"{dt.ToString("MM/dd/yyyy hh:mm:ss.fff tt")},{spo2}, {bpm}");
                                        sw.Flush();
       
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

                return; 
            }
        }
    }
}
