using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Fuzzy_C_Means.D3Writer;

namespace Fuzzy_C_Means
{
    public class CMeansAlghoritm
    {
        private static Random random = new Random();
        ClusteringConfig config;

        /// <summary>
        /// Число элементов сравнения
        /// </summary>
        private int N { get; set; }

        private double M { get; set; }

        private int K { get; set; }

        private double Eps { get; set; }

        private int MaxSteps { get; set; }

        private double R { get; set; }

        private CSVFile CSVFile { get; set; }


        public List<List<double>> Matrix { get; set; }

        List<List<double>> Weights = null;
        List<List<double>> Centroids = null;

        public CMeansAlghoritm(ClusteringConfig config)
        {
            this.config = config;
            this.N = config.N;
            this.Matrix = config.Matrix;
            this.M = config.ExponentialMass;
            this.K = config.ClusterCount;
            this.MaxSteps = config.MaxSteps;
            this.Eps = config.Epsilon;
            this.R = config.R;
            this.CSVFile = config.CSVFile;
        }

        public void Start()
        {
            int Step = 0;
            ExcelReport.AddReport(Matrix, "Матрица данных");

            List<List<double>> InitWeights = this.InitWeights(K, N);
            ExcelReport.AddReport(InitWeights, "Инициализация матрицы весов");

            //Matrix = NormalizeMatrix(Matrix);
            //ExcelReport.AddReport(Matrix, "Нормализация");

            //Weights = CalcWeightMatrix(Matrix);
            //ExcelReport.AddReport(Weights, "Матрица весов");

            List<List<double>> InitCentroids = GenerateCentroids(K, Matrix);
            ExcelReport.AddReport(InitCentroids, $"Генерация центров {K} кластеров");

            List<List<double>> PreviousWeights = InitWeights;
            //List<List<double>> Weights = null;
            //List<List<double>> Centroids = null;
            bool isStop = false;
            do
            {
                Step++;
                ExcelReport.AddReport(Step, $"Шаг {Step}");

                Centroids = RecalcCentroids(Weights ?? InitWeights, Matrix, K, M);
                ExcelReport.AddReport(Centroids, $"Пересчет центров");

                PreviousWeights = Weights ?? InitWeights;
                Weights = RecalcWeights(Centroids, Matrix, M);
                ExcelReport.AddReport(Weights, $"Пересчет весов");

                isStop = IsStop(PreviousWeights, Weights, Eps, Step, out double maxDelta);
                ExcelReport.AddReport(maxDelta, $"Максимальная разница между шагами");

            } while (!isStop);

            ExcelReport.AddReport(GetClusterLists(), "Группировка по кластерам");
            ExcelReport.WriteReports();
        }

        /// <summary>
        /// Инициализация матрицы весов
        /// </summary>
        /// <param name="K">Числов кластеров</param>
        /// <param name="N">Число точек</param>
        /// <returns></returns>
        private List<List<double>> InitWeights(int K, int N)
        {
            List<List<double>> Weights = new List<List<double>>();

            for (var i = 0; i < K; i++)
            {
                Weights.Add(new List<double>(new double[N]));
            }

            for (var i = 0; i < K; i++)
            {
                //Weights.Add(new List<double>());
                for (var j = 0; j < N; j++)
                {
                    double currentSum = Weights.Sum(w => w[j]);
                    Weights[i][j] = random.NextDouble() * (1 - currentSum) + 0;
                    if (i == K - 1)
                    {
                        Weights[i][j] = 1 - currentSum;
                    }

                }
            }

            return Weights;
        }

        /// <summary>
        /// Нормализовать матрицу
        /// </summary>
        /// <param name="Matrix"></param>
        private List<List<double>> NormalizeMatrix(List<List<double>> Matrix)
        {
            List<double> sqrAverages = new List<double>();
            List<double> averages = new List<double>();

            if (!Matrix.Any())
            {
                return null;
            }

            for (var i = 0; i < Matrix.First().Count; i++)
            {
                List<double> column = new List<double>();
                for (var j = 0; j < Matrix.Count; j++)
                {
                    column.Add(Matrix[j][i]);
                }
                averages.Add(column.Average());
                sqrAverages.Add(SqrAverage(column));
            }

            List<List<double>> NormalizedMatrix = new List<List<double>>();
            for (var i = 0; i < Matrix.Count; i++)
            {
                NormalizedMatrix.Add(new List<double>());
                for (var j = 0; j < Matrix[i].Count; j++)
                {
                    double z = (Matrix[i][j] - averages[j]) / sqrAverages[j];
                    NormalizedMatrix[i].Add(z);
                }
            }

            return NormalizedMatrix;
        }

