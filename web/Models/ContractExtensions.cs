using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using System.Numerics;

namespace BCDG
{
    public static class  ContractExtensions 
    {
        public static string GetSha3Signature<T>(this T eventDTO) where T : IEventDTO
        {
            return EventExtensions.GetEventABI<T>().Sha3Signature;
        }

        public static decimal ToEther(this BigInteger value)
        {
            return Web3.Convert.FromWei(value);
        }

        public static string ToDiceResult(this List<byte> dicePoints)
        {
            return $"[{string.Join(",", dicePoints)}]";
        }
        // 8 digit hex can convert block number range of 20 years
        public static string To8DigitHex(this HexBigInteger value)
        {
            return "0x" + value.Value.ToString("x64").Substring(64 - 8, 8);
        }
        public static string To8DigitHex(this BigInteger value)
        {
            return "0x" + value.ToString("x64").Substring(64 - 8, 8);
        }
    }
}
