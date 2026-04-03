using AlephionMath;
using System;

namespace Differentiation;

public class ExampleFunction
{
    private void EnsureDefined(double x, double y)
    {
        if (x == y)
            throw new DivideByZeroException("Function and its derivatives are not defined for x = y.");
    }

    private void EnsureDefined(Alephion x, Alephion y)
    {
        if (x == y)
            throw new DivideByZeroException("Function and its derivatives are not defined for x = y.");
    }

    public double F(double x, double y)
    {
        EnsureDefined(x, y);
        return (x + y) / (x - y);
    }

    public Alephion F(Alephion x, Alephion y)
    {
        EnsureDefined(x, y);
        return (x + y) / (x - y);
    }

    // First derivatives
    public double Fx(double x, double y)
    {
        EnsureDefined(x, y);
        double d = x - y;
        return -2.0 * y / (d * d);
    }

    public double Fy(double x, double y)
    {
        EnsureDefined(x, y);
        double d = x - y;
        return 2.0 * x / (d * d);
    }

    // Second derivatives
    public double Fxx(double x, double y)
    {
        EnsureDefined(x, y);
        double d = x - y;
        return 4.0 * y / (d * d * d);
    }

    public double Fxy(double x, double y)
    {
        EnsureDefined(x, y);
        double d = x - y;
        return -2.0 * (x + y) / (d * d * d);
    }

    public double Fyx(double x, double y)
    {
        return Fxy(x, y);
    }

    public double Fyy(double x, double y)
    {
        EnsureDefined(x, y);
        double d = x - y;
        return 4.0 * x / (d * d * d);
    }

    // Third derivatives
    public double Fxxx(double x, double y)
    {
        EnsureDefined(x, y);
        double d = x - y;
        return -12.0 * y / (d * d * d * d);
    }

    public double Fxxy(double x, double y)
    {
        EnsureDefined(x, y);
        double d = x - y;
        return 4.0 * (x + 2.0 * y) / (d * d * d * d);
    }

    public double Fxyx(double x, double y)
    {
        return Fxxy(x, y);
    }

    public double Fyxx(double x, double y)
    {
        return Fxxy(x, y);
    }

    public double Fxyy(double x, double y)
    {
        EnsureDefined(x, y);
        double d = x - y;
        return -4.0 * (2.0 * x + y) / (d * d * d * d);
    }

    public double Fyxy(double x, double y)
    {
        return Fxyy(x, y);
    }

    public double Fyyx(double x, double y)
    {
        return Fxyy(x, y);
    }

    public double Fyyy(double x, double y)
    {
        EnsureDefined(x, y);
        double d = x - y;
        return 12.0 * x / (d * d * d * d);
    }
}