using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Newtonsoft.Json.Linq;

namespace BCDG
{
    public class BlockTranRctReader
    {
        string rpcUrl = "http://localhost:7545";
        Web3 web3 = null;
        public BlockTranRctReader(string  rpcUrl)
        {
            this.rpcUrl = rpcUrl;
            web3 = new Web3(rpcUrl);
        }

        public async Task<IEnumerable<BlockChainTrans>> GetContractTrans(string contractAddress)
        {

            var filter = new Nethereum.RPC.Eth.DTOs.NewFilterInput()
            {
                Address = [contractAddress],
                FromBlock = new BlockParameter(0),
                ToBlock = new BlockParameter(await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync())
            };
            var dict = new Dictionary<string, BlockChainTrans>();
            List<TransEvent> txEvents = new List<TransEvent>();
            var logs = await web3.Eth.Filters.GetLogs.SendRequestAsync(filter);
            foreach (var log in logs)
            {
                var timeStamp = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync(log.BlockNumber.Value);
                var tranHash = log.TransactionHash;
                if (!dict.ContainsKey(tranHash))
                {
                    
                    dict.Add(log.TransactionHash, new BlockChainTrans()
                    {
                        TransactionHash = tranHash,
                        BlockNumber = log.BlockNumber.Value,
                        TimeStamp = await web3.Eth.Blocks.GetBlockByNumber.SendRequestAsync(log.BlockNumber.Value).Result.Timestamp,
                    });
                }
                var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(log.TransactionHash);
                var txLogs = receipt.Logs;
                var blockTrans = dict[tranHash];
                foreach (var txLog in txLogs)
                {
                    blockTrans.Events.Add(new TransEvent(txLog["topics"].Value<JArray>().First().ToString(), txLog["data"].ToString())
                    {
                        TransactionHash = tranHash
                    });
                }
            }
            return dict.Values;
        }   
    }
}
