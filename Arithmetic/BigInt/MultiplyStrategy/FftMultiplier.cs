using Arithmetic.BigInt.Interfaces;
using System;
using System.Collections.Generic;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class FftMultiplier : IMultiplier
{
    private const int WordSize = 32; 
    private const int MinFftBits = 2048;

    private static readonly BetterBigInteger One = new BetterBigInteger(new uint[] { 1 }, isNegative: false);

    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        if (BetterBigInteger.IsZero(a.GetDigits()) || BetterBigInteger.IsZero(b.GetDigits())) 
            return BetterBigInteger.Zero;

        int bitSize = (a.GetDigits().Length + b.GetDigits().Length) * WordSize;
        int n = GetNextPowerOfTwo(bitSize);

        bool resultIsNegative = a.IsNegative ^ b.IsNegative;

        BetterBigInteger result = AlgoSS(a, b, n);

        return SetSign(result, resultIsNegative);
    }

    private BetterBigInteger AlgoSS(BetterBigInteger a, BetterBigInteger b, int N)
    {
        if (N <= MinFftBits)
        {
            return new KaratsubaMultiplier().Multiply(a, b);
        }

        int m = 1 << (GetLog2(N) / 2);
        int n = N / m;

        var aBlocks = SplitIntoBlocks(a, m, n);
        var bBlocks = SplitIntoBlocks(b, m, n);

        ApplyWeighting(aBlocks, m, n, false);
        ApplyWeighting(bBlocks, m, n, false);

        int K = 2 * n; 
        NTT(aBlocks, K, false);
        NTT(bBlocks, K, false);

        var cBlocks = new BetterBigInteger[m];
        for (int i = 0; i < m; i++)
        {
            cBlocks[i] = AlgoSS(aBlocks[i], bBlocks[i], K);
        }

        NTT(cBlocks, K, true);

        ApplyWeighting(cBlocks, m, n, true);

        return CombineBlocks(cBlocks, m, n);
    }

    private void NTT(BetterBigInteger[] a, int K, bool invert)
    {
        int n = a.Length;
        RearrangeBitReversal(a);

        for (int len = 2; len <= n; len <<= 1)
        {
            for (int i = 0; i < n; i += len)
            {
                for (int j = 0; j < len / 2; j++)
                {
                    int step = (2 * K * j) / len;
                    if (invert) step = (2 * K) - step;

                    BetterBigInteger u = a[i + j];
                    BetterBigInteger v = ModularShift(a[i + j + len / 2], step, K);

                    a[i + j] = ModularAdd(u, v, K);
                    a[i + j + len / 2] = ModularSubtract(u, v, K);
                }
            }
        }

        if (invert)
        {
            int shiftCount = GetLog2(n);
            for (int i = 0; i < n; i++)
                a[i] >>= shiftCount;
        }
    }


    private BetterBigInteger ModularShift(BetterBigInteger val, int k, int K)
    {
        if (k == 0) return val;
        k %= (2 * K);

        if (k < K)
        {
            var high = val >> (K - k);
            var low = (val << k) & GetMask(K);
            return ModularSubtract(low, high, K);
        }
        else
        {
            return ModularSubtract(BetterBigInteger.Zero, ModularShift(val, k - K, K), K);
        }
    }

    private BetterBigInteger ModularAdd(BetterBigInteger a, BetterBigInteger b, int K)
    {
        var res = a + b;
        var mod = GetModulus(K);
        return res >= mod ? res - mod : res;
    }

    private BetterBigInteger ModularSubtract(BetterBigInteger a, BetterBigInteger b, int K)
    {
        if (a >= b) return a - b;
        return (a + GetModulus(K)) - b;
    }

    private BetterBigInteger[] SplitIntoBlocks(BetterBigInteger val, int m, int n)
    {
        var blocks = new BetterBigInteger[m];
        var mask = GetMask(n);
        for (int i = 0; i < m; i++)
        {
            blocks[i] = (val >> (i * n)) & mask;
        }
        return blocks;
    }

    private BetterBigInteger CombineBlocks(BetterBigInteger[] blocks, int m, int n)
    {
        var res = BetterBigInteger.Zero;
        for (int i = 0; i < m; i++)
        {
            res += (blocks[i] << (i * n));
        }
        return res;
    }


    private void ApplyWeighting(BetterBigInteger[] blocks, int m, int n, bool invert)
    {
        int K = 2 * n;
        for (int j = 0; j < m; j++)
        {
            int shift = (j * n) / m;
            if (invert) shift = (2 * n * m) - shift;
            blocks[j] = ModularShift(blocks[j], shift % (2 * n), K);
        }
    }


    private int GetNextPowerOfTwo(int value)
    {
        int n = 1;
        while (n < value) n <<= 1;
        return n;
    }

    private int GetLog2(int n)
    {
        int log = 0;
        while (n > 1) {
            n >>= 1;
            log++;
        }
        return log;
    }

    private BetterBigInteger GetMask(int bits)
    {
        return (One << bits) - One;
    }

    private BetterBigInteger GetModulus(int K)
    {
        return (One << K) + One;
    }

    private void RearrangeBitReversal(BetterBigInteger[] a)
    {
        int n = a.Length;
        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1) j ^= bit;
            j ^= bit;
            if (i < j) (a[i], a[j]) = (a[j], a[i]);
        }
    }

    private BetterBigInteger SetSign(BetterBigInteger val, bool isNegative)
    {
        return new BetterBigInteger(val.GetDigits().ToArray(), isNegative);
    }
}
