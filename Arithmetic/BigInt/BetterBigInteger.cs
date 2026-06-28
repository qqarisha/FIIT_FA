using System.Runtime.InteropServices;
using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;

namespace Arithmetic.BigInt;

public sealed class BetterBigInteger : IBigInteger
{
    private int _signBit;
    
    private uint _smallValue; // Если число маленькое, храним его прямо в этом поле, а _data == null.
    private uint[]? _data;
    
    public bool IsNegative => _signBit == 1;
    
    /// От массива цифр (little endian)
    public BetterBigInteger(uint[] digits, bool isNegative = false)
    {
        ArgumentNullException.ThrowIfNull(digits);
        Initialize(digits, isNegative);
    }
    
    public BetterBigInteger(IEnumerable<uint> digits, bool isNegative = false)
    {
        ArgumentNullException.ThrowIfNull(digits);
        Initialize(digits.ToArray(), isNegative);
    }

    private void Initialize(uint[] digits, bool isNegative)
    {
        int lastIndex = digits.Length - 1;
        while (lastIndex >= 0 && digits[lastIndex] == 0)
        {
            lastIndex--;
        }

        if (lastIndex < 0)
        {
            _smallValue = 0;
            _data = null;
            _signBit = 0;
        }
        else if (lastIndex == 0)
        {
            _smallValue = digits[0];
            _data = null;
            _signBit = isNegative ? 1 : 0;
        }
        else
        {
            _data = new uint[lastIndex + 1];
            Array.Copy(digits, _data, lastIndex + 1);
            _smallValue = 0;
            _signBit = isNegative ? 1 : 0;
        }
    }
    
    public BetterBigInteger(string value, int radix)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Строка пуста");
        if (radix < 2 || radix > 36) throw new ArgumentOutOfRangeException(nameof(radix));

        value = value.Trim();
        bool negative = false;
        int startIndex = 0;
        if (value[0] == '-') {
            negative = true;
            startIndex = 1;
        }
        else if (value[0] == '+') {
            startIndex = 1;
        }

        List<uint> digits = new List<uint> { 0 };
        for (int i = startIndex; i < value.Length; i++)
        {
            uint digitValue = CharToValue(value[i]);
            if (digitValue >= (uint)radix) throw new ArgumentException("Недопустимый символ");
            MultiplyByUintAndAdd(digits, (uint)radix, digitValue);
        }

