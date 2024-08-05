using System.Net.Sockets;
using SharpOpenNat;

namespace Sparrow.UPnP;

public class UPnPChecker(UPnPConfiguration upnp) {
   private bool IsPortOpen(string host, int port, TimeSpan timeout) {
      try {
         using var client = new TcpClient();
         var result = client.BeginConnect(host, port, null, null);
         var success = result.AsyncWaitHandle.WaitOne(timeout);
         if (!success) {
            return false;
         }

         try {
            client.EndConnect(result);
         }
         catch (SocketException se) {
            Console.Error.WriteLine($"failed to close port {port}, err={se.Message}");
            // ignored
         }

         return true;
      }
      catch (Exception e) {
         Console.Error.WriteLine($"failed to check port {port}, err={e.Message}");
         return false;
      }
   }

   public List<bool> CheckPortsOpened(string domain, List<int> ports) {
      List<bool> results = new(ports.Count);
      Console.WriteLine($"checking '{domain}' is reachable outside ...");
      results.AddRange(ports.Select(port => {
         var open = IsPortOpen(domain, port, TimeSpan.FromSeconds(5));
         Console.WriteLine($"port {port} is {(open ? "open" : "not reachable")}");
         return open;
      }));
      return results;
   }

   public async Task<bool> OpenPortAsync(string[] msg, int waitSeconds, CancellationToken cancel) {
      if (!upnp.IsUpnpEnabled()) return false;

      var result = false;
      var openPorts = Task.Run(async () => {
         try {
            Console.WriteLine(string.Join("\n\t", msg));
            Console.ReadKey(true);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(waitSeconds));
            var device = await NatDiscoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);
            var ip = await device.GetExternalIPAsync();
            Console.WriteLine($"External IP: {ip}");
            foreach (var map in upnp.PortMap!) {
               var protocol = map.Protocol switch {
                  "Tcp" => Protocol.Tcp,
                  "Udp" => Protocol.Udp,
                  _ => throw new NotSupportedException("Protocol not supported")
               };
               var mapping = new Mapping(protocol, map.Internal, map.External, map.Description);
               await device.CreatePortMapAsync(mapping);
               result = true;
            }
         }
         catch (Exception e) {
            await Console.Error.WriteLineAsync($"failed to open ports using UPnP, err={e.Message}");
            result = false;
         }

         return result;
      }, cancel);

      try {
         _ = await openPorts.WaitAsync(cancel);
         return true;
      }
      catch (Exception e) {
         await Console.Error.WriteLineAsync($"failed to open ports using UPnP, err={e.Message}");
         return false;
      }
   }
}
