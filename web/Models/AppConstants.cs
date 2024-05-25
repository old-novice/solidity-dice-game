using Microsoft.Extensions.Hosting.Internal;
using Nethereum.Contracts.Standards.ENS.PublicResolver.ContractDefinition;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;

namespace BCDG
{
    public class AppConstants
    {
        public static string RpcUrl = "http://localhost:7545";
        public static string ContractAddress = string.Empty;
        public static string DealerAddress = string.Empty;
        public static string ContractAbi;
        public static bool ContractSet => !string.IsNullOrEmpty(ContractAddress) && !string.IsNullOrEmpty(DealerAddress);

        static AppConstants()
        {
            ContractAbi = new StreamReader(typeof(AppConstants).Assembly.GetManifestResourceStream("web.contracts.DiceGame.abi")!).ReadToEnd();
        }

        const string settingFileName = "contract.json";
        static string settingFilePath;
        public static void Load(IConfiguration config, string dataPath)
        {
            RpcUrl = config["RpcUrl"] ?? RpcUrl;
            settingFilePath = Path.Combine(dataPath, settingFileName);
            if (File.Exists(settingFilePath))
            {
                var settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingFilePath))!;
                ContractAddress = settings.ContractAddress;
                DealerAddress = settings.DealerAddress;
            }
        }

        public static void Set(string dealerAddress, string contractAddress)
        {
            if (string.IsNullOrEmpty(settingFilePath))
                throw new InvalidOperationException("Load must be called first");
            if (!string.IsNullOrEmpty(DealerAddress) || !string.IsNullOrEmpty(ContractAddress))
                throw new InvalidOperationException("Settings already set");
            DealerAddress = dealerAddress;
            ContractAddress = contractAddress;
            var settings = new Settings
            {
                ContractAddress = ContractAddress,
                DealerAddress = DealerAddress
            };
            File.WriteAllText(settingFilePath, JsonConvert.SerializeObject(settings));
        }

        public class Settings
        {
            public string ContractAddress { get; set; }
            public string DealerAddress { get; set; }
        }
    }
}