        /// <summary>
        /// Среднее квадратичное отклонение
        /// </summary>
        private double SqrAverage(List<double> arr)
        {
            double AvgSum = arr.Average();
            double Sigma = 0;
            for (var i = 0; i < arr.Count; i++)
            {
                Sigma += Math.Pow((arr[i] - AvgSum), 2);
            }
            return Math.Pow((Sigma / arr.Count), 0.5);
        }

        /// <summary>
        /// Расчет матрицы весов
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        private List<List<double>> CalcWeightMatrix(List<List<double>> points)
        {
            List<List<double>> D = new List<List<double>>();
            for (var i = 0; i < points.Count; i++)
            {
                var d = new List<double>();
                D.Add(d);
                for (var j = 0; j < points.Count; j++)
                {
                    double distance = i == j ? 0 : Distance(points[i], points[j], R);
                    d.Add(distance);
                }
            }
            return D;
        }

        /// <summary>
        /// Начальное распределине центров кластеров
        /// </summary>
        /// <param name="K">Число кластеров</param>
        /// <param name="Matrix">Исходная матрица Х с данными</param>
        /// <returns></returns>
        private List<List<double>> GenerateCentroids(int K, List<List<double>> Matrix)
        {
            int pointSize = Matrix.First().Count;
            List<List<double>> Centroids = new List<List<double>>();

            for (var k = 0; k < K; k++)
            {
                var clusterCenter = new List<double>();
                int randomIndex = random.Next(0, Matrix.Count - 1);
                clusterCenter = Matrix[randomIndex].ToList();
                for (var i = 0; i < clusterCenter.Count; i++)
                {
                    double randK = random.NextDouble();
                    clusterCenter[i] *= randK;
                }
                Centroids.Add(clusterCenter);
            }

            return Centroids;
        }

        /// <summary>
        /// Расчет расстояния
        /// </summary>
        /// <param name="p1">Точка</param>
        /// <param name="p2">Точка</param>
        /// <param name="R">2 = Евклидово</param>
        /// <param name="W">Вес</param>
        /// <returns></returns>
        private double Distance(List<double> p1, List<double> p2, double R = 2, double W = 1)
        {
            double distance = 0;
            if (p1.Any() && p2.Any() && p1.Count == p2.Count)
            {
                for (int i = 0; i < p1.Count; i++)
                {
                    distance += Math.Pow(W * (p1[i] - p2[i]), R);
                }
            }
            distance = Math.Pow(distance, 1.0 / R);

            return distance;
        }

