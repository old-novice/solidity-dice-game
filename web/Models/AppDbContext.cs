using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BCDG
{
    public class BlockChainTrans
    {
        public int Id { get; set; }
        public string TransactionHash { get; set; } = string.Empty;
        public DateTime TimeStamp { get; set; }
        public BigInteger BlockNumber { get; set; }
        public string GameNumber { get; set; } = string.Empty;

        [NotMapped]
        public List<TransEvent> Events { get; set; } = new List<TransEvent>();
    }

    public class TransEvent
    {
        public int Id { get; set; }
        public string TransactionHash { get; set; } = string.Empty;
        public readonly string Topic;
        private readonly string Data;
        public TransEvent(string topic, string data)
        {
            Topic = topic;
            Data = data;
        }
        public TransEvent(string hexStrings)
        {
            var p = hexStrings.Split("|");
            Topic = p[0];
            Data = p[1];
        }
    }



    public class AppDbContext : DbContext
    {
        public DbSet<BlockChainTrans> BlockTxs { get; set; }
        public DbSet<TransEvent> TxEvents { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        //create index
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BlockChainTrans>().HasIndex(b => b.BlockNumber);
            modelBuilder.Entity<BlockChainTrans>().HasIndex(b => b.TransactionHash);
            modelBuilder.Entity<TransEvent>().HasIndex(b => b.TransactionHash);
            modelBuilder.Entity<TransEvent>().HasIndex(b => b.Topic);
        }
    }
}