//(The MIT License)

//Copyright (c) 2012 Darjan Oblak

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the 'Software'), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED 'AS IS', WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;

namespace NDtw
{
    public class Dtw : IDtw
    {
        private readonly int _xLen;
        private readonly int _yLen;
        private readonly bool _isXLongerOrEqualThanY;
        private readonly int _signalsLengthDifference;
        private readonly SeriesVariable[] _seriesVariables;
        private readonly DistanceMeasure _distanceMeasure;
        private readonly bool _boundaryConstraintStart;
        private readonly bool _boundaryConstraintEnd;
        private readonly bool _sakoeChibaConstraint;
        private readonly int _sakoeChibaMaxShift;
        private bool _calculated;
        private double[][] _distances;
        private double[][] _pathCost;
        private readonly bool _useSlopeConstraint;
        private readonly int _slopeMatrixLookbehind = 1;
        private readonly int _slopeStepSizeDiagonal;
        private readonly int _slopeStepSizeAside;

        //[indexX][indexY][step]
        private int[][][] _predecessorStepX;
        private int[][][] _predecessorStepY;

        /// <summary>
        /// Initialize class that performs single variable DTW calculation for given series and settings.
        /// </summary>
        /// <param name="x">Series A, array of values.</param>
        /// <param name="y">Series B, array of values.</param>
        /// /// <param name="distanceMeasure">Distance measure used (how distance for value pair (p,q) of signal elements is calculated from multiple variables).</param>
        /// <param name="boundaryConstraintStart">Apply boundary constraint at (1, 1).</param>
        /// <param name="boundaryConstraintEnd">Apply boundary constraint at (m, n).</param>
        /// <param name="slopeStepSizeDiagonal">Diagonal steps in local window for calculation. Results in Ikatura paralelogram shaped dtw-candidate space. Use in combination with slopeStepSizeAside parameter. Leave null for no constraint.</param>
        /// <param name="slopeStepSizeAside">Side steps in local window for calculation. Results in Ikatura paralelogram shaped dtw-candidate space. Use in combination with slopeStepSizeDiagonal parameter. Leave null for no constraint.</param>
        /// <param name="sakoeChibaMaxShift">Sakoe-Chiba max shift constraint (side steps). Leave null for no constraint.</param>
        public Dtw(double[] x, double[] y, DistanceMeasure distanceMeasure = DistanceMeasure.Euclidean, bool boundaryConstraintStart = true, bool boundaryConstraintEnd = true, int? slopeStepSizeDiagonal = null, int? slopeStepSizeAside = null, int? sakoeChibaMaxShift = null)
            : this(new [] { new SeriesVariable(x, y) }, distanceMeasure, boundaryConstraintStart, boundaryConstraintEnd, slopeStepSizeDiagonal, slopeStepSizeAside, sakoeChibaMaxShift)
        {
            
        }

