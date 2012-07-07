NDtw
====

Dynamic Time Warping (DTW) algorithm implementation for .NET C#

Features
====

* Single or multivariate
* Data preprocessing options (none, centering, normalization, standardization)
* Optional weights for variables
* Manhattan, Euclidean, SquaredEuclidean distance measures
* Optional boundary constraints (1, 1) and (m,n)
* Sakoe-Chiba band
* Ikatura paralelogram (custom slope constraint can be specified which results in such paralelogram)
* C# code written with performance in mind (fast data structures, local variables caching for loops etc.)
 
Three libraries:

* NDtw (algorithm)
* NDtw.Visualization.Wpf (visualization WPF user controls included: series and matrix)
* NDtw.Examples (WPF example application for demonstration purposes)

Usage:

* very simple: var cost = new Dtw(seriesA, seriesB).GetCost();
* or much more complex initialization of Dtw class with various features

Available on NuGet.

Screenshots
====

![DTW visualization](https://github.com/doblak/ndtw/raw/master/wiki/visualization-series.png)

![DTW visualization](https://github.com/doblak/ndtw/raw/master/wiki/visualization-matrix.png)

Thanks to
====

* http://oxyplot.codeplex.com/, a great charting library
* Sample data provider: 'Classification of individual consumption by purpose - 1998 (COICOP), source: Eurostat'
* everyone else for various code snippets (mentioned in code)

License
====
NDtw is released under the MIT license: www.opensource.org/licenses/MIT