        Initialize(digits.ToArray(), negative);  
    }
    
    private uint CharToValue(char c)
    {
        if (char.IsDigit(c)) return (uint)(c - '0');
        if (char.IsUpper(c)) return (uint)(c - 'A' + 10);
        if (char.IsLower(c)) return (uint)(c - 'a' + 10);
        throw new ArgumentException("Неверный символ");
    }

    public ReadOnlySpan<uint> GetDigits()
    {
        return _data ?? [_smallValue];
    }
    
    public int CompareTo(IBigInteger? other)
    {
        if (other == null) return 1;

        if (this.IsNegative && !other.IsNegative) return -1;
        if (!this.IsNegative && other.IsNegative) return 1;

        ReadOnlySpan<uint> thisDigits = this.GetDigits();
        ReadOnlySpan<uint> otherDigits = other.GetDigits();

        int signModifier = this.IsNegative ? -1 : 1;

        if (thisDigits.Length > otherDigits.Length) return 1 * signModifier;
        if (thisDigits.Length < otherDigits.Length) return -1 * signModifier;

        for (int i = thisDigits.Length - 1; i >= 0; i--)
        {
            if (thisDigits[i] > otherDigits[i]) return 1 * signModifier;
            if (thisDigits[i] < otherDigits[i]) return -1 * signModifier;
        }

        return 0;
    }
    public bool Equals(IBigInteger? other)
    {
        if (ReferenceEquals(this, other)) return true;

        if (other is null) return false;

        if (this.IsNegative != other.IsNegative) return false;

        return this.GetDigits().SequenceEqual(other.GetDigits());
    }
    public override bool Equals(object? obj) => obj is IBigInteger other && Equals(other);
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(IsNegative);
        foreach (var digit in GetDigits())
        {
            hash.Add(digit);
        }
        return hash.ToHashCode();
    }

    private static uint[] Add(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        int length = Math.Max(a.Length, b.Length);
        uint[] result = new uint[length + 1];
        uint carry = 0;
        for (int i = 0; i < length || carry > 0; i++)
        {
            uint valA = (i < a.Length) ? a[i] : 0;
            uint valB = (i < b.Length) ? b[i] : 0;

            uint sum = valA + carry;
            uint carry1 = (sum < valA) ? 1u : 0u; // Перенос от первой части
        
            sum += valB;
            uint carry2 = (sum < valB) ? 1u : 0u; // Перенос от второй части
        
            result[i] = sum;
            carry = carry1 + carry2;
        }
        return result;
    }

    private static uint[] Subtract(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        uint[] result = new uint[a.Length];
        uint borrow = 0;

        for (int i = 0; i < a.Length; i++)
        {
            uint valA = a[i];
            uint valB = (i < b.Length) ? b[i] : 0u;

            
            uint sub1 = valA - borrow;
            uint b1 = (valA < borrow) ? 1u : 0u;

            uint finalSub = sub1 - valB;
            uint b2 = (sub1 < valB) ? 1u : 0u; 

            result[i] = finalSub;
            borrow = b1 + b2; 
        }

        return result;
    }


    private static int Compare(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        if (a.Length != b.Length) return a.Length.CompareTo(b.Length);
        for (int i = a.Length - 1; i >= 0; i--)
        {
            if (a[i] != b[i]) {
                return a[i].CompareTo(b[i]);
            }
        }
        return 0;
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

    private static uint[] MultiplyByUint(ReadOnlySpan<uint> digits, uint multiplier)
    {
        if (multiplier == 0) return [0];
        if (multiplier == 1) return digits.ToArray();

        uint[] result = new uint[digits.Length + 1];
        uint carry = 0u;

        for (int i = 0; i < digits.Length; i++)
        {
            (uint high, uint low) = MultiplyUintByUint(digits[i], multiplier);
            uint sumLow = low + carry;
            uint carryFromLow = (sumLow < low) ? 1u : 0u;

            result[i] = sumLow;
            carry = high + carryFromLow;
        }
        result[digits.Length] = carry;
        return result;
    }

    private static void MultiplyByUintAndAdd(List<uint> digits, uint multiplier, uint addend)
    {
        uint carry = addend;
        for (int i = 0; i < digits.Count; i++)
        {
            // Используем метод из предыдущего ответа:
            (uint high, uint low) = MultiplyUintByUint(digits[i], multiplier);
        
            uint sumLow = low + carry;
            uint carryFromLow = (sumLow < low) ? 1u : 0u;
        
            digits[i] = sumLow;
            carry = high + carryFromLow;
        }
        while (carry > 0)
        {
            digits.Add(carry);
            carry = 0; // carry здесь всегда станет 0 после первого прохода
        }
    }


    private static BetterBigInteger AppendDigit(BetterBigInteger number, uint digit)
    {
        if (number.GetDigits().Length == 1 && number.GetDigits()[0] == 0)
            return new BetterBigInteger([digit], false);

        ReadOnlySpan<uint> oldDigits = number.GetDigits();
        uint[] newDigits = new uint[oldDigits.Length + 1];

        newDigits[0] = digit;

        for (int i = 0; i < oldDigits.Length; i++)
        {
            newDigits[i + 1] = oldDigits[i];
        }

        return new BetterBigInteger(newDigits, false);
    }
    private static (BetterBigInteger Quotient, BetterBigInteger Remainder) DivRem(BetterBigInteger a, BetterBigInteger b)
    {
        BetterBigInteger absA = new BetterBigInteger(a.GetDigits().ToArray(), false);
        BetterBigInteger absB = new BetterBigInteger(b.GetDigits().ToArray(), false);
    
        ReadOnlySpan<uint> aDigits = absA.GetDigits();
        uint[] quotientDigits = new uint[aDigits.Length];
        BetterBigInteger acc = new BetterBigInteger([0], false);

        for (int i = aDigits.Length - 1; i >= 0; i--)
        {
            acc = AppendDigit(acc, aDigits[i]);

            uint low = 0u, high = uint.MaxValue; // используем 0u и uint.MaxValue
            uint q = 0u;

            // Бинарный поиск частного для текущего шага
            while (low <= high)
            {
                uint mid = low + (high - low) / 2;
               
                // Важно: метод MultiplyByUint должен быть исправлен
                BetterBigInteger test = new BetterBigInteger(MultiplyByUint(absB.GetDigits(), mid), false);

                if (test <= acc)
                {
                    q = mid;
                    if (mid == uint.MaxValue) break;
                    low = mid + 1u;
                }
                else
                {
                high = mid - 1u;
                }
            }

            quotientDigits[i] = q;
            BetterBigInteger subtracted = new BetterBigInteger(MultiplyByUint(absB.GetDigits(), q), false);
            acc -= subtracted; // Использует ваш исправленный Subtract
        }

        return (new BetterBigInteger(quotientDigits, false), acc);
    }

    public static BetterBigInteger operator +(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (a.IsNegative == b.IsNegative)
        {
            uint[] res = Add(a.GetDigits(), b.GetDigits());
            return new BetterBigInteger(res, a.IsNegative);
        }

        int compare = Compare(a.GetDigits(), b.GetDigits());

        if (compare == 0) return new BetterBigInteger([0], false); // Числа равны, но знаки разные

        if (compare > 0) // |a| > |b|
        {
            uint[] resDigits = Subtract(a.GetDigits(), b.GetDigits());
            return new BetterBigInteger(resDigits, a.IsNegative);
        }
        else // |b| > |a|
        {
            uint[] resDigits = Subtract(b.GetDigits(), a.GetDigits());
            return new BetterBigInteger(resDigits, b.IsNegative);
        }
    }

    public static BetterBigInteger operator -(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return a + (-b);
    }

    public static BetterBigInteger operator -(BetterBigInteger a)
    {
        ArgumentNullException.ThrowIfNull(a);
        if (a.GetDigits().Length == 1 && a.GetDigits()[0] == 0)
        {
            return a;
        }
        return new BetterBigInteger(a.GetDigits().ToArray(), !a.IsNegative);
    }

    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (b.GetDigits().Length == 1 && b.GetDigits()[0] == 0)
        {
            throw new DivideByZeroException("Деление на ноль невозможно.");
        }

        var (quotient, _) = DivRem(a, b);

        bool resultNegative = a.IsNegative != b.IsNegative;

        if (quotient.GetDigits().Length == 1 && quotient.GetDigits()[0] == 0)
        {
            return new BetterBigInteger([0], false);
        }

        return new BetterBigInteger(quotient.GetDigits().ToArray(), resultNegative);
    }

    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (b.GetDigits().Length == 1 && b.GetDigits()[0] == 0)
        {
            throw new DivideByZeroException("Деление на ноль невозможно.");
        }

        var (_, remainder) = DivRem(a, b);
        bool remainderIsNegative = a.IsNegative;

        if (remainder.GetDigits().Length == 1 && remainder.GetDigits()[0] == 0)
        {
            return new BetterBigInteger([0], false);
        }

        return new BetterBigInteger(remainder.GetDigits().ToArray(), remainderIsNegative);
    }

    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if ((a.GetDigits().Length == 1 && a.GetDigits()[0] == 0) || 
        (b.GetDigits().Length == 1 && b.GetDigits()[0] == 0))
        {
            return new BetterBigInteger([0], false);
        }

        bool resultIsNegative = a.IsNegative != b.IsNegative;

        int maxLength = Math.Max(a.GetDigits().Length, b.GetDigits().Length);
        IMultiplier strategy;

        if (maxLength < 10)
        {
            strategy = new SimpleMultiplier();
        }
        else if (maxLength < 100)
        {
            strategy = new KaratsubaMultiplier();
        }
        else
        {
            strategy = new FftMultiplier();
        }

        BetterBigInteger result = strategy.Multiply(a, b);

        return new BetterBigInteger(result.GetDigits().ToArray(), resultIsNegative);
    }

    public static BetterBigInteger operator ~(BetterBigInteger a)
    {
        ArgumentNullException.ThrowIfNull(a);
        // ~a = -(a + 1)
        BetterBigInteger one = new BetterBigInteger([1], false);
        BetterBigInteger sum = a + one;
        return -sum;
    }

    private uint[] ToTwoComplement(int targetLength)
    {
        ReadOnlySpan<uint> digits = GetDigits();
        uint[] res = new uint[targetLength];

        for (int i = 0; i < digits.Length; i++) res[i] = digits[i];
        for (int i = digits.Length; i < targetLength; i++) res[i] = 0u;

        if (IsNegative)
        {
            for (int i = 0; i < targetLength; i++) res[i] = ~res[i];

            uint carry = 1u;
            for (int i = 0; i < targetLength && carry > 0; i++)
            {
                uint old = res[i];       
                res[i] = old + carry;
                carry = (res[i] < old) ? 1u : 0u;
            }
        }
        return res;
    }

    private static BetterBigInteger FromTwoComplement(uint[] data)
    {
        bool isNegative = (data[^1] & 0x80000000u) != 0u;

        if (!isNegative)
        {
            return new BetterBigInteger(data, false);
        }
        else
        {
            uint[] res = (uint[])data.Clone();
        
            uint borrow = 1u;
            for (int i = 0; i < res.Length && borrow > 0; i++)
            {
                uint old = res[i];
                res[i] = old - borrow;
                borrow = (old < borrow) ? 1u : 0u;
            }
        
            for (int i = 0; i < res.Length; i++) res[i] = ~res[i];
        
            return new BetterBigInteger(res, true);
        }
    }

    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        int len = Math.Max(a.GetDigits().Length, b.GetDigits().Length) + 1;
        uint[] aBits = a.ToTwoComplement(len);
        uint[] bBits = b.ToTwoComplement(len);
        
        uint[] res = new uint[len];
        for (int i = 0; i < len; i++) res[i] = aBits[i] & bBits[i];
        
        return FromTwoComplement(res);
    }

    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        int len = Math.Max(a.GetDigits().Length, b.GetDigits().Length) + 1;
        uint[] aBits = a.ToTwoComplement(len);
        uint[] bBits = b.ToTwoComplement(len);
        
        uint[] res = new uint[len];
        for (int i = 0; i < len; i++) res[i] = aBits[i] | bBits[i];
        
        return FromTwoComplement(res);
    }

    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        int len = Math.Max(a.GetDigits().Length, b.GetDigits().Length) + 1;
        uint[] aBits = a.ToTwoComplement(len);
        uint[] bBits = b.ToTwoComplement(len);
        
        uint[] res = new uint[len];
        for (int i = 0; i < len; i++) res[i] = aBits[i] ^ bBits[i];
        
        return FromTwoComplement(res);
    }

    public static BetterBigInteger operator <<(BetterBigInteger a, int shift)
    {
        ArgumentNullException.ThrowIfNull(a);
        if (shift == 0) return a;
        if (shift < 0) return a >> -shift;

        ReadOnlySpan<uint> aDigits = a.GetDigits();
        if (aDigits.Length == 1 && aDigits[0] == 0) return a;

        int wordShift = shift / 32;
        int bitShift = shift % 32;

        int newLength = aDigits.Length + wordShift + (bitShift > 0 ? 1 : 0);
        uint[] result = new uint[newLength];

        uint carry = 0u;
        for (int i = 0; i < aDigits.Length; i++)
        {
            uint current = aDigits[i];
        
            result[i + wordShift] = (current << bitShift) | carry;
        
            carry = (bitShift == 0) ? 0u : (current >> (32 - bitShift));
        }

        if (bitShift > 0 && carry > 0)
        {
            result[newLength - 1] = carry;
        }

        return new BetterBigInteger(result, a.IsNegative);
    }

    public static BetterBigInteger operator >> (BetterBigInteger a, int shift)
    {
        ArgumentNullException.ThrowIfNull(a);
        if (shift == 0) return a;
        if (shift < 0) return a << -shift;

        if (!a.IsNegative)
        {
            ReadOnlySpan<uint> aDigits = a.GetDigits();
            int wordShift = shift / 32;
            int bitShift = shift % 32;
            if (wordShift >= aDigits.Length) return new BetterBigInteger([0], false);
            int newLength = aDigits.Length - wordShift;
            uint[] result = new uint[newLength];
            for (int i = 0; i < newLength; i++)
            {
                uint current = aDigits[i + wordShift] >> bitShift;
                uint next = (i + wordShift + 1 < aDigits.Length) ? aDigits[i + wordShift + 1] << (32 - bitShift) : 0;
                result[i] = current | next;
            }
            return new BetterBigInteger(result, false);
        }
        else
        {
            return ~(~a >> shift);
        }
    }

    public static bool operator ==(BetterBigInteger a, BetterBigInteger b) => Equals(a, b);
    public static bool operator !=(BetterBigInteger a, BetterBigInteger b) => !Equals(a, b);
    public static bool operator <(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) < 0;
    public static bool operator >(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) > 0;
    public static bool operator <=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) <= 0;
    public static bool operator >=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) >= 0;
    
    private static uint DivideByUint(uint[] digits, uint divisor)
    {
        uint remainder = 0;
        for (int i = digits.Length - 1; i >= 0; i--)
        {
        // Нам нужно разделить число (remainder * 2^32 + digits[i]) на divisor.
        // Поскольку 2^32 не влезает в uint, делим по 16 бит (разбиваем на два полуслова).
        
            uint val = digits[i];
        
        // Старшие 16 бит остатка и старшие 16 бит val
            uint high = (remainder << 16) | (val >> 16);
            uint q1 = high / divisor;
            uint r1 = high % divisor;
        
        // Младшие 16 бит остатка и младшие 16 бит val
            uint low = (r1 << 16) | (val & 0xFFFF);
            uint q2 = low / divisor;
            uint r2 = low % divisor;
        
            digits[i] = (q1 << 16) | q2;
            remainder = r2;
        }
        
        return remainder;
    }

    public static bool IsZero(ReadOnlySpan<uint> digits)
    {
        foreach (var d in digits) {
            if (d != 0) return false;
        }
        return true;
    }
    public static BetterBigInteger Zero => new BetterBigInteger([0u], false);
    public override string ToString() => ToString(10);
    public string ToString(int radix)
    {
        if (radix < 2 || radix > 36)
            throw new ArgumentOutOfRangeException(nameof(radix));

        ReadOnlySpan<uint> digits = GetDigits();
        if (digits.Length == 1 && digits[0] == 0) return "0";

        uint[] workingCopy = digits.ToArray();
        var sb = new System.Text.StringBuilder();

        while (!IsZero(workingCopy))
        {
            uint remainder = DivideByUint(workingCopy, (uint)radix);
            sb.Append(ValueToChar(remainder));
        }

        if (IsNegative) sb.Append('-');

        char[] charArray = sb.ToString().ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }

    private char ValueToChar(uint value)
    {
        if (value < 10) return (char)('0' + value);
        return (char)('a' + (value - 10));
    }

}
