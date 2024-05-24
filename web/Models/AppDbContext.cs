using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using DiceGame;
using Microsoft.EntityFrameworkCore;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.Crmf;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BCDG
{
    public class BlockChainTrans
    {
        public int Id { get; set; }
        public string TransactionHash { get; set; } = string.Empty;
        public DateTime TimeStamp { get; set; }
        public string BlockNumber { get; set; }
        public string GameNumber { get; set; } = string.Empty;
        public string EventsJson { get; private set; }


        [NotMapped]
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public IEnumerable<JObject> _Events
        {
            get => JsonConvert.DeserializeObject<List<JObject>>(EventsJson ?? "[]") ?? new List<JObject>();
        }
        [NotMapped]
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public IEnumerable<TransEvent> Events
        {
            get => _Events.Select(jo => 
                JsonConvert.DeserializeObject<TransEvent>(jo.ToString()));
        }

        [NotMapped]
        public IEnumerable<string> EventLogs {
            get
            {
                foreach (var evt in Events)
                {
                    switch (evt.EventName)
                    {
                        case nameof(DepositReceivedEventDTO):
                            var depositEvt = evt.GetEventDTO<DepositReceivedEventDTO>();
                            yield return $"DepositReceivedEvent {depositEvt.From} {depositEvt.Amount.ToEther()}";
                            break;
                        case nameof(DiceRolledEventDTO):
                            var diceEvt = evt.GetEventDTO<DiceRolledEventDTO>();
                            yield return $"DiceRolledEvent {diceEvt.Player} {diceEvt.DiceRolls.ToDiceResult()} {diceEvt.Score}";
                            break;
                        case nameof(GameEndedEventDTO):
                            var gameEvt = evt.GetEventDTO<GameEndedEventDTO>();
                            yield return $"GameEndedEvent {gameEvt.Player} {gameEvt.Payout.ToEther()}";
                            break;
                        case nameof(GameStartedEventDTO):
                            var gameStartEvt = evt.GetEventDTO<GameStartedEventDTO>();
                            yield return $"GameStartedEvent from block[{gameStartEvt.StartBlock}] to block[{gameStartEvt.EndBlock}]";
                            break;
                        case nameof(PlayerJoinedEventDTO):
                            var playerEvt = evt.GetEventDTO<PlayerJoinedEventDTO>();
                            yield return $"PlayerJoinedEvent {playerEvt.Player} {playerEvt.Stake.ToEther()}";
                            break;
                        default:
                            yield return $"{evt.Topic} {evt.EventName} {evt.DataJson}";
                            break;
                    }
                    
                }
            }
       }


        public void AddEvent(TransEvent evt)
        {
            var events = _Events.ToList();
            events.Add(JObject.Parse(JsonConvert.SerializeObject(evt)));
            EventsJson = JsonConvert.SerializeObject(events);
        }

    }
    public class TransEvent
    {
        public string Topic { get; set;  }
        public string EventName { get; set;  }
        public string DataJson { get; set; }
        public T GetEventDTO<T>() => JsonConvert.DeserializeObject<T>(DataJson ?? "null")!;

        public TransEvent(string topic, string evtName, object data)
        {
            Topic = topic;
            EventName = evtName;
            DataJson = JsonConvert.SerializeObject(data);
        }
    }



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