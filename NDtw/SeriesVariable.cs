using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NDtw.Preprocessing;

namespace NDtw
{
    public class SeriesVariable
    {
        private readonly double[] _x;
        private readonly double[] _y;
        private readonly string _variableName;
        private readonly IPreprocessor _preprocessor;
        private readonly double _weight;

        public SeriesVariable(double[] x, double[] y, string variableName = null, IPreprocessor preprocessor = null, double weight = 1)
        {
            _x = x;
            _y = y;
            _variableName = variableName;
            _preprocessor = preprocessor;
            _weight = weight;
        }

        public string VariableName
        {
            get { return _variableName; }
        }

        public double Weight
        {
            get { return _weight; }
        }

        public double[] OriginalXSeries
        {
            get { return _x; }
        }

        public double[] OriginalYSeries
        {
            get { return _y; }
        }

        public double[] GetPreprocessedXSeries()
        {
            if (_preprocessor == null)
                return _x;

            return _preprocessor.Preprocess(_x);
        }

        public double[] GetPreprocessedYSeries()
        {
            if (_preprocessor == null)
                return _y;

            return _preprocessor.Preprocess(_y);
        }
    }
}
