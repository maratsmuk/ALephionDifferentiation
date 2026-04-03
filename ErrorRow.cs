using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Differentiation.Benchmarking
{
    public sealed class ErrorRow
    {
        public string FunctionName { get; set; } = "";
        public string FunctionType { get; set; } = "";
        public int Dimension { get; set; }

        public int PointIndex { get; set; }
        public string Point { get; set; } = "";

        public string MultiIndex { get; set; } = "";
        public int Order { get; set; }
        public decimal EncodedDegree { get; set; }

        public double ExactValue { get; set; }
        public double PredictedValue { get; set; }
        public double AbsError { get; set; }
        public double SquaredError { get; set; }
    }
}