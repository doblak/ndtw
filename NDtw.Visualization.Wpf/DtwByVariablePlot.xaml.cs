using System;
using System.Windows;
using System.Windows.Controls;
using OxyPlot;

namespace NDtw.Visualization.Wpf
{
    public partial class DtwByVariablePlot : UserControl
    {
        public DtwByVariablePlot()
        {
            InitializeComponent();
        }

        public static DependencyProperty DtwProperty =
            DependencyProperty.Register(
                "Dtw",
                typeof (IDtw),
                typeof(DtwByVariablePlot),
                new FrameworkPropertyMetadata(null, (d, e) => ((DtwByVariablePlot)d).OnDtwChanged()));

        public IDtw Dtw
        {
            get { return (IDtw) GetValue(DtwProperty); }
            set { SetValue(DtwProperty, value); }
        }

        public void OnDtwChanged()
        {
            var dtwPath = Dtw.GetPath();
            var xLength = Dtw.XLength;
            var yLength = Dtw.YLength;
            var seriesAMultivariate = Dtw.SeriesA;
            var seriesBMultivariate = Dtw.SeriesB;
            var cost = Dtw.GetCost();

            var plotModel = new PlotModel(String.Format("Dtw ({0:0.00})", cost));

            for (int variableIndex = 0; variableIndex < seriesAMultivariate.Length; variableIndex++)
            {
                var axisTitleAndKey = String.Format("Value (var {0})", variableIndex + 1);
                plotModel.Axes.Add(new LinearAxis(AxisPosition.Left, axisTitleAndKey) { Key = axisTitleAndKey, PositionTier = variableIndex} );

                var seriesAVariable = seriesAMultivariate[variableIndex];
                var seriesBVariable = seriesBMultivariate[variableIndex];

                var plotSeriesA = new LineSeries("Series A") { YAxisKey = axisTitleAndKey };
                for (int i = 0; i < xLength; i++)
                    plotSeriesA.Points.Add(new DataPoint(i, seriesAVariable[i]));

                var plotSeriesB = new LineSeries("Series B") { YAxisKey = axisTitleAndKey };
                for (int i = 0; i < yLength; i++)
                    plotSeriesB.Points.Add(new DataPoint(i, seriesBVariable[i]));

                var plotSeriesPath = new LineSeries("Dtw")
                                         {
                                             YAxisKey = axisTitleAndKey,
                                             StrokeThickness = 0.5,
                                             Color = OxyColors.DimGray,
                                         };

                for (int i = 0; i < dtwPath.Length; i++)
                {
                    plotSeriesPath.Points.Add(new DataPoint(dtwPath[i].Item1, seriesAVariable[dtwPath[i].Item1]));
                    plotSeriesPath.Points.Add(new DataPoint(dtwPath[i].Item2, seriesBVariable[dtwPath[i].Item2]));
                    plotSeriesPath.Points.Add(new DataPoint(double.NaN, double.NaN));
                }

                plotModel.Series.Add(plotSeriesA);
                plotModel.Series.Add(plotSeriesB);
                plotModel.Series.Add(plotSeriesPath);
            }

            plotModel.Axes.Add(new LinearAxis(AxisPosition.Bottom, "Index"));
            
            Plot.Model = plotModel;
        }
    }
}
