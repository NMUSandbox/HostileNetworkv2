using System;
using HostileNetworkUtils;
using System.Net.Sockets;
using System.Net;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace HostileNetwork {
    class HostileNetworkClient {

        static BackgroundWorker worker;
        static IPAddress sendAddress;
        static IPEndPoint remoteIPEndPoint;
        static volatile bool ackReceived = false;

        static void Main() {

            //create thread + register functions
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.ProgressChanged += worker_ProgressChanged;

            UdpClient client = launchClient();
            worker.RunWorkerAsync(client);

            string command = acceptCommand();
            string fileName = "";

            switch (command) {
                case "send":
                    fileName = getValidFileName();
                    break;
                case "get":
                    break;
                case "dir":
                    Console.WriteLine("Requesting directory listing from server...");
                    sendDirectoryRequest(client);
                    break;
                default:
                    break;
            }

            Console.ReadLine();
        }

        private static void worker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
        }

        private static void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
        }

        private static void worker_DoWork(object sender, DoWorkEventArgs e) {

            UdpClient client = e.Argument as UdpClient;
            while (true) {
                byte[] receivedBytes = client.Receive(ref remoteIPEndPoint);

                if (receivedBytes[Constants.FIELD_TYPE] == Constants.TYPE_ACK) {
                    Console.WriteLine("Received ack packet");
                    ackReceived = true;
                }
                else if (receivedBytes[Constants.FIELD_TYPE] == Constants.TYPE_DIRECTORY_DELIVERY) {
                    Console.WriteLine("Received directory delivery packet");

                    //receive the actual directory listing
                    receivedBytes = client.Receive(ref remoteIPEndPoint);
                    Console.WriteLine(receivedBytes);
                }
                else if (receivedBytes[Constants.FIELD_TYPE] == Constants.TYPE_FILE_DELIVERY) {
                    Console.WriteLine("Received file delivery packet");
                }
            }
        }

        private static void sendDirectoryRequest(UdpClient client) {

            MetadataPacket packet = new DirectoryMetadataPacket(Constants.TYPE_DIRECTORY_REQUEST);
            Utils.sendTo(client, packet.getBytes);

            Stopwatch timer = new Stopwatch();
            timer.Start();

            while (timer.ElapsedMilliseconds < Constants.OP_TIMEOUT_SECONDS*1000) {

                if (ackReceived) {
                    ackReceived = false;
                    return;
                }

                if (timer.ElapsedMilliseconds > Constants.ACK_TIMEOUT_MILLISECONDS) {
                    Utils.sendTo(client, packet.getBytes);
                }
            }

            Console.WriteLine("Operation timeout: error code #" + new Random().Next(9999));

         //   DateTime start = DateTime.Now;
         //   while //(DateTime.Now.Subtract(start).Seconds < Constants.OP_TIMEOUT_SECONDS) {

                //if (ackReceived) {
                //    ackReceived = false;
                //    return;
                //}

             //   if (DateTime.Now.Subtract(start).Milliseconds > Constants.ACK_TIMEOUT_MILLISECONDS) {
             //       Utils.sendTo(client, packet.getBytes);
            //    }
        //    }

            
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