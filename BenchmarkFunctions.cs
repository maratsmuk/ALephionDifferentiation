using System;
using AlephionMath; 

namespace Differentiation.Benchmarking
{
    public static class BenchmarkFunctionsAlephion
    {
        private static Alephion C(double value, int len) => new Alephion(value, len);

        private static Alephion Add(Alephion a, Alephion b) => a + b;
        private static Alephion Sub(Alephion a, Alephion b) => a - b;
        private static Alephion Mul(Alephion a, Alephion b) => a * b;
        private static Alephion Div(Alephion a, Alephion b) => a / b;

        private static Alephion Mul(double c, Alephion x, int len) => Mul(C(c, len), x);

        private static Alephion Pow(Alephion x, int power, int len)
        {
            if (power < 0)
                throw new ArgumentOutOfRangeException(nameof(power));

            if (power == 0) return new Alephion(1.0, len);
            if (power == 1) return new Alephion(x, len);

            Alephion result = new Alephion(1.0, len);
            Alephion current = new Alephion(x, len);
            int p = power;

            while (p > 0)
            {
                if ((p & 1) == 1)
                    result = result * current;

                p >>= 1;
                if (p > 0)
                    current = current * current;
            }

            return result;
        }

        public static Alephion Evaluate(
            string name,
            Alephion[] x,
            IAlephionOps<Alephion> ops,
            int len)
        {
            return name switch
            {
                "poly_2d" => Poly2D(x, ops, len),
                "poly_3d" => Poly3D(x, ops, len),
                "rational_2d" => Rational2D(x, ops, len),
                "rational_3d" => Rational3D(x, ops, len),
                "trig_2d" => Trig2D(x, ops, len),
                "trig_3d" => Trig3D(x, ops, len),
                "exp_2d" => Exp2D(x, ops, len),
                "exp_3d" => Exp3D(x, ops, len),
                "mixed_2d" => Mixed2D(x, ops, len),
                "mixed_3d" => Mixed3D(x, ops, len),
                _ => throw new ArgumentException($"Unknown function name: {name}", nameof(name))
            };
        }

        private static Alephion Poly2D(Alephion[] x, IAlephionOps<Alephion> ops, int len)
        {
            var x0 = x[0];
            var x1 = x[1];

            return Add(
                Add(
                    Sub(
                        Mul(C(1.5, len), Pow(x0, 4, len)),
                        Mul(C(2.0, len), Mul(Pow(x0, 2, len), x1))
                    ),
                    Mul(x0, Pow(x1, 3, len))
                ),
                Mul(C(0.75, len), Pow(x1, 5, len))
            );
        }

        private static Alephion Poly3D(Alephion[] x, IAlephionOps<Alephion> ops, int len)
        {
            var x0 = x[0];
            var x1 = x[1];
            var x2 = x[2];

            return Add(
                Add(
                    Add(
                        Mul(C(1.2, len), Pow(x0, 4, len)),
                        Mul(Mul(x0, x1), Pow(x2, 2, len))
                    ),
                    Mul(Pow(x0, 3, len), x1)
                ),
                Add(
                    Mul(C(-2.0, len), Mul(Pow(x1, 2, len), Pow(x2, 2, len))),
                    Mul(C(0.5, len), Pow(x2, 5, len))
                )
            );
        }

        private static Alephion Rational2D(Alephion[] x, IAlephionOps<Alephion> ops, int len)
        {
            var x0 = x[0];
            var x1 = x[1];

            var numerator = Add(
                Add(x0, Mul(C(2.0, len), x1)),
                Mul(x0, x1)
            );

            var denominator = Add(
                Add(C(2.0, len), Pow(x0, 2, len)),
                Mul(C(0.5, len), Pow(x1, 2, len))
            );

            return Div(numerator, denominator);
        }

        private static Alephion Rational3D(Alephion[] x, IAlephionOps<Alephion> ops, int len)
        {
            var x0 = x[0];
            var x1 = x[1];
            var x2 = x[2];

            var numerator = Add(
                Add(C(1.0, len), Mul(x0, x1)),
                x2
            );

            var denominator = Add(
                Add(
                    Add(
                        Add(C(3.0, len), Pow(x0, 2, len)),
                        Pow(x1, 2, len)
                    ),
                    Pow(x2, 2, len)
                ),
                Mul(x0, x2)
            );

            return Div(numerator, denominator);
        }

        private static Alephion Trig2D(Alephion[] x, IAlephionOps<Alephion> ops, int len)
        {
            var x0 = x[0];
            var x1 = x[1];

            return Add(
                Mul(ops.Sin(x0), ops.Cos(Mul(C(2.0, len), x1))),
                Mul(C(0.5, len), ops.Cos(Sub(x0, x1)))
            );
        }

        private static Alephion Trig3D(Alephion[] x, IAlephionOps<Alephion> ops, int len)
        {
            var x0 = x[0];
            var x1 = x[1];
            var x2 = x[2];

            return Add(
                Mul(ops.Sin(Add(x0, x1)), ops.Cos(x2)),
                Mul(
                    ops.Cos(Sub(Mul(C(2.0, len), x0), x2)),
                    ops.Sin(x1)
                )
            );
        }

        private static Alephion Exp2D(Alephion[] x, IAlephionOps<Alephion> ops, int len)
        {
            var x0 = x[0];
            var x1 = x[1];

            return Add(
                ops.Exp(Sub(Mul(C(0.3, len), x0), Mul(C(0.2, len), x1))),
                ops.Exp(Mul(C(-0.1, len), Mul(x0, x1)))
            );
        }

        private static Alephion Exp3D(Alephion[] x, IAlephionOps<Alephion> ops, int len)
        {
            var x0 = x[0];
            var x1 = x[1];
            var x2 = x[2];

            return Add(
                ops.Exp(
                    Add(
                        Sub(Mul(C(0.2, len), x0), Mul(C(0.15, len), x2)),
                        Mul(C(0.1, len), x1)
                    )
                ),
                ops.Exp(
                    Sub(
                        Mul(C(0.05, len), Mul(x0, x1)),
                        Mul(C(0.03, len), Pow(x2, 2, len))
                    )
                )
            );
        }

        private static Alephion Mixed2D(Alephion[] x, IAlephionOps<Alephion> ops, int len)
        {
            var x0 = x[0];
            var x1 = x[1];

            return Add(
                Mul(ops.Exp(Mul(C(0.2, len), x0)), ops.Sin(x1)),
                Div(
                    Add(Pow(x0, 2, len), x1),
                    Add(C(2.0, len), Pow(x0, 2, len))
                )
            );
        }

        private static Alephion Mixed3D(Alephion[] x, IAlephionOps<Alephion> ops, int len)
        {
            var x0 = x[0];
            var x1 = x[1];
            var x2 = x[2];

            return Add(
                Mul(
                    ops.Exp(Mul(C(0.1, len), x0)),
                    ops.Cos(Mul(x1, x2))
                ),
                Div(
                    Add(Mul(x0, x1), ops.Sin(x2)),
                    Add(C(2.0, len), Add(Pow(x0, 2, len), Pow(x1, 2, len)))
                )
            );
        }
    }
}