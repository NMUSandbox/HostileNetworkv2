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
    public class HostileNetworkClient {

        private static IPAddress sendAddress;
        private static IPEndPoint remoteIPEndPoint;
        private static byte[] receivedPacketBytes = new Byte[Constants.PACKET_SIZE];

        private static void Main() {

            UdpClient server = GetConnectedServerObject();
            string fileName = "";
            string command;

            while (true) {
                command = acceptCommand();
                fileName = "";

                switch (command) {
                    case "send":
                        fileName = GetValidFileName();
                        Console.WriteLine("sending file");
                        if (Constants.DEBUG_PING_PONG_ACTIVE) {
                            bool sendSuccess = false;
                            while (!sendSuccess) {
                                sendSuccess = PingPong.SendFileTo(fileName, server);
                            }
                        }
                        break;
                    case "get":
                        HandleFileGetRequest(server);
                        break;
                    case "dir":
                        HandleDirectoryRequest(server);
                        break;
                    default:
                        break;
                }
            }
        }
        private static void HandleFileGetRequest(UdpClient target) {
            IPEndPoint IPRef = null;
            Console.Write("Please enter a filepath\\filename: ");
            string name = Console.ReadLine();
            FileMetadataPacket pack = new FileMetadataPacket(Constants.TYPE_FILE_REQUEST, 0, name.Length, Encoding.ASCII.GetBytes(name), 0);
            
            bool sent = false;

            Stopwatch timeout = new Stopwatch();
            byte[] backup = new byte[Constants.PACKET_SIZE];
            byte[] receivedBytes = null;
            while (!sent) {
                for (int i = 0; i < Constants.PACKET_SIZE; i++) {
                    backup[i] = pack.MyPacketAsBytes[i];
                }
                Utils.SendTo(target, backup);
                timeout.Restart();
                while (timeout.ElapsedMilliseconds < Constants.PACKET_TIMEOUT_MILLISECONDS) {
                    if (target.Available != 0) {
                        receivedBytes = target.Receive(ref IPRef);

                        if (Utils.VerifyChecksum(receivedBytes)) {
                            if (receivedBytes[Constants.FIELD_TYPE] == Constants.TYPE_FILE_DELIVERY) {
                                sent = true;
                            }
                        }
                    }
                }
                if (Constants.DEBUG_PRINTING && sent != true) Console.WriteLine("Timeout");
            }
            PingPong.SendAckTo(-1, target);

            bool success = false;
            while (!success) {
                AckPacket ack = new AckPacket(-1);
                Utils.SendTo(target, ack.MyPacketAsBytes);
                success = PingPong.ReceiveFileFrom(receivedBytes, target);
            }
            //      Console.WriteLine("Handle file get complete");
        }
        private static void HandleDirectoryRequest(UdpClient server) {

            DirectoryMetadataPacket dir = new DirectoryMetadataPacket(Constants.TYPE_DIRECTORY_REQUEST);

            PingPong.sendUntilAck(dir, server);
            IPEndPoint IPref = null;
            bool success = false;
            while (!success) {
                success = PingPong.StartReceiveDirectory(server.Receive(ref IPref), server);
            }
        }

        private static UdpClient GetConnectedServerObject() {

            UdpClient server = new UdpClient(Constants.CLIENT_PORT);
            sendAddress = IPAddress.Parse(Constants.SEND_ADDRESS_STRING);
            remoteIPEndPoint = new IPEndPoint(sendAddress, Constants.SERVER_PORT);
            server.Connect(remoteIPEndPoint);

            return server;
        }

        private static string GetValidFileName() {

            Console.Write("Please enter a filepath\\filename: ");
            string fileName = Console.ReadLine();

            while (fileName == "" || !File.Exists(fileName)) {
                Console.WriteLine("Invalid file name or file does not exist.\n");
                Console.Write("Please enter a filepath\\filename: ");
                fileName = Console.ReadLine();
            }
            return fileName;
        }

        private static string acceptCommand() {

            string command = "";

            Console.WriteLine("Usage: <get|send|dir>");
            Console.Write("Please enter a command: ");
            command = Console.ReadLine();

            while (command != "dir" && command != "get" && command != "send") {
                Console.WriteLine("Invalid command. Usage: <get|send|dir>\n");
                Console.Write("Please enter a command: ");
                command = Console.ReadLine();
            }

            return command;
        }
    }
}