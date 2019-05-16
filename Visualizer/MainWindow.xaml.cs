using LiveCharts;
using LiveCharts.Configurations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Reader; 

namespace Visualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        //https://lvcharts.net/App/examples/v1/wpf/Constant%20Changes

        private double _axisMax;
        private double _axisMin;


        private DeviceReader _reader; 
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ChartValues<MeasureModel> ChartValuesRedProcessed { get; set; }
        public ChartValues<MeasureModel> ChartValuesIRProcessed { get; set; }

        public ChartValues<MeasureModel> ChartValuesRed { get; set; }
        public ChartValues<MeasureModel> ChartValuesIR { get; set; }
        public Func<double, string> DateTimeFormatter { get; set; }
        public double AxisStep { get; set; }
        public double AxisUnit { get; set; }
        public double AxisMax
        {
            get { return _axisMax; }
            set
            {
                _axisMax = value;
                OnPropertyChanged("AxisMax"); 
            }
        }
        public double AxisMin
        {
            get { return _axisMin; }
            set
            {
                _axisMin = value;
                OnPropertyChanged("AxisMin");
            }
        }

        private void SetAxisLimits(double nowInSeconds)
        {
            AxisMax = nowInSeconds + .1; // lets force the axis to be 1 second ahead
            AxisMin = nowInSeconds - 5; // and 8 seconds behind
        }


        public MainWindow()
        {
            InitializeComponent();

            CartesianMapper<MeasureModel> mapper = Mappers.Xy<MeasureModel>().X(model => model.Time).Y(model => model.Value);
            Charting.For<MeasureModel>(mapper);

            ChartValuesRed = new ChartValues<MeasureModel>();
            ChartValuesIR = new ChartValues<MeasureModel>();

            ChartValuesRedProcessed = new ChartValues<MeasureModel>();
            ChartValuesIRProcessed = new ChartValues<MeasureModel>();

            //lets save the mapper globally.
            Charting.For<MeasureModel>(mapper);

            //lets set how to display the X Labels
            //DateTimeFormatter = value => new DateTime((long)value).ToString("ss");

            //AxisStep forces the distance between each separator in the X axis
            AxisStep = 1; // TimeSpan.FromMilliseconds(50).Ticks;
            //AxisUnit forces lets the axis know that we are plotting seconds
            //this is not always necessary, but it can prevent wrong labeling
            AxisUnit = 1;

            SetAxisLimits(0);

            //The next code simulates data changes every 300 ms

            DataContext = this;

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _reader = new DeviceReader();
            _reader.Start("COM3", 9600, 100);
            _reader.OnLineRead += (r, ir, dt) =>
            {
                if(ChartValuesIR.Count > 100)
                {
                    ChartValuesIR.RemoveAt(0);
                    ChartValuesRed.RemoveAt(0); 
                }

                ChartValuesRed.Add(new MeasureModel
                {
                    Time = dt,
                    Value = r, //r.Next(-8, 10)
                });
                ChartValuesIR.Add(new MeasureModel
                {
                    Time = dt,
                    Value = ir, //r.Next(-8, 10)
                });
                SetAxisLimits(dt);
            };
            _reader.OnBatchCompleted += (r, ir) =>
            {
                SignalProcessor.Mean(ref r, ref ir);
                List<MeasureModel> duplicateR = new List<MeasureModel>(r.Count);
                foreach (MeasureModel m in r)
                {
                    duplicateR.Add(new MeasureModel
                    {
                        Time = m.Time,
                        Value = m.Value
                    });
                }
                SignalProcessor.LineLeveling(ref ir, ref r); 

                ChartValuesRedProcessed.Clear();
                ChartValuesRedProcessed.AddRange(r);

                ChartValuesIRProcessed.Clear();
                ChartValuesIRProcessed.AddRange(duplicateR);

                return; 
            }; 
        }
    }
}
