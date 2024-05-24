using BCDG;
using System.Numerics;

namespace BCDG
{
    public class DataSyncer
    {
        public string rpcUrl = "http://localhost:7545";
        public string contractAddress = null!;
        private AppDbContext dbContext;
        Timer timer;
        BlockTranRctReader reader;
        public DataSyncer(AppDbContext dbContext, string rpcUrl = null!)
        {
            this.dbContext = dbContext;
            this.rpcUrl = rpcUrl ?? this.rpcUrl;
        }
        public void StartSync(string contractAddress)
        {
            this.contractAddress = contractAddress;
            reader = new BlockTranRctReader(rpcUrl);
            timer = new Timer(Sync, null, 0, 30000);
        }
        bool busy = false;
        public void Sync(object state)
        {
            if (busy) return;
            busy = true;
            var startBlock = BigInteger.Parse(dbContext.BlockTxs.OrderByDescending(t => t.BlockNumber).FirstOrDefault()?.BlockNumber ?? "0");
            var blocks = reader.GetContractTrans(contractAddress, startBlock).Result;
            foreach (var block in blocks.Where(b => b.BlockNumber.CompareTo(startBlock.ToString()) > 0))
            {
                dbContext.BlockTxs.Add(block);
            }
            dbContext.SaveChanges();
            busy = false;
        }
        public void StopSync()
        {
            timer.Dispose();
        }

    }
}
