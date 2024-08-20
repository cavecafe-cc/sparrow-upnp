using System.Net.Sockets;
using SharpOpenNat;

namespace Sparrow.UPnP;

public class UPnPChecker(UPnPConfiguration upnp) {

   private const string tag = nameof(UPnPChecker);
   public const int HTTP_PROXY_PORT = 443;
   
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
            Log.Catch(tag, $"IsPortOpen (failed to close port {port})", se);
            // ignored
         }

         return true;
      }
      catch (Exception e) {
         Log.Catch(tag, $"IsPortOpen (failed to check port {port} open", e);
         return false;
      }
   }
   
   public List<bool> CheckPortsOpened(string domain, bool withHttpProxy, List<int> externalPorts, int waitSeconds = 5) {
      List<bool> results = new(externalPorts.Count);
      results.AddRange(externalPorts.Select(port => {
         var open = IsPortOpen(domain, withHttpProxy ? HTTP_PROXY_PORT : port, TimeSpan.FromSeconds(waitSeconds));
         Log.Info(tag,$"port {port} is {(open ? "open" : "not reachable")}" );
         return open;
      }));
      return results;
   }
   
   public List<bool> CheckPortsOpened(List<(string domain, int externalPort)> dpList, bool withHttpProxy, int waitSeconds = 5) {
      return dpList.Select(dp =>
         IsPortOpen(dp.domain, withHttpProxy ?
            HTTP_PROXY_PORT :
            dp.externalPort, TimeSpan.FromSeconds(waitSeconds))).ToList();
   }

   public async Task<bool> OpenPortAsync(string[] msg, int waitSeconds, CancellationToken cancel) {
      if (!upnp.IsUpnpEnabled()) return false;

      var result = false;
      var openPorts = Task.Run(async () => {
         try {
            var backgroundColor = Console.BackgroundColor;
            Console.BackgroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(string.Join("\n\t", msg));
            Console.BackgroundColor = backgroundColor;
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