using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using DiceGame;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BCDG
{
    public class BlockChainTrans
    {
        public int Id { get; set; }
        public string TransactionHash { get; set; } = string.Empty;
        public DateTime TimeStamp { get; set; }
        public string BlockNumber { get; set; }
        public string GameNumber { get; set; } = string.Empty;
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
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

        static string getPlayerType(string account) => account == AppConstants.DealerAddress ? "莊家" : "玩家";
        static string toEth(BigInteger value) => value.ToEther().ToString("n3") + " ETH";
        static string getScoreText(int score)
        {
            switch(score)
            {
                case 3:
                    return "逼嘰";
                case 12:
                    return "十八";
                case 255:
                    return "一色";
                default:
                    return string.Empty;
            }
        } 
        [NotMapped]
        public IEnumerable<string> EventLogs {
            get
            {
                // 分析輸嬴
                var diceEvents = Events.Where(e => e.EventName == nameof(DiceRolledEventDTO)).Select(e => e.GetEventDTO<DiceRolledEventDTO>()).Where(e => e.Score > 0).ToArray();
                var gameEndEvents = Events.Where(e => e.EventName == nameof(GameEndedEventDTO)).Select(e => e.GetEventDTO<GameEndedEventDTO>()).ToArray();
                Dictionary<string, string> winLoseRemarks = new Dictionary<string, string>();
                if (diceEvents.Any())
                {
                    var dealerScore = diceEvents.Single(e => e.Player == AppConstants.DealerAddress).Score;
                    if (dealerScore == 3) winLoseRemarks[AppConstants.DealerAddress] = "通賠";
                    else if (dealerScore >= 12) winLoseRemarks[AppConstants.DealerAddress] = "通殺";
                    else winLoseRemarks[AppConstants.DealerAddress] = "";
                    foreach (var playerDice in diceEvents.Where(e => e.Player != AppConstants.DealerAddress && e.Score > 0))
                    {
                        var score = playerDice.Score;
                        var remark = score == dealerScore ? "平手" : score > dealerScore ? "贏" : "輸";
                        winLoseRemarks[playerDice.Player] = remark;
                    }
                }



                foreach (var evt in Events)
                {
                    switch (evt.EventName)
                    {
                        case nameof(DepositReceivedEventDTO):
                            var depositEvt = evt.GetEventDTO<DepositReceivedEventDTO>();
                            yield return $"莊家儲值 @{depositEvt.From} {toEth(depositEvt.Amount)}";
                            break;
                        case nameof(DiceRolledEventDTO):
                            var diceEvt = evt.GetEventDTO<DiceRolledEventDTO>();
                            yield return $"{getPlayerType(diceEvt.Player)}擲骰 @{diceEvt.Player} 結果：{diceEvt.DiceRolls.ToDiceResult()} 點數：{diceEvt.Score} {getScoreText(diceEvt.Score)} {(diceEvt.Score > 0 ? winLoseRemarks[diceEvt.Player] : "")}";
                            break;
                        case nameof(GameEndedEventDTO):
                            var gameEvt = evt.GetEventDTO<GameEndedEventDTO>();
                            yield return $"局末結算 支付 @{gameEvt.Player} {toEth(gameEvt.Payout)}";
                            break;
                        case nameof(GameStartedEventDTO):
                            var gameStartEvt = evt.GetEventDTO<GameStartedEventDTO>();
                            yield return $"莊家開局 區塊範圍：[{gameStartEvt.StartBlock.To8DigitHex()}] ~ [{gameStartEvt.EndBlock.To8DigitHex()}]";
                            break;
                        case nameof(PlayerJoinedEventDTO):
                            var playerEvt = evt.GetEventDTO<PlayerJoinedEventDTO>();
                            yield return $"玩家下注 @{playerEvt.Player} {toEth(playerEvt.Stake)}";
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
}