using System;

namespace NDtw
{
    public interface IDtw
    {
        double GetCost();
        Tuple<int, int>[] GetPath();
        double[][] GetDistanceMatrix();
        double[][] GetCostMatrix();
        int XLength { get; }
        int YLength { get; }
        double[][] SeriesA { get; }
        double[][] SeriesB { get; }
    }
}
