using HostileNetworkUtils;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace HostileNetwork {
    public class HostileNetworkServer {

        private static IPAddress sendAddress;
        private static IPEndPoint remoteIPEndPoint;
        private static byte[] receivedPacketBytes = new Byte[Constants.PACKET_SIZE];

        private static void Main() {

            byte[] receivedBytes;
            UdpClient client = GetConnectedClientObject();

            while (true) {
                receivedBytes = client.Receive(ref remoteIPEndPoint);

                sendAddress = remoteIPEndPoint.Address;
                remoteIPEndPoint = new IPEndPoint(sendAddress, remoteIPEndPoint.Port);
                client.Connect(remoteIPEndPoint);

                if (!Utils.VerifyChecksum(receivedBytes))
                    continue;

                switch (receivedBytes[Constants.FIELD_TYPE]) {
                    case Constants.TYPE_DIRECTORY_REQUEST:
                        //                 Console.WriteLine("DIR request received");
                        bool success = false;
                        while (!success) {
                            AckPacket ack = new AckPacket(-1);
                            Utils.SendTo(client, ack.MyPacketAsBytes);
                            //      Console.WriteLine("Ackd the request, going into PingPong method");
                            success = PingPong.SendDirectoryTo(client);
                        }
                        break;
                    case Constants.TYPE_FILE_DELIVERY:
                        if (Constants.DEBUG_PING_PONG_ACTIVE) {
                            bool receiveSuccess = false;
                            while (!receiveSuccess) {
                                AckPacket ack = new AckPacket(-1);
                                Utils.SendTo(client, ack.MyPacketAsBytes);
                                receiveSuccess = PingPong.ReceiveFileFrom(receivedBytes, client);
                            }
                        }
                        break;
                    case Constants.TYPE_FILE_REQUEST:
                        //   Console.WriteLine("file request!");
                        byte[] rawFilename = new byte[BitConverter.ToInt32(receivedBytes, Constants.FIELD_FILENAME_LENGTH)];
                        for (int i = 0; i < rawFilename.Length; i++) {
                            rawFilename[i] = receivedBytes[i + Constants.FIELD_FILENAME];
                        }
                        //  Console.WriteLine("parsing name:"+Encoding.ASCII.GetString(rawFilename));
                        PingPong.SendFileTo(Encoding.ASCII.GetString(rawFilename), client);
                        break;
                    default:
                        break;
                }
            }
        }
        private static UdpClient GetConnectedClientObject() {

            UdpClient client = new UdpClient(Constants.SERVER_PORT);

            return client;
        }
    }
}
