using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Reader
{
    public class DeviceReader
    {
        public event Action<double, double, double, double, double> OnLineRead;
        public event Action<List<MeasureModel>, List<MeasureModel>, List<MeasureModel>, List<MeasureModel>> OnBatchCompleted;
        public event Action<MeasureModel, MeasureModel, MeasureModel, MeasureModel, DateTime> OnEveryLine; 

        private SerialPort _port;
        private DateTime _lastSend = new DateTime(1, 1, 1);
        private List<double> rs = new List<double>();
        private List<double> irs = new List<double>();
        private List<double> gs = new List<double>();
        private List<double> mags = new List<double>(); 
        private int _batchSize;

        private List<MeasureModel> rBatch = new List<MeasureModel>();
        private List<MeasureModel> irBatch = new List<MeasureModel>();
        private List<MeasureModel> gBatch = new List<MeasureModel>();
        private List<MeasureModel> mBatch = new List<MeasureModel>(); 

        public void Start(string comPort, int baudRate, int batchSize)
        {
            DateTime startTime = DateTime.Now; 
            _batchSize = batchSize; 
            if(_port == null)
            {
                _port = new SerialPort(comPort, baudRate);
                _port.DataReceived += (sender, e) =>
                {
                    //string line = _port.ReadExisting().Trim(); 
                    string line = _port.ReadLine().Trim();
                    if(line.StartsWith("R["))
                    {
                        double r = 0, ir = 0, g = 0, x = 0, y = 0, z = 0;
                        //R[972] IR[709] G[211]
                        //-?[0-9]\d*(\.\d+)?
                        Match match = Regex.Match(line, @"^R\[([0-9]+)\] IR\[([0-9]+)\] G\[([0-9]+)\] X\[(-?[0-9]\d*\.\d+?)\] Y\[(-?[0-9]\d*\.\d+?)\] Z\[(-?[0-9]\d*\.\d+?)\]"); 
                        if(match.Groups.Count ==7)
                        {
                            r = double.Parse(match.Groups[1].Value);
                            ir = double.Parse(match.Groups[2].Value);
                            g = double.Parse(match.Groups[3].Value);

                            x = double.Parse(match.Groups[4].Value);
                            y = double.Parse(match.Groups[5].Value);
                            z = double.Parse(match.Groups[6].Value);

                            double mag = Math.Sqrt(x * x + y * y + z * z) - 9.28; 

                            rs.Add(r);
                            irs.Add(ir);
                            gs.Add(g);
                            mags.Add(mag); 

                            DateTime now = DateTime.Now;
                            double timeStamp = (now - startTime).TotalSeconds;

                            if ((now - _lastSend).TotalMilliseconds >= 50)
                            {
                                double rAverage = rs.Average();
                                double irAverage = irs.Average();
                                double gAverage = gs.Average();
                                double mAverage = mags.Average(); 

                                OnLineRead?.Invoke(rAverage, irAverage, gAverage, mAverage, timeStamp);
                                irs.Clear();
                                rs.Clear();
                                gs.Clear();
                                mags.Clear(); 
                            }

                            MeasureModel rModel=  new MeasureModel
                            {
                                Time = timeStamp,
                                Value = r,
                            }; 
                            MeasureModel irModel = new MeasureModel
                            {
                                Time = timeStamp,
                                Value = ir,
                            };
                            MeasureModel gModel = new MeasureModel
                            {
                                Time = timeStamp,
                                Value = g,
                            };

                            MeasureModel mModel = new MeasureModel
                            {
                                Time = timeStamp,
                                Value = mag,
                            };

                            rBatch.Add(rModel);
                            irBatch.Add(irModel);
                            gBatch.Add(gModel);
                            mBatch.Add(mModel); 

                            OnEveryLine?.Invoke(rModel, irModel, gModel, mModel, now); 

                            if (irBatch.Count >= _batchSize)
                            {
                                OnBatchCompleted?.Invoke(rBatch, irBatch, gBatch, mBatch);

                                rBatch.Clear();
                                irBatch.Clear();
                                gBatch.Clear();
                                mBatch.Clear(); 
                            }

                            _lastSend = now; 
                        }
                    }
                };
                _port.Open(); 
            }
        }

        public void Stop()
        {
            _port?.Close();
            _port = null; 
        }
    }
}
