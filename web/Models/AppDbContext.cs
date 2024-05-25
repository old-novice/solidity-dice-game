using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Org.BouncyCastle.Asn1.Crmf;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BCDG
{
    public class AppDbContext : DbContext
    {
        public DbSet<BlockChainTrans> BlockTxs { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        //create index
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BlockChainTrans>().HasIndex(b => b.BlockNumber);
            modelBuilder.Entity<BlockChainTrans>().HasIndex(b => b.TransactionHash);
        }
    }
}