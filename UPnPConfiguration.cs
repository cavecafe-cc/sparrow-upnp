namespace Sparrow.UPnP;

public class UPnPConfiguration {
   public bool Enabled { get; init; }
   public PortMap[]? PortMap { get; init; }

   public bool IsUpnpEnabled() {
      return this is { Enabled: true, PortMap.Length: > 0 };
   }
}

public class PortMap {
   public int Internal { get; init; }
   public int External { get; init; }
   public string Protocol { get; set; } = "Tcp";
   public string Description { get; set; } = "UPnP";
}