using HostileNetworkUtils;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HostileNetwork {
    class HostileNetworkClient {

        static BackgroundWorker worker;
        static IPAddress sendAddress;
        static IPEndPoint remoteIPEndPoint;
        static volatile bool ackReceived = false;

        static void Main() {

            /*
            //create thread + register functions
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerAsync(server);
            */

            UdpClient server = launchClient();
            string fileName = "";
            string command;

            while (true) {
                command = acceptCommand();
                fileName = "";
            
                switch (command) {
                    case "send":
                        fileName = getValidFileName();
                        break;
                    case "get":
                        break;
                    case "dir":
                        Console.WriteLine("Requesting directory listing from server...");
                        handleDirectoryRequest(server);
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
                    ackReceived = true;
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

        static void handleDirectoryRequest(UdpClient server) {

            if(!sendDirectoryRequest(server)) {
                Console.WriteLine("Failed to send directory request!");
                return;
            }

            if (!receiveDirectory(server)) {
                Console.WriteLine("Failed to receive directory!");
                return;
            }
        }

        static bool receiveDirectory(UdpClient server) {

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
        }

        static bool sendDirectoryRequest(UdpClient server) {

            MetadataPacket packet = new DirectoryMetadataPacket(Constants.TYPE_DIRECTORY_REQUEST);
            Utils.sendTo(server, packet.getBytes);

            Stopwatch operationTimer = new Stopwatch();
            packet.getTimer.Start();
            operationTimer.Start();
            

            while (operationTimer.ElapsedMilliseconds < Constants.OP_TIMEOUT_SECONDS*1000) {

                byte[] receivedBytes = server.Receive(ref remoteIPEndPoint);

                if (packet.getTimer.ElapsedMilliseconds > Constants.ACK_TIMEOUT_MILLISECONDS) {
                    Utils.sendTo(server, packet.getBytes);
                    packet.getTimer.Restart();
                }

                if (receivedBytes[Constants.FIELD_TYPE] == Constants.TYPE_ACK) {
                    return true;
                }
            }

            Console.WriteLine("Operation timeout: error code #" + new Random().Next(9999) + "(Server may be down)");
            return false;
        }

        private static UdpClient launchClient() {

            UdpClient server = new UdpClient(Constants.CLIENT_PORT);
            sendAddress = IPAddress.Parse(Constants.SEND_ADDRESS_STRING);
            remoteIPEndPoint = new IPEndPoint(sendAddress, Constants.SERVER_PORT);
            server.Connect(remoteIPEndPoint);

            return server;

            //while (true) {
            //    data = Console.ReadLine();
            //    sendBytes = Encoding.ASCII.GetBytes(DateTime.Now.ToString() + " " + data);

            //    Utils.sendTo(client, sendBytes);
            //    //client.Send(sendBytes, sendBytes.GetLength(0));
            //    rcvPacket = client.Receive(ref remoteIPEndPoint);

            //    string rcvData = Encoding.ASCII.GetString(rcvPacket);
            //    Console.WriteLine("Handling client at " + remoteIPEndPoint + " - ");

            //    Console.WriteLine("Message Received: " + rcvData);
            //}
        }

        static string getValidFileName() {

            Console.Write("Please enter a filepath\\filename: ");
            string fileName = Console.ReadLine();

            while (fileName == "" || !File.Exists(fileName)) {
                Console.WriteLine("Invalid file name or file does not exist.\n");
                Console.Write("Please enter a filepath\\filename: ");
                fileName = Console.ReadLine();
            }
            return fileName;
        }

        static string acceptCommand() {

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