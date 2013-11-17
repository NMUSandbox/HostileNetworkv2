using HostileNetworkUtils;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

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

                IPHostEntry host;
                host = Dns.GetHostEntry(remoteIPEndPoint.Address);

                if (host.HostName == Constants.IGNORE_HOSTNAME_STRING) {
                    continue;
                }

                sendAddress = remoteIPEndPoint.Address;
                remoteIPEndPoint = new IPEndPoint(sendAddress, remoteIPEndPoint.Port);
                client.Connect(remoteIPEndPoint);

                if (!Utils.VerifyChecksum(receivedBytes))
                    continue;

                switch (receivedBytes[Constants.FIELD_TYPE]) {
                    case Constants.TYPE_DIRECTORY_REQUEST:
                        bool success = false;
                        while (!success) {
                            AckPacket ack = new AckPacket(-1);
                            Utils.SendTo(client, ack.MyPacketAsBytes);
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
                        byte[] rawFilename = new byte[BitConverter.ToInt32(receivedBytes, Constants.FIELD_FILENAME_LENGTH)];
                        for (int i = 0; i < rawFilename.Length; i++) {
                            rawFilename[i] = receivedBytes[i + Constants.FIELD_FILENAME];
                        }

                        PingPong.SendFileTo(Encoding.Default.GetString(rawFilename), client);

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
