using System;

namespace CSharpSuction.Exceptions
{
    [Serializable]
    public class SuctionConfigurationException : Exception
    {
        public SuctionConfigurationException(string message) : base(message) { }
    }
}
