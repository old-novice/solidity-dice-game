using BCDG;
using DiceGame;
using Nethereum.Hex.HexTypes;
using System.Numerics;

namespace BCDG
{
    public class DataSyncer
    {
        public string contractAddress;
        private AppDbContext dbContext;
        Timer timer;
        BlockTranRctReader reader;
        public DataSyncer(AppDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public void StartSync()
        {
            this.contractAddress = AppConstants.ContractAddress;
            reader = new BlockTranRctReader(AppConstants.RpcUrl);
            timer = new Timer(Sync, null, 0, 30000);
        }

        public string MaxBlockNumber { get; private set; } = string.Empty;

        bool busy = false;
        public void Sync(object state)
        {
            if (busy) return;
            busy = true;
            var startBlock = dbContext.BlockTxs.OrderByDescending(t => t.BlockNumber).FirstOrDefault();
            var startBlockNumber = new HexBigInteger(startBlock?.BlockNumber ?? "0");
            if (string.IsNullOrEmpty(MaxBlockNumber))
                MaxBlockNumber = startBlockNumber.To8DigitHex();
            var blocks = reader.GetContractTrans(contractAddress, startBlockNumber).Result;
            foreach (var block in blocks.Where(b => b.BlockNumber.CompareTo(startBlock?.BlockNumber ?? "00000000") > 0))
            {
                if (block.Events.Any(b => b.EventName == nameof(DiceRolledEventDTO)))
                {
                    var range = dbContext.BlockTxs.Where(b => b.GameNumber == "").OrderByDescending(b => b.BlockNumber).ToArray();
                    // update game number for all blocks in the same game
                    var gameStartBlock = range.FirstOrDefault(b => b.Events.Any(e => e.EventName == nameof(GameStartedEventDTO)));
                    if (gameStartBlock != null)
                    {
                        var gameNumber = gameStartBlock.BlockNumber;
                        gameStartBlock.GameNumber = gameNumber;
                        foreach (var b in range.Where(b => b.BlockNumber.CompareTo(gameNumber) > 0))
                        {
                            b.GameNumber = gameNumber;
                        }
                        block.GameNumber = gameNumber;
                    }
                }
                if (block.BlockNumber.CompareTo(MaxBlockNumber) > 0)
                    MaxBlockNumber = block.BlockNumber;
                dbContext.BlockTxs.Add(block);
                dbContext.SaveChanges();
            }
            busy = false;
        }
        public void StopSync()
        {
            timer.Dispose();
        }

    }
}
