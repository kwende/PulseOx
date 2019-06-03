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
        public Func<double, string> DateTimeFormatter { get; set; }
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

            CartesianMapper<MeasureModel> mapper = Mappers.Xy<MeasureModel>().X(model => model.Time).Y(model => model.Value);

            Spo2 = new ChartValues<MeasureModel>();
            Bpm = new ChartValues<MeasureModel>();
            Accel = new ChartValues<MeasureModel>();

            Charting.For<MeasureModel>(mapper);

            DateTimeFormatter = value => new DateTime((long)value).ToString("HH:mm");

            AxisStep = TimeSpan.FromMilliseconds(1000).Ticks;
            AxisUnit = TimeSpan.TicksPerSecond;

            DataContext = this;

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == true)
            {
                FileReader fr = new FileReader(ofd.FileName);
                FileReader.Result result = fr.Read();

                Spo2.AddRange(result.Spo2List);
                Bpm.AddRange(result.BpmList);
                Accel.AddRange(result.AccelList);
            }
        }
    }
}