        /// <summary>
        /// Initialize class that performs multivariate DTW calculation for given series and settings.
        /// </summary>
        /// <param name="seriesVariables">Array of series value pairs for different variables with additional options for data preprocessing and weights.</param>
        /// <param name="distanceMeasure">Distance measure used (how distance for value pair (p,q) of signal elements is calculated from multiple variables).</param>
        /// <param name="boundaryConstraintStart">Apply boundary constraint at (1, 1).</param>
        /// <param name="boundaryConstraintEnd">Apply boundary constraint at (m, n).</param>
        /// <param name="slopeStepSizeDiagonal">Diagonal steps in local window for calculation. Results in Ikatura paralelogram shaped dtw-candidate space. Use in combination with slopeStepSizeAside parameter. Leave null for no constraint.</param>
        /// <param name="slopeStepSizeAside">Side steps in local window for calculation. Results in Ikatura paralelogram shaped dtw-candidate space. Use in combination with slopeStepSizeDiagonal parameter. Leave null for no constraint.</param>
        /// <param name="sakoeChibaMaxShift">Sakoe-Chiba max shift constraint (side steps). Leave null for no constraint.</param>
        public Dtw(SeriesVariable[] seriesVariables, DistanceMeasure distanceMeasure = DistanceMeasure.Euclidean, bool boundaryConstraintStart = true, bool boundaryConstraintEnd = true, int? slopeStepSizeDiagonal = null, int? slopeStepSizeAside = null, int? sakoeChibaMaxShift = null)
        {
            _seriesVariables = seriesVariables;
            _distanceMeasure = distanceMeasure;
            _boundaryConstraintStart = boundaryConstraintStart;
            _boundaryConstraintEnd = boundaryConstraintEnd;

            if (seriesVariables == null || seriesVariables.Length == 0)
                throw new ArgumentException("Series should have values for at least one variable.");

            for (int i = 1; i < _seriesVariables.Length; i++)
            {
                if (_seriesVariables[i].OriginalXSeries.Length != _seriesVariables[0].OriginalXSeries.Length)
                    throw new ArgumentException("All variables withing series should have the same number of values.");

                if (_seriesVariables[i].OriginalYSeries.Length != _seriesVariables[0].OriginalYSeries.Length)
                    throw new ArgumentException("All variables withing series should have the same number of values.");
            }

            _xLen = _seriesVariables[0].OriginalXSeries.Length;
            _yLen = _seriesVariables[0].OriginalYSeries.Length;

            if(_xLen == 0 || _yLen == 0)
                throw new ArgumentException("Both series should have at least one value.");

            if (sakoeChibaMaxShift != null && sakoeChibaMaxShift < 0)
                throw new ArgumentException("Sakoe-Chiba max shift value should be positive or null.");

            _isXLongerOrEqualThanY = _xLen >= _yLen;
            _signalsLengthDifference = Math.Abs(_xLen - _yLen);
            _sakoeChibaConstraint = sakoeChibaMaxShift.HasValue;
            _sakoeChibaMaxShift = sakoeChibaMaxShift.HasValue ? sakoeChibaMaxShift.Value : int.MaxValue;
            
            if (slopeStepSizeAside != null || slopeStepSizeDiagonal != null)
            {
                if (slopeStepSizeAside == null || slopeStepSizeDiagonal == null)
                    throw new ArgumentException("Both values or none for slope constraint must be specified.");

                if (slopeStepSizeDiagonal < 1)
                    throw new ArgumentException("Diagonal slope constraint parameter must be greater than 0.");

                if (slopeStepSizeAside < 0)
                    throw new ArgumentException("Diagonal slope constraint parameter must be greater or equal to 0.");

                _useSlopeConstraint = true;
                _slopeStepSizeAside = slopeStepSizeAside.Value;
                _slopeStepSizeDiagonal = slopeStepSizeDiagonal.Value;

                _slopeMatrixLookbehind = slopeStepSizeDiagonal.Value + slopeStepSizeAside.Value;
            }        
    
            //todo: throw error when solution (path from (1, 1) to (m, n) is not even possible due to slope constraints)
        }

        private void InitializeArrays()
        {
            _distances = new double[_xLen + _slopeMatrixLookbehind][];
            for (int i = 0; i < _xLen + _slopeMatrixLookbehind; i++)
                _distances[i] = new double[_yLen + _slopeMatrixLookbehind];

            _pathCost = new double[_xLen + _slopeMatrixLookbehind][];
            for (int i = 0; i < _xLen + _slopeMatrixLookbehind; i++)
                _pathCost[i] = new double[_yLen + _slopeMatrixLookbehind];

            _predecessorStepX = new int[_xLen + _slopeMatrixLookbehind][][];
            for (int i = 0; i < _xLen + _slopeMatrixLookbehind; i++)
                _predecessorStepX[i] = new int[_yLen + _slopeMatrixLookbehind][];

            _predecessorStepY = new int[_xLen + _slopeMatrixLookbehind][][];
            for (int i = 0; i < _xLen + _slopeMatrixLookbehind; i++)
                _predecessorStepY[i] = new int[_yLen + _slopeMatrixLookbehind][];
        }

