using Fuzzy_C_Means;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
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
using static Fuzzy_C_Means.D3Writer;

namespace PlotViewer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CSVFile file;
        CMeansAlghoritm cMeans;
        public double R { get; set; }
        public int K { get; set; }
        public double M { get; set; }

        List<CheckBox> checkBoxes = new List<CheckBox>();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            //Создание графика
            var plotModel = new PlotModel();
            var linearAxisX = new LinearAxis { Position = AxisPosition.Bottom };
            linearAxisX.Title = "X Axis";
            plotModel.Axes.Add(linearAxisX);

            var linearAxisY = new LinearAxis();
            linearAxisY.Title = "Y Axis";
            plotModel.Axes.Add(linearAxisY);

            this.plot.Model = plotModel;
        }

        private void ImportCSVBittonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                string FilePath = openFileDialog.FileName;

                file = CSVReader.ReadData(FilePath);
            }

            GenerateCheckboxex(file.Headers);

            ButtonStartClick(null, null);
        }

        private void ButtonStartClick(object sender, RoutedEventArgs e)
        {
            K = Int32.Parse(textBoxK.Text);
            R = Double.Parse(textBoxR.Text.Replace('.', ','));
            M = Double.Parse(textBoxM.Text.Replace('.', ','));

            if (file != null)
            {

                cMeans = new CMeansAlghoritm(new ClusteringConfig()
                {
                    ClusterCount = K,
                    Epsilon = 0.0001,
                    ExponentialMass = M,
                    Matrix = file.Matrix,
                    MaxSteps = 100,
                    R = R,
                    CSVFile = file
                });

                //foreach (List<double> point in file.Matrix)
                //{
                //    //отображение точек на графике
                //    PointAnnotation annotation = new PointAnnotation { Shape = MarkerType.Circle, X = point[0], Y = point[1] };
                //    this.plot.Model.Annotations.Add(annotation);
                //}

                //plot.InvalidatePlot();

                cMeans.Start();
                this.plot.Model.Annotations.Clear();

                var check = checkBoxes.Where(c => c.IsChecked.Value);

                int index1 = 0;
                int index2 = 1;

                if (check.Count() == 2)
                {
                    index1 = Int32.Parse(checkBoxes.First(c => c.IsChecked.Value).Name.Split('c').Last());
                    index2 = Int32.Parse(checkBoxes.Last(c => c.IsChecked.Value).Name.Split('c').Last());
                }

                checkBoxes[index1].IsChecked = true;
                checkBoxes[index2].IsChecked = true;

                //сопоставить точка - кластер
                List<int> clusterNumbers = cMeans.DetectClusters();

                //найти центры
                var centers = cMeans.GetCenters(index1, index2);

                var c1 = cMeans.GetColumn(index1);
                var c2 = cMeans.GetColumn(index2);

                this.plot.Model.Axes[0].Title = file.Headers[index1+1];
                this.plot.Model.Axes[1].Title = file.Headers[index2+1];

                DrawPlot(c1, c2, clusterNumbers, centers, file.Names);
            }
        }

        private void GenerateCheckboxex(List<string> headers)
        {
            BoxStackPanel.Children.Clear();
            checkBoxes.Clear();

            for (var i = 1; i < headers.Count; i++)
            {
                CheckBox cb = new CheckBox()
                {
                    Name = $"c{ i - 1}",
                    Content = headers[i],
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
                };
                cb.Click += ChangeDataRows;
                BoxStackPanel.Children.Add(cb);
                checkBoxes.Add(cb);
            }
        }

        private void ChangeDataRows(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;

            if (checkBoxes.Count(c => c.IsChecked.Value) == 0)
            {
                return;
            }

            if (checkBoxes.Count(c => c.IsChecked.Value) > 2)
            {
                checkBox.IsChecked = false;
            }

            int index1 = Int32.Parse(checkBoxes.First(c => c.IsChecked.Value).Name.Split('c').Last());
            int index2 = Int32.Parse(checkBoxes.Last(c => c.IsChecked.Value).Name.Split('c').Last());

            if (index1 == index2)
            {
                return;
            }

            //проверить выбрано ли 2

            //взять столбцы из Х
            var c1 = cMeans.GetColumn(index1);
            var c2 = cMeans.GetColumn(index2);

            //сопоставить точка - кластер
            List<int> clusterNumbers = cMeans.DetectClusters();

            //найти центры
            var centers = cMeans.GetCenters(index1, index2);

            this.plot.Model.Axes[0].Title = file.Headers[index1+1];
            this.plot.Model.Axes[1].Title = file.Headers[index2+1];

            //сгенерировать цвета
            //вывести
            DrawPlot(c1, c2, clusterNumbers, centers, file.Names);
        }

        private void DrawClusterLegend(Dictionary<int, OxyColor> colors)
        {
            BoxStackPanelClusters.Children.Clear();
            for (var i = 0; i < colors.Count; i++)
            {
                BoxStackPanelClusters.Children.Add(new Label()
                {
                    Background = new SolidColorBrush(Color.FromArgb(255, colors[i].R, colors[i].G, colors[i].B)),
                    Content = $"Кластер {i}",
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
                });
            }
        }

        private void DrawPlot(List<double> X, List<double> Y, List<int> clusters, List<List<double>> centers, List<string> names)
        {
            this.plot.Model.Annotations.Clear();

            Dictionary<int, OxyColor> colors = GenerateColors(centers.Count);

            DrawClusterLegend(colors);

            DrawCenters(centers, colors);

            for (var i = 0; i < X.Count; i++)
            {
                //отображение точек на графике
                PointAnnotation annotation = new PointAnnotation
                {
                    Shape = MarkerType.Circle,
                    X = X[i],
                    Y = Y[i],
                    Fill = colors[clusters[i]],
                    ToolTip = $"{X[i]} {Y[i]}; Кластер {clusters[i]}; Объект - {names[i]}"
                };

                this.plot.Model.Annotations.Add(annotation);
            }

            plot.InvalidatePlot();
        }

        private void DrawCenters(List<List<double>> centers, Dictionary<int, OxyColor> colors)
        {
            for (var i = 0; i < centers.Count; i++)
            {
                //for (var j = 0; j < centers[i].Count; j++)
                {
                    PointAnnotation annotation = new PointAnnotation
                    {
                        Shape = MarkerType.Triangle,
                        X = centers[i][0],
                        Y = centers[i][1],
                        Fill = colors[i],
                        Size = 15
                    };
                    this.plot.Model.Annotations.Add(annotation);
                }
            }
        }

        private Dictionary<int, OxyColor> GenerateColors(int N)
        {
            Random random = new Random();
            Dictionary<int, OxyColor> colors = new Dictionary<int, OxyColor>();
            for (var i = 0; i < N; i++)
            {
                colors.Add(i, OxyColor.FromRgb((byte)random.Next(0, 255), (byte)random.Next(0, 255), (byte)random.Next(0, 255)));
            }
            return colors;
        }
    }
}
