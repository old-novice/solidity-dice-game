using DiceGame;
using Microsoft.Extensions.DependencyModel;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.Mozilla;
using System.Diagnostics;
using System.Numerics;

namespace BCDG
{

    public interface EventDecoderBase
    {
        string Sha3Signature { get; }
        string EventName { get; }
        public object Decode(JToken data);
    }

    public class EventDecoder<T> : EventDecoderBase where T: IEventDTO, new()
    {
        public string Sha3Signature { get; }
        public string EventName => typeof(T).Name; 
        public EventDecoder()
        {
            Sha3Signature = new T().GetSha3Signature();
        }

        public object Decode(JToken data)
        {
            return new T().DecodeEvent(data);
        }
    }


    public class BlockTranRctReader
    {
        string rpcUrl = "http://localhost:7545";
        Web3 web3;
        Dictionary<string, EventDecoderBase> eventDecoders;
        public BlockTranRctReader(string  rpcUrl = null!)
        {
            this.rpcUrl = rpcUrl ?? this.rpcUrl;
            web3 = new Web3(this.rpcUrl);
            var eventList = new List<EventDecoderBase>()
            {
                new EventDecoder<DepositReceivedEventDTO>(),
                new EventDecoder<DiceRolledEventDTO>(),
                new EventDecoder<GameEndedEventDTO>(),
                new EventDecoder<GameStartedEventDTO>(),
                new EventDecoder<PlayerJoinedEventDTO>()
            };
            eventDecoders = eventList.ToDictionary(e => "0x" + e.Sha3Signature, e => e);
        }

        public async Task DumpEvents(string contractAddress)
        {
            var eventNames = typeof(DiceGame.DiceGameDeploymentBase).Assembly.GetTypes()
                // find all type implementing the interface
                .Where(t => t.GetInterfaces().Contains(typeof(IEventDTO)) && !t.Name.Contains("Base"))
                .Select(t => t.Name)
                .ToList();

            var s = new DepositReceivedEventDTO().GetSha3Signature();

        }
        public async Task<IEnumerable<BlockChainTrans>> GetContractTrans(string contractAddress, HexBigInteger startBlock)
        {
            var filter = new Nethereum.RPC.Eth.DTOs.NewFilterInput()
            {
                Address = [contractAddress],
                FromBlock = new BlockParameter(startBlock),
                ToBlock = new BlockParameter(await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync())
            };
            var dict = new Dictionary<string, BlockChainTrans>();
            var logs = await web3.Eth.Filters.GetLogs.SendRequestAsync(filter);
            foreach (var log in logs)
            {
                var block = await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new BlockParameter(log.BlockNumber));

                var tranHash = log.TransactionHash;
                if (!dict.ContainsKey(tranHash))
                {
                    dict.Add(log.TransactionHash, new BlockChainTrans()
                    {
                        TransactionHash = tranHash,
                        BlockNumber = log.BlockNumber.To8DigitHex(),
                        TimeStamp = DateTimeOffset.FromUnixTimeSeconds((long)(block.Timestamp.Value)).DateTime
                    });

                    var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(log.TransactionHash);
                    var txLogs = receipt.Logs;
                    var blockTrans = dict[tranHash];
                    foreach (var txLog in txLogs)
                    {
                        var topic = txLog["topics"].Value<JArray>().First().ToString();
                        if (eventDecoders.ContainsKey(topic))
                        {
                            var decoder = eventDecoders[topic];
                            var values = decoder.Decode(txLog);
                            blockTrans.AddEvent(new TransEvent(topic, decoder.EventName, values));
                        }
                    }
                }
            }
            return dict.Values;
        }   
    }
}