        private void CalculateDistances()
        {
            for (int additionalIndex = 1; additionalIndex <= _slopeMatrixLookbehind; additionalIndex++)
            {           
                //initialize [x.len - 1 + additionalIndex][all] elements
                for (int i = 0; i < _yLen + _slopeMatrixLookbehind; i++)
                    _pathCost[_xLen - 1 + additionalIndex][i] = double.PositiveInfinity;

                //initialize [all][y.len - 1 + additionalIndex] elements
                for (int i = 0; i < _xLen + _slopeMatrixLookbehind; i++)
                    _pathCost[i][_yLen - 1 + additionalIndex] = double.PositiveInfinity;
            }

            //calculate distances for 'data' part of the matrix
            foreach (SeriesVariable seriesVariable in _seriesVariables)
            {
                var xSeriesForVariable = seriesVariable.GetPreprocessedXSeries();
                var ySeriesForVariable = seriesVariable.GetPreprocessedYSeries();
                //weight for current variable distances that is applied BEFORE the value is further transformed by distance measure
                var variableWeight = seriesVariable.Weight;

                for (int i = 0; i < _xLen; i++)
                {
                    var currentDistances = _distances[i];
                    var xVal = xSeriesForVariable[i];
                    for (int j = 0; j < _yLen; j++)
                    {
                        if(_distanceMeasure == DistanceMeasure.Manhattan)
                            currentDistances[j] += Math.Abs(xVal - ySeriesForVariable[j]) * variableWeight;
                        else if (_distanceMeasure == DistanceMeasure.Maximum)
                            currentDistances[j] = Math.Max(currentDistances[j], Math.Abs(xVal - ySeriesForVariable[j]) * variableWeight);
                        else
                        {
                            //Math.Pow(xVal - ySeriesForVariable[j], 2) is much slower, so direct multiplication with temporary variable is used
                            var dist = (xVal - ySeriesForVariable[j]) * variableWeight;
                            currentDistances[j] += dist * dist;
                        }        
                    }
                }
            }

            if(_distanceMeasure == DistanceMeasure.Euclidean)
                for (int i = 0; i < _xLen; i++)
                {
                    var currentDistances = _distances[i];
                    for (int j = 0; j < _yLen; j++)
                        currentDistances[j] = Math.Sqrt(currentDistances[j]);
                }
        }

        private void CalculateWithoutSlopeConstraint()
        {
            var stepMove0 = new[] { 0 };
            var stepMove1 = new[] { 1 };

            for (int i = _xLen - 1; i >= 0; i--)
            {
                var currentRowDistances = _distances[i];
                var currentRowPathCost = _pathCost[i];
                var previousRowPathCost = _pathCost[i + 1];

                var currentRowPredecessorStepX = _predecessorStepX[i];
                var currentRowPredecessorStepY = _predecessorStepY[i];

                for (int j = _yLen - 1; j >= 0; j--)
                {
                    //Sakoe-Chiba constraint, but make it wider in one dimension when signal lengths are not equal
                    if (_sakoeChibaConstraint && 
                        (_isXLongerOrEqualThanY
                       ? j > i && j - i > _sakoeChibaMaxShift || j < i && i - j > _sakoeChibaMaxShift + _signalsLengthDifference
                       : j > i && j - i > _sakoeChibaMaxShift + _signalsLengthDifference || j < i && i - j > _sakoeChibaMaxShift))
                    {
                        currentRowPathCost[j] = double.PositiveInfinity;
                        continue;
                    }

                    var diagonalNeighbourCost = previousRowPathCost[j + 1];
                    var xNeighbourCost = previousRowPathCost[j];
                    var yNeighbourCost = currentRowPathCost[j + 1];

                    //on the topright edge, when boundary constrained only assign current distance as path distance to the (m, n) element
                    //on the topright edge, when not boundary constrained, assign current distance as path distance to all edge elements
                    if (double.IsInfinity(diagonalNeighbourCost) && (!_boundaryConstraintEnd || i - j == _xLen - _yLen))
                        currentRowPathCost[j] = currentRowDistances[j];
                    else if (diagonalNeighbourCost <= xNeighbourCost && diagonalNeighbourCost <= yNeighbourCost)
                    {
                        currentRowPathCost[j] = diagonalNeighbourCost + currentRowDistances[j];
                        currentRowPredecessorStepX[j] = stepMove1;
                        currentRowPredecessorStepY[j] = stepMove1;
                    }
                    else if (xNeighbourCost <= yNeighbourCost)
                    {
                        currentRowPathCost[j] = xNeighbourCost + currentRowDistances[j];
                        currentRowPredecessorStepX[j] = stepMove1;
                        currentRowPredecessorStepY[j] = stepMove0;
                    }
                    else
                    {
                        currentRowPathCost[j] = yNeighbourCost + currentRowDistances[j];
                        currentRowPredecessorStepX[j] = stepMove0;
                        currentRowPredecessorStepY[j] = stepMove1;
                    }
                }
            }
        }

