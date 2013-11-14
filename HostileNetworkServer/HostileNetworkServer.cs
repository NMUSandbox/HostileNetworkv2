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

        //private static BackgroundWorker worker;
        private static IPAddress sendAddress;
        private static IPEndPoint remoteIPEndPoint;
        private static volatile bool packetReceived = false;
        private static byte[] receivedPacketBytes = new Byte[Constants.PACKET_SIZE];

        private static void Main() {

            /*
            //create thread + register functions
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.ProgressChanged += worker_ProgressChanged;
            
            worker.RunWorkerAsync(client);
             * */
            

            byte[] receivedBytes;
            UdpClient client = GetConnectedClientObject();

            while (true) {
                receivedBytes = client.Receive(ref remoteIPEndPoint);

                if (!Utils.VerifyChecksum(receivedBytes))
                    continue;

                switch (receivedBytes[Constants.FIELD_TYPE]) {
                    case Constants.TYPE_DIRECTORY_REQUEST:
                        Console.WriteLine("DIR request received");
                        bool success = false;
                        while (!success)
                        {
                            AckPacket ack = new AckPacket(-1);
                            Utils.SendTo(client, ack.MyPacketAsBytes);
                            Console.WriteLine("Ackd the request, going into PingPong method");
                            success = PingPong.SendDirectoryTo(client);
                        }
                        break;
                    case Constants.TYPE_FILE_DELIVERY:
                        if (Constants.DEBUG_PING_PONG_ACTIVE)
                        {
                            bool receiveSuccess = false;
                            while (!receiveSuccess)
                            {
                                AckPacket ack = new AckPacket(-1);
                                Utils.SendTo(client, ack.MyPacketAsBytes);
                                receiveSuccess = PingPong.ReceiveFileFrom(receivedBytes, client);
                            }
                        }
                        break;
                    case Constants.TYPE_FILE_REQUEST:
                        break;
                    default:
                        break;
                }
            }
        }

        private static void worker_ProgressChanged(object sender, ProgressChangedEventArgs e) {

            Console.WriteLine("progress changed: " + e.UserState);
        }

        private static void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {

            Console.WriteLine("all done");
        }

        private static void worker_DoWork(object sender, DoWorkEventArgs e) {

            UdpClient client = e.Argument as UdpClient;
        }

        private static bool SendDirectoryMetadataPacket(UdpClient client) {

            //make sure to send ack
            AckPacket directoryRequestAckPacket = new AckPacket(0);
            Utils.SendTo(client, directoryRequestAckPacket.MyPacketAsBytes);

            byte[] directoryListing = Utils.GetDirectoryListing();
            int totalPackets = Utils.GetDirectoryPacketsTotal();
            DirectoryMetadataPacket directoryMetadataPacket = new
                DirectoryMetadataPacket(Constants.TYPE_DIRECTORY_DELIVERY, totalPackets, directoryListing.Length);

            Utils.SendTo(client, directoryMetadataPacket.MyPacketAsBytes);
            Console.WriteLine("Sent directory metadata packet");

            Stopwatch operationTimer = new Stopwatch();
            operationTimer.Start();
            directoryMetadataPacket.MyTimer.Start();

            try {
                client.BeginReceive(new AsyncCallback(ReceivePingPongCallback), client);
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }

            while (operationTimer.ElapsedMilliseconds < Constants.OP_TIMEOUT_SECONDS * 1000) {

                if (packetReceived) {

                    packetReceived = false;

                    Console.WriteLine("directory metadata ack received");
                    if (!Utils.VerifyChecksum(receivedPacketBytes))
                        continue;

                    Console.WriteLine("VALID directory metadata ack received");
                    return true;
                }

                if (directoryMetadataPacket.MyTimer.ElapsedMilliseconds > Constants.PACKET_TIMEOUT_MILLISECONDS) {
                    Utils.SendTo(client, directoryMetadataPacket.MyPacketAsBytes);
                    directoryMetadataPacket.MyTimer.Restart();
                }
            }

            Console.WriteLine("Operation timeout: error code #" + new Random().Next(9999));
            return false;
        }

        private static bool SendDirectoryPackets(UdpClient client) {

            byte[] directoryListing = Utils.GetDirectoryListing();

            int totalPackets = Utils.GetDirectoryPacketsTotal();
            int payloadSize = Constants.PACKET_SIZE - 20;
            byte[] payload = new Byte[payloadSize];

            int byteIndex = 0;
            for (int i = 0; i < totalPackets; i++) {

                for (int j = 0; j < directoryListing.Length; j++) {
                    payload[j] = directoryListing[byteIndex++];
                }
            }

            return false;
        }

        private static void ReceivePingPongCallback(IAsyncResult res) {

            UdpClient client = res.AsyncState as UdpClient;

            byte[] received = client.EndReceive(res, ref remoteIPEndPoint);

            if (Utils.VerifyChecksum(received)) {
                receivedPacketBytes = received;
                packetReceived = true;
            }
        }

        private static UdpClient GetConnectedClientObject() {
        
            UdpClient client = new UdpClient(Constants.SERVER_PORT);
            sendAddress = IPAddress.Parse(Constants.SEND_ADDRESS_STRING);
            remoteIPEndPoint = new IPEndPoint(sendAddress, Constants.CLIENT_PORT);
            client.Connect(remoteIPEndPoint);

            return client;
        }
    }
}
