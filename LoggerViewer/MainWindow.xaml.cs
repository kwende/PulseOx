using LiveCharts;
using LiveCharts.Configurations;
using Microsoft.Win32;
using Reader;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
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

namespace LoggerViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public Func<double, string> Formatter { get; set; }
        public double AxisStep { get; set; }

        public ChartValues<MeasureModel> Spo2 { get; set; }
        public ChartValues<MeasureModel> Bpm { get; set; }
        public ChartValues<MeasureModel> Accel { get; set; }

        public double AxisUnit { get; set; }

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();

            CartesianMapper<MeasureModel> mapper = Mappers.Xy<MeasureModel>().X(dayModel => (double)dayModel.DateTime.Ticks / TimeSpan.FromHours(1).Ticks).Y(model => model.Value);

            Spo2 = new ChartValues<MeasureModel>();
            Bpm = new ChartValues<MeasureModel>();
            Accel = new ChartValues<MeasureModel>();

            Charting.For<MeasureModel>(mapper);

            Formatter = value => new System.DateTime((long)(value * TimeSpan.FromHours(1).Ticks)).ToString("t");

            AxisStep = TimeSpan.FromMilliseconds(1000).Ticks;
            AxisUnit = TimeSpan.TicksPerSecond;

            DataContext = this;

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Spo2Chart.DataClick += Spo2Chart_DataClick;
            AccelChart.DataClick += Spo2Chart_DataClick;
            BpmChart.DataClick += Spo2Chart_DataClick; 
        }

        private void Spo2Chart_DataClick(object sender, ChartPoint chartPoint)
        {
            string xAsTime = new System.DateTime((long)(chartPoint.X * TimeSpan.FromHours(1).Ticks)).ToString("t"); 

            MessageBox.Show($"ChartPoint: {xAsTime}, {chartPoint.Y}"); 
        }

        private List<MeasureModel> SmoothList(List<MeasureModel> input, int smoothFactor)
        {
            List<MeasureModel> smoothed = new List<MeasureModel>();
            for(int c=0;c<smoothFactor;c++)
            {
                smoothed.Add(new MeasureModel
                {
                    Time = input[0].Time,
                    DateTime = input[0].DateTime,
                    Value = input[0].Value,
                });
            }

            for (int c = smoothFactor; c < input.Count; c++)
            {
                double sum = 0.0;
                for (int d = c - smoothFactor; d < c; d++)
                {
                    sum += input[d].Value;
                }
                double average = sum / (smoothFactor * 1.0);
                smoothed.Add(new MeasureModel
                {
                    Time = input[c].Time,
                    DateTime = input[c].DateTime,
                    Value = average,
                });
            }
            return smoothed; 
        }

        private void DisplaySmoothed(FileReader.AnalyzedResult result, int smoothFactor)
        {
            Spo2.Clear();
            Bpm.Clear();
            Accel.Clear();

            Spo2.AddRange(SmoothList(result.Spo2List, smoothFactor));
            Bpm.AddRange(SmoothList(result.BpmList, smoothFactor));
            //Spo2.AddRange(result.Spo2List);
            //Bpm.AddRange(result.BpmList);
            Accel.AddRange(result.AccelList);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == true)
            {
                FileReader fr = new FileReader(ofd.FileName);
                this.Title = System.IO.Path.GetFileName(ofd.FileName); 
                FileReader.AnalyzedResult result = fr.Read(true);
                MessageBox.Show($"Bpm Success: {result.BpmReadSuccessCount / (result.TotalRecordGroupsAnalyzed * 1.0) * 100}%"); 
                DisplaySmoothed(result, 5);
            }
        }
    }
}
