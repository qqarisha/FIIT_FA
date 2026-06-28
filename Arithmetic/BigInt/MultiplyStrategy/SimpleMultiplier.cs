using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        ReadOnlySpan<uint> aDigits = a.GetDigits();
        ReadOnlySpan<uint> bDigits = b.GetDigits();

        uint[] result = new uint[aDigits.Length + bDigits.Length];

        for (int i = 0; i < aDigits.Length; i++)
        {
            uint aDigit = aDigits[i];
            if (aDigit == 0) continue;

            uint carry = 0u;
            for (int j = 0; j < bDigits.Length; j++)
            {
                (uint high, uint low) = MultiplyUintByUint(aDigit, bDigits[j]);

                uint prev = result[i + j];

                uint sumLow = prev + low;
                uint carryLow = (sumLow < prev) ? 1u : 0u;

                uint totalSum = sumLow + carry;
                uint carryTotal = (totalSum < sumLow) ? 1u : 0u;
            
                result[i + j] = totalSum;

                carry = high + carryLow + carryTotal;
            }

            int k = i + bDigits.Length;
            while (carry > 0 && k < result.Length)
            {
                uint old = result[k];
                result[k] = old + carry;
                carry = (result[k] < old) ? 1u : 0u;
                k++;
            }
        }

        return new BetterBigInteger(result, false);
    }

    private static (uint high, uint low) MultiplyUintByUint(uint a, uint b)
    {
        uint aL = (ushort)a;
        uint aH = a >> 16;
        uint bL = (ushort)b;
        uint bH = b >> 16;

        uint p0 = aL * bL;
        uint p1 = aH * bL;
        uint p2 = aL * bH;
        uint p3 = aH * bH;

        uint middle = (p0 >> 16) + (ushort)p1 + (ushort)p2;
        uint high = p3 + (middle >> 16) + (p1 >> 16) + (p2 >> 16);
        uint low = (middle << 16) | (ushort)p0;

        return (high, low);
    }
}
