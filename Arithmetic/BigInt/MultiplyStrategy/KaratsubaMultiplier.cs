using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class KaratsubaMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        if (a.GetDigits().Length < 32 || b.GetDigits().Length < 32)
            return new SimpleMultiplier().Multiply(a, b);

        return Karatsuba(a, b);
    }

    private BetterBigInteger Karatsuba(BetterBigInteger x, BetterBigInteger y)
    {
        ReadOnlySpan<uint> X = x.GetDigits();
        ReadOnlySpan<uint> Y = y.GetDigits();

        int n = Math.Max(X.Length, Y.Length);

        uint[] xNorm = Normalize(X, n);
        uint[] yNorm = Normalize(Y, n);

        return KaratsubaCore(xNorm, yNorm);
    }

    private BetterBigInteger KaratsubaCore(uint[] X, uint[] Y)
    {
        int n = X.Length;

        if (n < 32)
        {
            return new SimpleMultiplier().Multiply(
                new BetterBigInteger(X, false),
                new BetterBigInteger(Y, false)
            );
        }

        int m = n / 2;

        uint[] x0 = X[..m];
        uint[] x1 = X[m..];
        uint[] y0 = Y[..m];
        uint[] y1 = Y[m..];

        BetterBigInteger z0 = KaratsubaCore(x0, y0);

        BetterBigInteger z2 = KaratsubaCore(x1, y1);

        BetterBigInteger x0x1 = new BetterBigInteger(x0, false) + new BetterBigInteger(x1, false);

        BetterBigInteger y0y1 = new BetterBigInteger(y0, false) + new BetterBigInteger(y1, false);

        BetterBigInteger z1 = Karatsuba(x0x1, y0y1) - z0 - z2;

        BetterBigInteger part2 = z2 << (64 * m);
        BetterBigInteger part1 = z1 << (32 * m);

        return part2 + part1 + z0;
    }

    private uint[] Normalize(ReadOnlySpan<uint> arr, int n)
    {
        uint[] res = new uint[n];
        for (int i = 0; i < arr.Length; i++)
            res[i] = arr[i];
        return res;
    }
}
