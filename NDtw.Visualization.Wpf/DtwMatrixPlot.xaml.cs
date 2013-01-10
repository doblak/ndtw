using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using OxyPlot;

namespace NDtw.Visualization.Wpf
{
    public partial class DtwMatrixPlot : UserControl
    {
        public DtwMatrixPlot()
        {
            InitializeComponent();
        }

        public static DependencyProperty DtwProperty =
            DependencyProperty.Register(
                "Dtw",
                typeof (IDtw),
                typeof (DtwMatrixPlot),
                new FrameworkPropertyMetadata(null, (d, e) => ((DtwMatrixPlot)d).OnDataChanged()));

        public IDtw Dtw
        {
            get { return (IDtw) GetValue(DtwProperty); }
            set { SetValue(DtwProperty, value); }
        }

        public static DependencyProperty DrawDistanceProperty =
            DependencyProperty.Register(
                "DrawDistance",
                typeof(bool),
                typeof(DtwMatrixPlot),
                new FrameworkPropertyMetadata(false, (d, e) => ((DtwMatrixPlot)d).OnDataChanged()));

        public bool DrawDistance
        {
            get { return (bool)GetValue(DrawDistanceProperty); }
            set { SetValue(DrawDistanceProperty, value); }
        }

        public static DependencyProperty DrawCostProperty =
            DependencyProperty.Register(
                "DrawCost",
                typeof(bool),
                typeof(DtwMatrixPlot),
                new FrameworkPropertyMetadata(false, (d, e) => ((DtwMatrixPlot)d).OnDataChanged()));

        public bool DrawCost
        {
            get { return (bool)GetValue(DrawCostProperty); }
            set { SetValue(DrawCostProperty, value); }
        }

        public void OnDataChanged()
        {
            if(DrawCost && DrawDistance)
                throw new Exception("Only one of the values can be drawn at once, 'cost' or 'distance'.");
            
            double[][] matrixValues = null; 
            if(DrawCost)
                matrixValues = Dtw.GetCostMatrix();
            if (DrawDistance)
                matrixValues = Dtw.GetDistanceMatrix();

            var dtwPath = Dtw.GetPath();
            var xLength = Dtw.XLength;
            var yLength = Dtw.YLength;
            var cost = Dtw.GetCost();
            var costNormalized = Dtw.GetCost() / Math.Sqrt(xLength * xLength + yLength * yLength);

            var plotModel = new PlotModel(String.Format("Dtw norm by length: {0:0.00}, total: {1:0.00}", costNormalized, cost))
                {
                    LegendTextColor = DrawCost || DrawDistance ? OxyColors.White : OxyColors.Black,
                };

            if (matrixValues != null)
            {
                var maxMatrixValue = 0.0;
                for (int i = 0; i < xLength; i++)
                    for (int j = 0; j < yLength; j++)
                        maxMatrixValue = Math.Max(maxMatrixValue, Double.IsPositiveInfinity(matrixValues[i][j]) ? 0 : matrixValues[i][j]);

                for (int i = 0; i < xLength; i++)
                    for (int j = 0; j < yLength; j++)
                    {
                        var value = matrixValues[i][j];
                        var isValuePositiveInfinity = Double.IsPositiveInfinity(value);

                        var intensityBytes = isValuePositiveInfinity ? new byte[] { 0, 0, 0 } : GetFauxColourRgbIntensity(value, 0, maxMatrixValue);
                        //var intensityByte = (byte)(255 - Math.Floor(255 * intensity));
                        plotModel.Annotations.Add(new PolygonAnnotation
                        {
                            Points =
                                new[]
                                {
                                    new DataPoint(i - 0.5, j - 0.5), new DataPoint(i + 0.5, j - 0.5),
                                    new DataPoint(i + 0.5, j + 0.5), new DataPoint(i - 0.5, j + 0.5),
                                },
                            StrokeThickness = 0,
                            Selectable = false,
                            Layer = AnnotationLayer.BelowAxes,
                            Fill = OxyColor.FromArgb(255, intensityBytes[0], intensityBytes[1], intensityBytes[2]),
                        });
                    }

                for (int i = 0; i < 30; i++)
                {
                    var intensityBytes = GetFauxColourRgbIntensity(i, 0, 29);

                    plotModel.Annotations.Add(new RectangleAnnotation
                    {
                        MinimumX = -39,
                        MaximumX = -25,
                        MinimumY = -i - 6,
                        MaximumY = -i - 5,
                        Selectable = false,
                        Fill = OxyColor.FromArgb(255, intensityBytes[0], intensityBytes[1], intensityBytes[2])
                    });                       
                }

                plotModel.Annotations.Add(new TextAnnotation
                {
                    Position = new DataPoint(-24, -5),
                    HorizontalAlignment = HorizontalTextAlign.Left,
                    VerticalAlignment = VerticalTextAlign.Middle,
                    StrokeThickness = 0,
                    Text = "0"
                });

                plotModel.Annotations.Add(new TextAnnotation
                {
                    Position = new DataPoint(-24, -34),
                    HorizontalAlignment = HorizontalTextAlign.Left,
                    VerticalAlignment = VerticalTextAlign.Middle,
                    StrokeThickness = 0,
                    Text = String.Format("{0:0.00}", maxMatrixValue),
                });
            }

            var matrixPathSeries = new LineSeries("Path")
            {
                StrokeThickness = 1,
                Color = OxyColors.Red,
            };

            for (int i = 0; i < dtwPath.Length; i++)
                matrixPathSeries.Points.Add(new DataPoint(dtwPath[i].Item1, dtwPath[i].Item2));

            plotModel.Series.Add(matrixPathSeries);

            var seriesMatrixScale = (xLength + yLength) * 0.05;

            for (int variableIndex = 0; variableIndex < Dtw.SeriesVariables.Length; variableIndex++)
            {
                var variableA = Dtw.SeriesVariables[variableIndex];
                var variableASeries = variableA.OriginalXSeries;
                var variableB = Dtw.SeriesVariables[variableIndex];
                var variableBSeries = variableB.OriginalYSeries;

                var minSeriesA = variableASeries.Min();
                var maxSeriesA = variableASeries.Max();
                var normalizedSeriesA = variableASeries.Select(x => (x - minSeriesA) / (maxSeriesA - minSeriesA)).ToList();
                var matrixSeriesA = new LineSeries(variableA.VariableName);

                for (int i = 0; i < normalizedSeriesA.Count; i++)
                    matrixSeriesA.Points.Add(new DataPoint(i, (-1 + normalizedSeriesA[i]) * seriesMatrixScale - 1 - seriesMatrixScale * (variableIndex + 1)));

                plotModel.Series.Add(matrixSeriesA);

                var minSeriesB = variableBSeries.Min();
                var maxSeriesB = variableBSeries.Max();
                var normalizedSeriesB = variableBSeries.Select(x => (x - minSeriesB) / (maxSeriesB - minSeriesB)).ToList();
                var matrixSeriesB = new LineSeries(variableB.VariableName);

                for (int i = 0; i < normalizedSeriesB.Count; i++)
                    matrixSeriesB.Points.Add(new DataPoint( -normalizedSeriesB[i] * seriesMatrixScale - 1 - seriesMatrixScale * (variableIndex + 1), i));

                plotModel.Series.Add(matrixSeriesB);
            }

            plotModel.Axes.Add(new LinearAxis(AxisPosition.Bottom, "           Series A") { Maximum = Math.Max(xLength, yLength), PositionAtZeroCrossing = true});
            plotModel.Axes.Add(new LinearAxis(AxisPosition.Left, "                  Series B") { Maximum = Math.Max(xLength, yLength), PositionAtZeroCrossing = true });
            
            MatrixPlot.Model = plotModel;
        }

