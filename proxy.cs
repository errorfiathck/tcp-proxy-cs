using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace didge {
    public class StaticTcpProxy {
        // Use this method to proxy from the proxy's port to the target server's end point.  Stop the proxy using returned the CancellationTokenSource.
        public static CancellationTokenSource Start(IPEndPoint proxyServerEndPoint, IPEndPoint serverEndPoint) {
            var cancellationTokenSource = new CancellationTokenSource();
            Task.Run(async () => {
                await Start(proxyServerEndPoint, serverEndPoint, cancellationTokenSource.Token);
            });
            return cancellationTokenSource;
        }

        // Use this method with Task.WhenAny to share a single CancellationToken between several proxies
        public static async Task Start(IPEndPoint proxyServerEndPoint, IPEndPoint serverEndPoint, CancellationToken token) {
            var proxyServer = new TcpListener(proxyServerEndPoint);
            proxyServer.Start();
            token.Register(() => proxyServer.Stop());

            while(true) {
                try {
                    var proxyClient = await proxyServer.AcceptTcpClientAsync();
                    proxyClient.NoDelay = true;
                    Run(proxyClient, serverEndPoint);
                }
                catch(Exception exc) {
                    Console.WriteLine(exc);
                }
            }
        }

        private static void Run(TcpClient proxyClient, IPEndPoint serverEndPoint) {
            Task.Run(async () => {
                try {
                    using(proxyClient)
                    using(var serverClient = new TcpClient()) {
                        serverClient.NoDelay = true;
                        await serverClient.ConnectAsync(serverEndPoint.Address, serverEndPoint.Port);
                        var serverStream = serverClient.GetStream();
                        var proxyStream = proxyClient.GetStream();
                        await Task.WhenAny(proxyStream.CopyToAsync(serverStream), serverStream.CopyToAsync(proxyStream));
                    }
                }
                catch(Exception exc) {
                    Console.WriteLine(exc);
                }
            });
        }
    }
}