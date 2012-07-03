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

namespace NDtw
{
    public class Dtw : IDtw
    {
        private readonly int _xLen;
        private readonly int _yLen;
        private readonly double[][] _xSeriesByVariable;
        private readonly double[][] _ySeriesByVariable;
        private readonly int _maxShift;
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
        /// <param name="slopeStepSizeDiagonal">Diagonal steps in local window for calculation. Results in Ikatura paralelogram shaped dtw-candidate space. Use in combination with slopeStepSizeAside parameter. Leave null for no constraint.</param>
        /// <param name="slopeStepSizeAside">Side steps in local window for calculation. Results in Ikatura paralelogram shaped dtw-candidate space. Use in combination with slopeStepSizeDiagonal parameter. Leave null for no constraint.</param>
        /// <param name="maxShift">Sekoe-Chiba max shift constraint (side steps). Leave null for no constraint.</param>
        public Dtw(double[] x, double[] y, int? slopeStepSizeDiagonal = null, int? slopeStepSizeAside = null, int? maxShift = null)
            : this(new[] { x }, new[] { y }, slopeStepSizeDiagonal, slopeStepSizeAside, maxShift)
        {
            
        }

        /// <summary>
        /// Initialize class that performs multivariate DTW calculation for given series and settings.
        /// </summary>
        /// <param name="x">Series A, first dimension is for variable, second dimension for actual values.</param>
        /// <param name="y">Series B, first dimension is for variable, second dimension for actual values.</param>
        /// <param name="slopeStepSizeDiagonal">Diagonal steps in local window for calculation. Results in Ikatura paralelogram shaped dtw-candidate space. Use in combination with slopeStepSizeAside parameter. Leave null for no constraint.</param>
        /// <param name="slopeStepSizeAside">Side steps in local window for calculation. Results in Ikatura paralelogram shaped dtw-candidate space. Use in combination with slopeStepSizeDiagonal parameter. Leave null for no constraint.</param>
        /// <param name="maxShift">Sekoe-Chiba max shift constraint (side steps). Leave null for no constraint.</param>
        public Dtw(double[][] x, double[][] y, int? slopeStepSizeDiagonal = null, int? slopeStepSizeAside = null, int? maxShift = null)
        {
            _xSeriesByVariable = x;
            _ySeriesByVariable = y;

            if (x.Length == 0 || y.Length == 0)
                throw new ArgumentException("Series should have values for at least one variable.");

            if(x.Length != y.Length)
                throw new ArgumentException("Both series should have the same numbers of variables.");

            for (int i = 0; i < _xSeriesByVariable.Length; i++)
            {
                if (x[i].Length != x[0].Length)
                    throw new ArgumentException("All variables withing series should have the same number of values.");

                if (y[i].Length != y[0].Length)
                    throw new ArgumentException("All variables withing series should have the same number of values.");
            }

            _xLen = x[0].Length;
            _yLen = y[0].Length;

            if(_xLen != _yLen)
                throw new ArgumentException("Both series should have at least one value.");

            if(maxShift != null && maxShift < 0)
                throw new ArgumentException("Sekoe-Chiba max shift value should be positive or null.");

            _maxShift = maxShift ?? int.MaxValue;
            
            //todo: consider if this is even ok (larger maxshift when signals are not of equal length)?
            var signalsLengthDifference = Math.Abs(_xLen - _yLen);
            //temporary workaround
            if (_maxShift <= int.MaxValue - signalsLengthDifference)
                _maxShift += signalsLengthDifference;

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
            for (int variableIndex = 0; variableIndex < _xSeriesByVariable.Length; variableIndex++)
            {
                var xSeriesForVariable = _xSeriesByVariable[variableIndex];
                var ySeriesForVariable = _ySeriesByVariable[variableIndex];
                for (int i = 0; i < _xLen; i++)
                {
                    var currentDistances = _distances[i];
                    var xVal = xSeriesForVariable[i];
                    for (int j = 0; j < _yLen; j++)
                    {
                        currentDistances[j] += Math.Abs(xVal - ySeriesForVariable[j]);
                    }
                }
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
                    if (Math.Abs(i - j) > _maxShift)
                    {
                        currentRowPathCost[j] = double.PositiveInfinity;
                        continue;
                    }

                    var diagonalNeighbourCost = previousRowPathCost[j + 1];
                    var xNeighbourCost = previousRowPathCost[j];
                    var yNeighbourCost = currentRowPathCost[j + 1];

                    if (double.IsInfinity(diagonalNeighbourCost) && i - j == _xLen - _yLen)
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
                    if (Math.Abs(i - j) > _maxShift)
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

                    if (double.IsInfinity(lowestCost) && i - j == _xLen - _yLen)
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

            return _pathCost[0][0];
        }

        public Tuple<int, int>[] GetPath()
        {
            Calculate();

            var path = new List<Tuple<int, int>>();
            var indexX = 0;
            var indexY = 0;
            path.Add(new Tuple<int, int>(indexX, indexY));
            while (indexX < _xLen - 1 || indexY < _yLen - 1)
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

        public double[][] SeriesA
        {
            get { return _xSeriesByVariable; }
        }
        
        public double[][] SeriesB
        {
            get { return _ySeriesByVariable; }
        }
    }
}