        private void CalculateWithSlopeLimit()
        {
            //precreated array that contain arrays of steps which are used when stepaside path is the optimal one
            //stepAsideMoves*[0] is empty element, because contents are 1-based, access to elements is faster that way
            var stepAsideMovesHorizontalX = new int[_slopeStepSizeAside + 1][];
            var stepAsideMovesHorizontalY = new int[_slopeStepSizeAside + 1][];
            var stepAsideMovesVerticalX = new int[_slopeStepSizeAside + 1][];
            var stepAsideMovesVerticalY = new int[_slopeStepSizeAside + 1][];
            for(int i = 1; i <= _slopeStepSizeAside; i++)
            {
                var movesXHorizontal = new List<int>();
                var movesYHorizontal = new List<int>();
                var movesXVertical= new List<int>();
                var movesYVertical= new List<int>();

                //make steps in horizontal/vertical direction
                for(int stepAside = 1; stepAside <= i; stepAside++)
                {
                    movesXHorizontal.Add(1);
                    movesYHorizontal.Add(0);

                    movesXVertical.Add(0);
                    movesYVertical.Add(1);
                }

                //make steps in diagonal direction
                for(int stepForward = 1; stepForward <= _slopeStepSizeDiagonal; stepForward++)
                {
                    movesXHorizontal.Add(1);
                    movesYHorizontal.Add(1);

                    movesXVertical.Add(1);
                    movesYVertical.Add(1);
                }

                stepAsideMovesHorizontalX[i] = movesXHorizontal.ToArray();
                stepAsideMovesHorizontalY[i] = movesYHorizontal.ToArray();

                stepAsideMovesVerticalX[i] = movesXVertical.ToArray();
                stepAsideMovesVerticalY[i] = movesYVertical.ToArray();
            }
            
            var stepMove1 = new[] { 1 };

            for (int i = _xLen - 1; i >= 0; i--)
            {
                var currentRowDistances = _distances[i];

                var currentRowPathCost = _pathCost[i];
                var previousRowPathCost = _pathCost[i + 1];

                var currentRowPredecessorStepX = _predecessorStepX[i];
                var currentRowPredecessorStepY = _predecessorStepY[i];

                for (int j = _yLen - 1; j >= 0; j--)
                {
                    //Sakoe-Chiba constraint, but make it wider in one dimension when signal lengths are not equal
                    if (_sakoeChibaConstraint && 
                        (_isXLongerOrEqualThanY 
                        ? j > i && j - i > _sakoeChibaMaxShift || j < i && i - j > _sakoeChibaMaxShift + _signalsLengthDifference
                        : j > i && j - i > _sakoeChibaMaxShift + _signalsLengthDifference || j < i && i - j > _sakoeChibaMaxShift))
                    {
                        currentRowPathCost[j] = double.PositiveInfinity;
                        continue;
                    }

                    //just initialize lowest cost with diagonal neighbour element
                    var lowestCost = previousRowPathCost[j + 1];
                    var lowestCostStepX = stepMove1;
                    var lowestCostStepY = stepMove1;

                    for(int alternativePathAside = 1; alternativePathAside <= _slopeStepSizeAside; alternativePathAside++)
                    {
                        var costHorizontalStepAside = 0.0;
                        var costVerticalStepAside = 0.0;
                        
                        for(int stepAside = 1; stepAside <= alternativePathAside; stepAside++)
                        {
                            costHorizontalStepAside += _distances[i + stepAside][j];
                            costVerticalStepAside += _distances[i][j + stepAside];
                        }
                        
                        for(int stepForward = 1; stepForward < _slopeStepSizeDiagonal; stepForward++)
                        {
                            costHorizontalStepAside += _distances[i + alternativePathAside + stepForward][j + stepForward];
                            costVerticalStepAside += _distances[i + stepForward][j + alternativePathAside + stepForward];
                        }

                        //at final step, add comulative cost
                        costHorizontalStepAside += _pathCost[i + alternativePathAside + _slopeStepSizeDiagonal][j + _slopeStepSizeDiagonal];

                        //at final step, add comulative cost
                        costVerticalStepAside += _pathCost[i + _slopeStepSizeDiagonal][j + alternativePathAside + _slopeStepSizeDiagonal];

                        //check if currently considered horizontal stepaside is better than the best option found until now
                        if (costHorizontalStepAside < lowestCost)
                        {
                            lowestCost = costHorizontalStepAside;
                            lowestCostStepX = stepAsideMovesHorizontalX[alternativePathAside];
                            lowestCostStepY = stepAsideMovesHorizontalY[alternativePathAside];
                        }

                        //check if currently considered vertical stepaside is better than the best option found until now
                        if (costVerticalStepAside < lowestCost)
                        {
                            lowestCost = costVerticalStepAside;
                            lowestCostStepX = stepAsideMovesVerticalX[alternativePathAside];
                            lowestCostStepY = stepAsideMovesVerticalY[alternativePathAside];
                        }
                    }

                    //on the topright edge, when boundary constrained only assign current distance as path distance to the (m, n) element
                    //on the topright edge, when not boundary constrained, assign current distance as path distance to all edge elements
                    if (double.IsInfinity(lowestCost) && (!_boundaryConstraintEnd || i - j == _xLen - _yLen))
                        lowestCost = 0;

                    currentRowPathCost[j] = lowestCost + currentRowDistances[j];
                    currentRowPredecessorStepX[j] = lowestCostStepX;
                    currentRowPredecessorStepY[j] = lowestCostStepY;
                }
            }
        }

