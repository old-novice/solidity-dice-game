using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
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

    }
}
