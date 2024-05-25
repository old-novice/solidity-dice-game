using Newtonsoft.Json;

namespace BCDG
{
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
}