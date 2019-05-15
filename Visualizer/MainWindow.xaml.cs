﻿using LiveCharts;
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

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ChartValues<MeasureModel> ChartValues { get; set; }
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

        private void SetAxisLimits(DateTime now)
        {
            AxisMax = now.Ticks + TimeSpan.FromMilliseconds(100).Ticks; // lets force the axis to be 1 second ahead
            AxisMin = now.Ticks - TimeSpan.FromSeconds(5).Ticks; // and 8 seconds behind
        }


        public MainWindow()
        {
            InitializeComponent();

            CartesianMapper<MeasureModel> mapper = Mappers.Xy<MeasureModel>().X(model => model.DateTime.Ticks).Y(model => model.Value);
            Charting.For<MeasureModel>(mapper);

            ChartValues = new ChartValues<MeasureModel>();
            //lets save the mapper globally.
            Charting.For<MeasureModel>(mapper);

            //the values property will store our values array
            ChartValues = new ChartValues<MeasureModel>();

            //lets set how to display the X Labels
            DateTimeFormatter = value => new DateTime((long)value).ToString("ss");

            //AxisStep forces the distance between each separator in the X axis
            AxisStep = TimeSpan.FromMilliseconds(50).Ticks;
            //AxisUnit forces lets the axis know that we are plotting seconds
            //this is not always necessary, but it can prevent wrong labeling
            AxisUnit = TimeSpan.TicksPerSecond;

            SetAxisLimits(DateTime.Now);

            //The next code simulates data changes every 300 ms

            DataContext = this;

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                using (StreamReader sr = new StreamReader("C:/users/brush/desktop/test.txt"))
                {
                    while(!sr.EndOfStream)
                    {
                        string line = sr.ReadLine().Trim(); 
                        string sValue = line.Split(' ')[0]; 
                        double value = double.Parse(sValue);

                        Thread.Sleep(50);
                        var now = DateTime.Now;

                        //_trend += r.Next(-8, 10);

                        ChartValues.Add(new MeasureModel
                        {
                            DateTime = now,
                            Value = value, //r.Next(-8, 10)
                        });

                        SetAxisLimits(now);

                        //lets only use the last 150 values
                        if (ChartValues.Count > 150) ChartValues.RemoveAt(0);
                    }
                }
               
            }); 
        }
    }
}