        /// <summary>
        /// Generate heatmap color
        /// </summary>
        /// <remarks>Thanks to Eddie Yee Tak Ma: http://eddiema.ca/2011/01/21/c-sharp-heatmaps/ </remarks>
        public static byte[] GetFauxColourRgbIntensity(double val, double min, double max)
        {
            byte r = 0;
            byte g = 0;
            byte b = 0;
            val = (val - min) / (max - min);
            if (val <= 0.2)
            {
                b = (byte)((val / 0.2) * 255);
            }
            else if (val > 0.2 && val <= 0.7)
            {
                b = (byte)((1.0 - ((val - 0.2) / 0.5)) * 255);
            }
            if (val >= 0.2 && val <= 0.6)
            {
                g = (byte)(((val - 0.2) / 0.4) * 255);
            }
            else if (val > 0.6 && val <= 0.9)
            {
                g = (byte)((1.0 - ((val - 0.6) / 0.3)) * 255);
            }
            if (val >= 0.5)
            {
                r = (byte)(((val - 0.5) / 0.5) * 255);
            }
            return new byte[] { r, g, b };
        }

        /// <summary>
        /// Generate heatmap color (grayscale)
        /// </summary>
        /// <remarks>Thanks to Eddie Yee Tak Ma: http://eddiema.ca/2011/01/21/c-sharp-heatmaps/ </remarks>
        public static byte[] GetGrayscaleRgbIntensity(double val, double min, double max)
        {
            byte y;
            val = (val - min) / (max - min);
            y = (byte)((1.0 - val) * 255);
            return new [] { y, y, y };
        }

        private const double AspectRatio = 1;
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            //keep aspect ratio
            if (sizeInfo.NewSize.Width / sizeInfo.NewSize.Height > AspectRatio)
            {
                MatrixPlot.Width = sizeInfo.NewSize.Height * AspectRatio;
                MatrixPlot.Height = MatrixPlot.Width / AspectRatio;
            }
            else
            {
                MatrixPlot.Height = sizeInfo.NewSize.Width / AspectRatio;
                MatrixPlot.Width = MatrixPlot.Height * AspectRatio;
            }
        }
    }
}
