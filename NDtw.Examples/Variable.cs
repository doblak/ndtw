using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using NDtw.Examples.Infrastructure;
using NDtw.Preprocessing;

namespace NDtw.Examples
{
    public class Variable
    {
        public string Name { get; set; }
        public IPreprocessor Preprocessor { get; set; }
        public double Weight { get; set; }
    }
}
