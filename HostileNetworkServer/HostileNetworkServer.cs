using HostileNetworkUtils;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HostileNetwork {
    class HostileNetworkServer {

        static IPAddress sendAddress;
        static IPEndPoint remoteIPEndPoint;

        static void Main() {

            /*
            //create thread + register functions
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.ProgressChanged += worker_ProgressChanged;
            
            worker.RunWorkerAsync(client);
            */

            byte[] receivedBytes;
            UdpClient client = LaunchServer();

            while (true) {
                receivedBytes = client.Receive(ref remoteIPEndPoint);

                switch (receivedBytes[Constants.FIELD_TYPE]) {
                    case Constants.TYPE_DIRECTORY_REQUEST:
                        RespondToDirectoryRequest(client);
                        break;
                    case Constants.TYPE_FILE_DELIVERY:
                        break;
                    case Constants.TYPE_FILE_REQUEST:
                        break;
                    default:
                        break;
                }
            }
        }

        static void worker_ProgressChanged(object sender, ProgressChangedEventArgs e) {

            Console.WriteLine("progress changed: " + e.UserState);
        }

        static void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {

            Console.WriteLine("all done");
        }

        static void worker_DoWork(object sender, DoWorkEventArgs e) 
        {
            UdpClient client = e.Argument as UdpClient;
            while (true) {
                byte[] receivedBytes = client.Receive(ref remoteIPEndPoint);

                if (receivedBytes[Constants.FIELD_TYPE] == Constants.TYPE_DIRECTORY_REQUEST) {
                    
                    
                }
            }
        }

        static void RespondToDirectoryRequest(UdpClient client) {

            //make sure to send ack
            byte[] directoryRequestAck = new Byte[Constants.PACKET_SIZE];
            directoryRequestAck[Constants.FIELD_TYPE] = Constants.TYPE_ACK;
            Utils.SendTo(client, directoryRequestAck);


            //***********Need some extra work here to get info for packet constructor
            //send directory metadata packet to client
            DirectoryMetadataPacket directoryMetadataPacket = new DirectoryMetadataPacket(Constants.TYPE_DIRECTORY_DELIVERY, null,0);
           
            Utils.SendTo(client, directoryMetadataPacket.MyPacketAsBytes);
            //send directory listing
            Utils.SendTo(client, GetDirectoryListingPacket());
        }

        // Returns a packet containing the payload with the directory listing.
        static byte[] GetDirectoryListingPacket() {

            byte[] packetOut = new Byte[Constants.PACKET_SIZE];
            for (int i = 0; i < Constants.PACKET_SIZE; i++)
                packetOut[i] = 0;

            packetOut[Constants.FIELD_PACKET_ID] = 0;

            byte[] directoryListingBytes = GetDirectoryListing();
            for (int i = 0; i < directoryListingBytes.Length; i++) {
                packetOut[Constants.FIELD_PAYLOAD + i] = directoryListingBytes[i];
            }

            byte[] checksum = Utils.GetChecksum(directoryListingBytes);
            for (int i = 0; i < checksum.Length; i++) {
                packetOut[Constants.FIELD_CHECKSUM + i] = checksum[i];
            }
            
            return packetOut;
        }

        static byte[] GetDirectoryListing() {

            string directoryListingString = "";
            
            try {

                DirectoryInfo dirInfo = new DirectoryInfo(Directory.GetCurrentDirectory());

                DirectoryInfo[] dirInfos = dirInfo.GetDirectories("*.*");
                foreach (DirectoryInfo d in dirInfos) {
                    directoryListingString += d.Name + Environment.NewLine;
                }

                FileInfo[] fileNames = dirInfo.GetFiles("*.*");
                foreach (FileInfo fi in fileNames) {
                    directoryListingString += fi.Name + Environment.NewLine;
                }
            } catch (UnauthorizedAccessException UAEx) {
                Console.WriteLine(UAEx.Message);
            } catch (PathTooLongException PathEx) {
                Console.WriteLine(PathEx.Message);
            }
            
            return Encoding.Unicode.GetBytes(directoryListingString);
        }

        private static UdpClient LaunchServer() {
        
            UdpClient client = new UdpClient(Constants.SERVER_PORT);
            sendAddress = IPAddress.Parse(Constants.SEND_ADDRESS_STRING);
            remoteIPEndPoint = new IPEndPoint(sendAddress, Constants.CLIENT_PORT);
            client.Connect(remoteIPEndPoint);

            return client;
        }
    }
}
