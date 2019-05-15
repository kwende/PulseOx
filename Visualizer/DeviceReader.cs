using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Visualizer
{
    public class DeviceReader
    {
        public event Action<double, double, DateTime> OnLineRead;
        public event Action<List<MeasureModel>, List<MeasureModel>> OnBatchCompleted; 

        private SerialPort _port;
        private DateTime _lastSend = new DateTime(1, 1, 1);
        private List<double> rs = new List<double>();
        private List<double> irs = new List<double>();
        private int _batchSize;

        private List<MeasureModel> rBatch = new List<MeasureModel>();
        private List<MeasureModel> irBatch = new List<MeasureModel>(); 

        public void Start(string comPort, int baudRate, int batchSize)
        {
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
                        double r = 0, ir = 0; 
                        //R[972] IR[709] G[211]
                        Match match = Regex.Match(line, @"^R\[([0-9]+)\] IR\[([0-9]+)\]"); 
                        if(match.Groups.Count ==3)
                        {
                            r = double.Parse(match.Groups[1].Value);
                            ir = double.Parse(match.Groups[2].Value);

                            rs.Add(r);
                            irs.Add(ir); 

                            DateTime now = DateTime.Now; 
                            if((now - _lastSend).TotalMilliseconds >= 50)
                            {
                                double rAverage = rs.Average();
                                double irAverage = irs.Average();

                                OnLineRead(rAverage, irAverage, now);
                                irs.Clear();
                                rs.Clear();

                                rBatch.Add(new MeasureModel
                                {
                                     DateTime = now,
                                     Value = rAverage,
                                });

                                irBatch.Add(new MeasureModel
                                {
                                    DateTime = now,
                                    Value = irAverage,
                                }); 

                                if(irBatch.Count >= _batchSize)
                                {
                                    OnBatchCompleted(rBatch, irBatch); 
                                    rBatch.Clear();
                                    irBatch.Clear(); 
                                }
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
