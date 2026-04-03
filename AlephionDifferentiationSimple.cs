using AlephionMath;

namespace Differentiation
{
    public class AlephionDifferentiationSimple
    {
        public List<double> Differentiate(ExampleFunction function, double x0, double y0, int m = 3)
        {
            var hx = Alephion.CreateFromAtom(new AlephAtom(1.0, 110));
            var hy = Alephion.CreateFromAtom(new AlephAtom(1.0, 101));
            var func = function.F(new Alephion(x0, 20) + hx, new Alephion(y0, 20) + hy);
            Console.WriteLine($"{func}");
            var f = func.GetFinitePart();
            var fx = (func / hx).GetFinitePart();
            var fy = (func / hy).GetFinitePart();
            var fxx = (func / hx / hx).GetFinitePart() * 2;
            var fyy = (func / hy / hy).GetFinitePart() * 2;
            var fxy = (func / hx / hy).GetFinitePart();

            var fxxx = (func / hx / hx / hx).GetFinitePart() * 6;
            var fxxy = (func / hx / hx / hy).GetFinitePart() * 2;
            var fxyy = (func / hx / hy/ hy).GetFinitePart() * 2;
            var fyyy = (func / hy / hy / hy).GetFinitePart() * 6;
            return new List<double>() { f, fx, fy, fxx, fxy, fyy, fxxx, fxxy, fxyy, fyyy };
        }
    }
}
