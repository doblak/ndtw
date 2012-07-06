using System.Linq;

namespace NDtw.Preprocessing
{
    public class CentralizationPreprocessor : IPreprocessor
    {
        public double[] Preprocess(double[] data)
        {
            var avg = data.Average();
            return data.Select(x => x - avg).ToArray();
        }

        public override string ToString()
        {
            return "Centralization";
        }
    }
}
