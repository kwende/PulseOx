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
            string path = "data_" + DateTime.Now.ToString().Replace("/", "_").Replace(":", "_") + ".dat";
            //using (StreamWriter sw = new StreamWriter(path))
            using (FileStream fs = File.Create(path))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    Reader.DeviceReader dr = new Reader.DeviceReader();
                    dr.Start("COM3", 9600, 100);
                    dr.OnLineRead += (r, ir, time) =>
                    {
                        Console.Write(".");
                        bw.Write(DateTime.Now.ToFileTime());
                        bw.Write(r);
                        bw.Write(ir);
                        bw.Write((byte)0x69); 
                        //sw.WriteLine($"{DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff")},{r},{ir}");
                        //sw.Flush();
                    };
                    Console.WriteLine("Press ENTER to quit.");
                    Console.ReadLine();
                }
            }
        }
    }
}
