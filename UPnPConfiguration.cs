using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Sparrow.UPnP;

public class UPnPConfiguration {

   public const string JSON = "upnp.json";

   public UPnPConfiguration(ILogger<UPnPConfiguration> log, string configPath = "") {
      if (string.IsNullOrWhiteSpace(configPath)) {
         configPath = JSON;
      }
      if (!Path.IsPathRooted(configPath)) {
         configPath = Path.Combine(Directory.GetCurrentDirectory(), configPath);
      }
      var builder = new ConfigurationBuilder()
         .AddJsonFile(configPath, optional: false, reloadOnChange: true);

      var config = builder.Build();
      Enabled = config.GetValue<bool>(nameof(Enabled));
      log.LogInformation($"Enabled: {Enabled}");

   }

   public bool Enabled { get; init; } = false;
   public PortMap[]? PortMap { get; set; } = Array.Empty<PortMap>();

   public bool IsUpnpEnabled() {
      return this is { Enabled: true, PortMap.Length: > 0 };
   }
}

public class PortMap {
   public int Internal { get; set; }
   public int External { get; set; }
   public string Protocol { get; set; } = "Tcp";
   public string Description { get; set; } = "UPnP";
}