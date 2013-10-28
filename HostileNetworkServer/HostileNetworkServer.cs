using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HostileNetworkUtils;
using System.Net.Sockets;
using System.Net;

namespace HostileNetwork {
    class HostileNetworkServer {

        static void Main() {

            launchServer();
        }

        private static void launchServer() {

            const int PORT = 58008;
            string data = "";
            string sendAddressString = "127.0.0.1";

            UdpClient server = new UdpClient(PORT);
            IPAddress sendAddress = IPAddress.Parse(sendAddressString);

            IPEndPoint remoteIPEndPoint = new IPEndPoint(sendAddress, PORT);

            Console.WriteLine("Server started, waiting for client connection...");

            while (true) {
                byte[] receivedBytes = server.Receive(ref remoteIPEndPoint);
                data = Encoding.ASCII.GetString(receivedBytes);
                Console.WriteLine("Handling client at " + remoteIPEndPoint + " - ");
                Console.WriteLine("Message Received " + data.TrimEnd());


                server.Send(receivedBytes, receivedBytes.Length, remoteIPEndPoint);
                Console.WriteLine("Message Echoed to" + remoteIPEndPoint + data);
            }

            Console.WriteLine("Press Enter Program Finished");
            Console.ReadLine(); //delay end of program
            server.Close();  //close the connection
        }
    }
}
