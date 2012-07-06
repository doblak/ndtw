using System.Linq;

namespace NDtw.Preprocessing
{
    public class NormalizationPreprocessor : IPreprocessor
    {
        private readonly double _minBoundary;
        private readonly double _maxBoundary;

        /// <summary>
        /// Initialize to use normalization to range [0, 1]
        /// </summary>
        public NormalizationPreprocessor() : this (0, 1) { }

        /// <summary>
        /// Initialize to use normalization to range [minBoundary, maxBoundary]
        /// </summary>
        public NormalizationPreprocessor(double minBoundary, double maxBoundary)
        {
            _minBoundary = minBoundary;
            _maxBoundary = maxBoundary;
        }

        public double[] Preprocess(double[] data)
        {
            // x = ((x - min_x) / (max_x - min_x)) * (maxBoundary - minBoundary) + minBoundary

            var min = data.Min();
            var max = data.Max();
            var constFactor = (_maxBoundary - _minBoundary)/(max - min);

            return data.Select(x => (x - min) * constFactor + _minBoundary).ToArray();
        }

        public override string ToString()
        {
            return "Normalization";
        }
    }
}
