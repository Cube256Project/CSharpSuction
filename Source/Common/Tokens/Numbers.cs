using Common.Tokenization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Tokens
{
    public abstract class Number : Atomic, GeneralString
    {
    }

    public abstract class IntegralNumber : Number
    {
    }

    [RegularExpression("[0-9]+")]
    public sealed class DecimalInteger : IntegralNumber
    {
        public DecimalInteger() { }

        public DecimalInteger(int value)
        {
            _text = value.ToString();
        }
    }
}
