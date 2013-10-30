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

            string data = "";

            UdpClient server = new UdpClient(Constants.PORT);
            IPAddress sendAddress = IPAddress.Parse(Constants.SEND_ADDRESS_STRING);

            IPEndPoint remoteIPEndPoint = new IPEndPoint(sendAddress, Constants.PORT);

            Console.WriteLine("Server started, waiting for client connection...");

            while (true) {
                byte[] receivedBytes = server.Receive(ref remoteIPEndPoint);
                server.Connect(remoteIPEndPoint);
                data = Encoding.ASCII.GetString(receivedBytes);
                Console.WriteLine("Handling client at " + remoteIPEndPoint + " - ");
                Console.WriteLine("Message Received " + data.TrimEnd());

                Utils.sendTo(server, receivedBytes);
                //server.Send(receivedBytes, receivedBytes.Length, remoteIPEndPoint);
                Console.WriteLine("Message Echoed to" + remoteIPEndPoint + data);
            }

            //server.Close();  //close the connection
        }
    }
}