        /// <summary>
        /// Условие останова
        /// </summary>
        /// <param name="PreviousU">Матрица весов K+1 шага</param>
        /// <param name="CurrentU">Матрица весов K шага</param>
        /// <param name="eps">Порог</param>
        /// <returns></returns>
        private bool IsStop(List<List<double>> PreviousU, List<List<double>> CurrentU, double eps, double step, out double maxDelta)
        {
            maxDelta = Double.MinValue;
            if (step >= MaxSteps)
            {
                return true;
            }

            for (var i = 0; i < PreviousU.Count; i++)
            {
                for (var j = 0; j < PreviousU[i].Count; j++)
                {
                    var delta = Math.Abs(CurrentU[i][j] - PreviousU[i][j]);
                    if (delta > maxDelta)
                    {
                        maxDelta = delta;
                    }
                }
            }
            if (maxDelta < eps)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Пересчет центров класетра
        /// </summary>
        /// <param name="U">Матрица весов</param>
        /// <param name="X">Матрица данных точек</param>
        /// <param name="K">Число кластеров</param>
        /// <param name="M">К-т фазификации</param>
        /// <returns></returns>
        private List<List<double>> RecalcCentroids(List<List<double>> U, List<List<double>> X, int K, double M)
        {
            List<List<double>> Centroids = new List<List<double>>();

            for (var k = 0; k < K; k++)
            {
                Centroids.Add(new List<double>());

                List<double> DividendSum = new List<double>(new double[X.First().Count]);
                List<double> DividerSum = new List<double>(new double[X.First().Count]);

                for (var i = 0; i < X.Count; i++)
                {
                    //List<double> Point = new List<double>();

                    for (var j = 0; j < X[i].Count; j++)
                    {
                        double val = Math.Pow(U[k][i], M) * X[i][j];
                        //Point.Add(val);
                        DividendSum[j] += val;
                        DividerSum[j] += Math.Pow(U[k][i], M);
                    }
                }

                Centroids[k] = DividendSum.DivideBy(DividerSum);
            }

            return Centroids;
        }

        /// <summary>
        /// Пересчет матрицы весов
        /// </summary>
        /// <param name="C">Кластеры</param>
        /// <param name="X">Точки</param>
        /// <param name="M">К-т фаззификации</param>
        /// <returns></returns>
        private List<List<double>> RecalcWeights(List<List<double>> C, List<List<double>> X, double M)
        {
            List<List<double>> Weights = new List<List<double>>();

            for (var i = 0; i < C.Count; i++)
            {
                Weights.Add(new List<double>());
                for (var j = 0; j < X.Count; j++)
                {
                    double Sum = 0;

                    for (var k = 0; k < C.Count; k++)
                    {
                        var f1 = Distance(X[j], C[i], R);
                        var f2 = Distance(X[j], C[k], R);
                        Sum += f1 / f2;
                    }

                    Sum = 1.0 / Math.Pow(Sum, (2 / (M - 1)));
                    Weights[i].Add(Sum);
                }
            }

            return Weights;
        }

        public List<double> GetColumn(int index)
        {
            return Matrix.Select(l => l[index]).ToList();
        }

        public List<List<double>> GetCenters(int index1, int index2)
        {
            List<List<double>> centers = new List<List<double>>();

            foreach (var c in Centroids)
            {
                var i1 = c[index1];
                var i2 = c[index2];
                centers.Add(
                    new List<double>() { i1, i2 });
            }

            return centers;
        }

        public List<int> DetectClusters()
        {
            List<int> clusterNumbers = new List<int>(new int[Weights.First().Count]);
            List<double> maxes = new List<double>(new double[Weights.First().Count]);

            maxes.ForEach(e => e = Double.MinValue);

            for (var j = 0; j < Weights.First().Count; j++)
            {
                for (var i = 0; i < Weights.Count; i++)
                {
                    if (Weights[i][j] > maxes[j])
                    {
                        maxes[j] = Weights[i][j];
                        clusterNumbers[j] = i;
                    }
                }
            }

            return clusterNumbers;
        }

        private Dictionary<int, List<string>> GetClusterLists()
        {
            Dictionary<int, List<string>> clusterNames = new Dictionary<int, List<string>>();

            var clusters = DetectClusters();
            var names = CSVFile.Names;

            for (var i = 0; i < clusters.Count; i++)
            {
                if (clusterNames.ContainsKey(clusters[i]))
                {
                    clusterNames[clusters[i]].Add(names[i]);
                }
                else
                {
                    clusterNames.Add(clusters[i], new List<string>() { names[i] });
                }
            }
            return clusterNames;
        }

        public NodeD3 MakeNodes()
        {
            return new NodeD3()
            {
                Name = "Zero",
                Value = 0,
                Children = new List<NodeD3>()
                {
                    new NodeD3()
                    {
                        Name = "Sec",
                        Value = 2,
                        Children = new List<NodeD3>()
                    },
                    new NodeD3()
                    {
                        Name = "Th",
                        Value = 2,
                        Children = new List<NodeD3>()
                    }
                }
            };
        }
    }

    public class ClusteringConfig
    {
        public int ClusterCount { get; set; }

        public double ExponentialMass { get; set; }

        public double Epsilon { get; set; }

        public int MaxSteps { get; set; }

        public List<List<double>> Matrix { get; set; }

        public int N { get => Matrix.Count; }

        public double R;

        public CSVFile CSVFile { get; set; }
    }
}