        private void Calculate()
        {
            if (!_calculated)
            {
                InitializeArrays();
                CalculateDistances();

                if (_useSlopeConstraint)
                    CalculateWithSlopeLimit();
                else
                    CalculateWithoutSlopeConstraint();

                _calculated = true;
            }
        }

        public double GetCost()
        {
            Calculate();

            if(_boundaryConstraintStart)
                return _pathCost[0][0];

            return Math.Min(_pathCost[0].Min(), _pathCost.Select(y => y[0]).Min());
        }

        public Tuple<int, int>[] GetPath()
        {
            Calculate();

            var path = new List<Tuple<int, int>>();
            var indexX = 0;
            var indexY = 0;
            if(!_boundaryConstraintStart)
            {
                //find the starting element with lowest cost
                var min = double.PositiveInfinity;
                for (int i = 0; i < Math.Max(_xLen, _yLen); i++)
                {
                    if (i < _xLen && _pathCost[i][0] < min)
                    {
                        indexX = i;
                        indexY = 0;
                        min = _pathCost[i][0];
                    }
                    if (i < _yLen && _pathCost[0][i] < min)
                    {
                        indexX = 0;
                        indexY = i;
                        min = _pathCost[0][i];
                    }
                }
            }

            path.Add(new Tuple<int, int>(indexX, indexY));
            while (
                _boundaryConstraintEnd 
                ? (indexX < _xLen - 1 || indexY < _yLen - 1) 
                : (indexX < _xLen - 1 && indexY < _yLen - 1))
            {
                var stepX = _predecessorStepX[indexX][indexY];
                var stepY = _predecessorStepY[indexX][indexY];

                for(int i = 0; i < stepX.Length; i++)
                {
                    indexX += stepX[i];
                    indexY += stepY[i];
                    path.Add(new Tuple<int, int>(indexX, indexY));
                }
            }
            return path.ToArray();
        }

        public double[][] GetDistanceMatrix()
        {
            Calculate();
            return _distances;
        }

        public double[][] GetCostMatrix()
        {
            Calculate();
            return _pathCost;
        }

        public int XLength
        {
            get { return _xLen; }
        }

        public int YLength
        {
            get { return _yLen; }
        }

        public SeriesVariable[] SeriesVariables
        {
            get { return _seriesVariables; }
        }   
    }
}
