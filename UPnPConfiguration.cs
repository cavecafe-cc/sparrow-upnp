namespace Sparrow.UPnP;

public class UPnPConfiguration {
   public bool Enabled { get; set; }
   public PortMap[]? PortMap { get; set; }

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