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

        private static BackgroundWorker worker;
        private static IPAddress sendAddress;
        private static IPEndPoint remoteIPEndPoint;
        private static volatile bool packetReceived = false;
        private static byte[] receivedPacketBytes = new Byte[Constants.PACKET_SIZE];

        private static void Main() {

            /*
            //create thread + register functions
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerAsync(server);
            */

            UdpClient server = GetConnectedServerObject();
            string fileName = "";
            string command;

            while (true) {
                command = acceptCommand();
                fileName = "";
            
                switch (command) {
                    case "send":
                        fileName = GetValidFileName();
                        break;
                    case "get":
                        break;
                    case "dir":
                        Console.WriteLine("Requesting directory listing from server...");
                        HandleDirectoryRequest(server);
                        break;
                    default:
                        break;
                }
            }
        }

        private static void worker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
        }

        private static void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
        }

        private static void worker_DoWork(object sender, DoWorkEventArgs e) {

            UdpClient server = e.Argument as UdpClient;
            while (true) {
                byte[] receivedBytes = server.Receive(ref remoteIPEndPoint);

                if (receivedBytes[Constants.FIELD_TYPE] == Constants.TYPE_ACK) {
                    Console.WriteLine("Received ack packet");
                    packetReceived = true;
                }
                else if (receivedBytes[Constants.FIELD_TYPE] == Constants.TYPE_DIRECTORY_DELIVERY) {
                    Console.WriteLine("Received directory delivery packet");

                    //receive the actual directory listing
                    receivedBytes = server.Receive(ref remoteIPEndPoint);
                    string directoryListing = Encoding.Unicode.GetString(receivedBytes);
                    Console.WriteLine(directoryListing);
                }
                else if (receivedBytes[Constants.FIELD_TYPE] == Constants.TYPE_FILE_DELIVERY) {
                    Console.WriteLine("Received file delivery packet");
                }
            }
        }

        private static void HandleDirectoryRequest(UdpClient server) {

            if(!SendDirectoryRequest(server)) {
                Console.WriteLine("Failed to send directory request!");
                return;
            }

            if (!ReceiveDirectory(server)) {
                Console.WriteLine("Failed to receive directory!");
                return;
            }
        }

        private static bool ReceiveDirectory(UdpClient server) {


            //************************START HERE
            Stopwatch operationTimer = new Stopwatch();
            operationTimer.Start();

            while (operationTimer.ElapsedMilliseconds < Constants.OP_TIMEOUT_SECONDS * 1000) {
                
                byte[] directoryMetadata = server.Receive(ref remoteIPEndPoint);
                if (directoryMetadata[Constants.FIELD_TYPE] != Constants.TYPE_DIRECTORY_DELIVERY) 
                    continue;

                operationTimer.Restart();

                int payloadSize = Constants.PACKET_SIZE - 20;
                int packetsReceived = 0;
                int totalPackets = BitConverter.ToInt32(directoryMetadata,Constants.FIELD_TOTAL_PACKETS);
                while (packetsReceived < totalPackets) {

                    byte[] directoryPayloadPacket = server.Receive(ref remoteIPEndPoint);
                    packetsReceived++;

                    for (int i = 0; i < payloadSize; i++) {
                        Console.Write(directoryPayloadPacket[Constants.FIELD_PAYLOAD + i]);
                    }
                }
            }

            return false;

            /*
            byte[] directoryMetadata = server.Receive(ref remoteIPEndPoint);
            if (directoryMetadata[Constants.FIELD_TYPE] == Constants.TYPE_DIRECTORY_DELIVERY) {
                int packetsPrinted = 0;
                int totalPackets = directoryMetadata[Constants.FIELD_TOTAL_PACKETS];

                while (packetsPrinted < totalPackets) {
                    
                    byte[] next = server.Receive(ref remoteIPEndPoint);
                
                    if (next[Constants.FIELD_PACKET_ID] < packetsPrinted) {
                        //if the packet# is < packetsPrinted, ack it
                        //send ack with ID next[Constants.FIELD_PACKET_ID]
                    }

                    //if it's == packetsPrinted, print it's payload to the screen, ack it, increment packetsPrinted
                    else if (next[Constants.FIELD_PACKET_ID] == packetsPrinted) {
                        //get payload bytes, turn them into a string with Unicode encoding
                        //print those bytes
                        //ack the packet
                        packetsPrinted++;
                    }

                    //if it's > that, store it in the list. 
                    else {
                        //store the packet in the list
                    }
                    
                    //check the lowest #'d packet in the list, if it's the next one needed print it, remove it, inc packetsPrinted *REPEAT*
                }
            }
           
            return false;
             * */
        }

        private static bool SendDirectoryRequest(UdpClient server) {

            DirectoryMetadataPacket directoryRequestPacket = new DirectoryMetadataPacket(Constants.TYPE_DIRECTORY_REQUEST);
            Utils.SendTo(server, directoryRequestPacket.MyPacketAsBytes);

            Stopwatch operationTimer = new Stopwatch();
            operationTimer.Start();
            directoryRequestPacket.MyTimer.Start();

            try {
                server.BeginReceive(new AsyncCallback(ReceivePingPongCallback), server);
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }

            while (operationTimer.ElapsedMilliseconds < Constants.OP_TIMEOUT_SECONDS * 1000) {

                if (packetReceived) {
                    packetReceived = false;

                    Console.WriteLine("Received directory request ack.");

                    if (!Utils.VerifyChecksum(receivedPacketBytes))
                        continue;

                    Console.WriteLine("Received VALID directory request ack.");

                    return true;
                }

                if (directoryRequestPacket.MyTimer.ElapsedMilliseconds > Constants.ACK_TIMEOUT_MILLISECONDS) {
                    Utils.SendTo(server, directoryRequestPacket.MyPacketAsBytes);
                    directoryRequestPacket.MyTimer.Restart();
                }
            }

            Console.WriteLine("Operation timeout: error code #" + new Random().Next(9999));
            return false;
        }

        private static void ReceivePingPongCallback(IAsyncResult res) {

            UdpClient server = res.AsyncState as UdpClient;

            byte[] received = server.EndReceive(res, ref remoteIPEndPoint);

            if (Utils.VerifyChecksum(received)) {
                receivedPacketBytes = received;
                packetReceived = true;
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