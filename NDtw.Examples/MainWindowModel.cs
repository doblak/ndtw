using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using NDtw.Examples.Infrastructure;
using NDtw.Examples.Infrastructure.MultiSelect;

namespace NDtw.Examples
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            InitializeData();
            _selectedEntities.CollectionChanged += (sender, e) => Recalculate();
            _selectedVariables.CollectionChanged += (sender, e) => Recalculate();
        }

        private void Recalculate()
        {
            if (!CanRecalculate)
                return;

            var seriesAMultivariate = new List<IList<double>>();
            foreach (var selectedVariable in SelectedVariables)
                seriesAMultivariate.Add(DataSeries.GetValues(_selectedEntities[0], selectedVariable));

            var seriesBMultivariate = new List<IList<double>>();
            foreach (var selectedVariable in SelectedVariables)
                seriesBMultivariate.Add(DataSeries.GetValues(_selectedEntities[1], selectedVariable));

            var seriesAMultivariateArray = seriesAMultivariate.Select(x => x.ToArray()).ToArray();
            var seriesBMultivariateArray = seriesBMultivariate.Select(x => x.ToArray()).ToArray();

            var dtw = new Dtw(
                seriesAMultivariateArray, 
                seriesBMultivariateArray,
                _selectedDistanceMeasure.Value,
                UseBoundaryConstraintStart,
                UseBoundaryConstraintEnd,
                UseSlopeConstraint ? SlopeConstraintDiagonal : (int?)null,
                UseSlopeConstraint ? SlopeConstraintAside : (int?)null, 
                UseSakoeChibaMaxShift ? SakoeChibaMaxShift : (int?)null);

            if (MeasurePerformance)
            {
                var swDtwPerformance = new Stopwatch();
                swDtwPerformance.Start();

                for (int i = 0; i < 250; i++)
                {
                    var tempDtw = new Dtw(
                        seriesAMultivariateArray,
                        seriesBMultivariateArray,
                        _selectedDistanceMeasure.Value,
                        UseBoundaryConstraintStart,
                        UseBoundaryConstraintEnd,
                        UseSlopeConstraint ? SlopeConstraintDiagonal : (int?)null,
                        UseSlopeConstraint ? SlopeConstraintAside : (int?)null,
                        UseSakoeChibaMaxShift ? SakoeChibaMaxShift : (int?)null);
                    var tempDtwPath = tempDtw.GetCost();
                }
                swDtwPerformance.Stop();
                OperationDuration = swDtwPerformance.Elapsed;
            }
            Dtw = dtw;
        }

        public MultivariateDataSeriesRepository DataSeries { get; set; }

        private void InitializeData()
        {
            DataSeries = DataSeriesFactory.CreateMultivariateConsumptionByPurposeEurostat();

            Entities = new ObservableCollection<string>(DataSeries.GetEntities());
            SelectedEntities.Add(Entities[0]);
            SelectedEntities.Add(Entities[1]);

            Variables = new ObservableCollection<string>(DataSeries.GetVariables());
            SelectedVariables.Add(Variables[0]);

            DistanceMeasures = new ObservableCollection<DistanceMeasure>()
                                   {
                                       DistanceMeasure.Manhattan, 
                                       DistanceMeasure.Euclidean, 
                                       DistanceMeasure.SquaredEuclidean
                                   };
            
            SelectedDistanceMeasure = DistanceMeasure.Euclidean;
        }

        public ObservableCollection<string> Entities { get; private set; }
        private readonly ObservableCollection<string> _selectedEntities = new ObservableCollection<string>();
        public ObservableCollection<string> SelectedEntities
        {
            get { return _selectedEntities; }
        }

        public ObservableCollection<string> Variables { get; private set; }
        private readonly ObservableCollection<string> _selectedVariables = new ObservableCollection<string>();
        public ObservableCollection<string> SelectedVariables
        {
            get { return _selectedVariables; }
        }

        public ObservableCollection<DistanceMeasure> DistanceMeasures { get; private set; }
        
        private DistanceMeasure? _selectedDistanceMeasure;
        public DistanceMeasure? SelectedDistanceMeasure
        {
            get { return _selectedDistanceMeasure; }
            set
            {
                _selectedDistanceMeasure = value;
                NotifyPropertyChanged(() => SelectedDistanceMeasure);
                Recalculate();
            }
        }

        private bool _useBoundaryConstraintStart = true;
        public bool UseBoundaryConstraintStart
        {
            get
            {
                return _useBoundaryConstraintStart;
            }
            set
            {
                _useBoundaryConstraintStart = value;
                NotifyPropertyChanged(() => UseBoundaryConstraintStart);
                Recalculate();
            }
        }

        private bool _useBoundaryConstraintEnd = true;
        public bool UseBoundaryConstraintEnd
        {
            get
            {
                return _useBoundaryConstraintEnd;
            }
            set
            {
                _useBoundaryConstraintEnd = value;
                NotifyPropertyChanged(() => UseBoundaryConstraintEnd);
                Recalculate();
            }
        }

        private bool _useSakoeChibaMaxShift = true;
        public bool UseSakoeChibaMaxShift
        {
            get
            {
                return _useSakoeChibaMaxShift;
            }
            set
            {
                _useSakoeChibaMaxShift = value;
                NotifyPropertyChanged(() => UseSakoeChibaMaxShift);
                Recalculate();
            }
        }

        private int _sakoeChibaMaxShift = 50;
        public int SakoeChibaMaxShift
        {
            get
            {
                return _sakoeChibaMaxShift;
            }
            set
            {
                _sakoeChibaMaxShift = value;
                NotifyPropertyChanged(() => SakoeChibaMaxShift);
            }
        }

        private bool _useSlopeConstraint = true;
        public bool UseSlopeConstraint
        {
            get
            {
                return _useSlopeConstraint;
            }
            set
            {
                _useSlopeConstraint = value;
                NotifyPropertyChanged(() => UseSlopeConstraint);
                Recalculate();
            }
        }

        private int _slopeConstraintDiagonal = 1;
        public int SlopeConstraintDiagonal
        {
            get
            {
                return _slopeConstraintDiagonal;
            }
            set
            {
                _slopeConstraintDiagonal = value;
                NotifyPropertyChanged(() => SlopeConstraintDiagonal);
            }
        }

        private int _slopeConstraintAside = 1;
        public int SlopeConstraintAside
        {
            get
            {
                return _slopeConstraintAside;
            }
            set
            {
                _slopeConstraintAside = value;
                NotifyPropertyChanged(() => SlopeConstraintAside);
            }
        }

        private bool _measurePerformance;
        public bool MeasurePerformance
        {
            get
            {
                return _measurePerformance;
            }
            set
            {
                _measurePerformance = value;
                NotifyPropertyChanged(() => MeasurePerformance);
            }
        }

        private TimeSpan _operationDuration;
        public TimeSpan OperationDuration
        {
            get
            {
                return _operationDuration;
            }
            private set
            {
                _operationDuration = value;
                NotifyPropertyChanged(() => OperationDuration);
            }
        }

        private IDtw _dtw;
        public IDtw Dtw
        {
            get
            {
                return _dtw;
            }
            private set
            {
                _dtw = value;
                NotifyPropertyChanged(() => Dtw);
            }
        }

        private bool _drawDistance;
        public bool DrawDistance
        {
            get
            {
                return _drawDistance;
            }
            set
            {
                _drawDistance = value;
                if (value && DrawCost)
                    DrawCost = false;

                NotifyPropertyChanged(() => DrawDistance);
            }
        }

        private bool _drawCost;
        public bool DrawCost
        {
            get
            {
                return _drawCost;
            }
            set
            {
                _drawCost = value;
                if (value && DrawDistance)
                    DrawDistance = false;

                NotifyPropertyChanged(() => DrawCost);
            }
        }


        public bool CanRecalculate
        {
            get { return _selectedEntities.Count == 2 && _selectedVariables.Count >= 1 && _selectedDistanceMeasure != null; }
        }

        private ICommand _recalculateCommand;
        public ICommand RecalculateCommand
        {
            get
            {
                if (_recalculateCommand == null)
                {
                    _recalculateCommand = new RelayCommand(
                        param => Recalculate(),
                        param => CanRecalculate
                    );
                }
                return _recalculateCommand;
            }
        }
    }
}
 