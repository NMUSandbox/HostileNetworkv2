using System;
using System.Text;
using HostileNetworkUtils;
using System.Net.Sockets;
using System.Net;
using System.ComponentModel;
using System.IO;

namespace HostileNetwork {
    class HostileNetworkServer {

        static IPAddress sendAddress;
        static IPEndPoint remoteIPEndPoint;

        static void Main() {

            //create thread + register functions
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.ProgressChanged += worker_ProgressChanged;

            UdpClient server = launchServer();
            worker.RunWorkerAsync(server);

            Console.ReadLine();
        }

        static void worker_ProgressChanged(object sender, ProgressChangedEventArgs e) {

            Console.WriteLine("progress changed: " + e.UserState);
        }

        static void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {

            Console.WriteLine("all done");
        }

        static void worker_DoWork(object sender, DoWorkEventArgs e) 
        {
            UdpClient server = e.Argument as UdpClient;
            while (true) {
                byte[] receivedBytes = server.Receive(ref remoteIPEndPoint);

                if (receivedBytes[Constants.FIELD_TYPE] == Constants.TYPE_DIRECTORY_REQUEST) {

                    //make sure to send ack
                    byte[] directoryRequestAck = new Byte[Constants.PACKET_SIZE];
                    directoryRequestAck[Constants.FIELD_TYPE] = Constants.TYPE_ACK;
                    Utils.sendTo(server, directoryRequestAck);

                    //send directory metadata packet to client
                    MetadataPacket directoryListingMetadataPacket = new DirectoryMetadataPacket(Constants.TYPE_DIRECTORY_DELIVERY);
                    Utils.sendTo(server, directoryListingMetadataPacket.getBytes);

                    //send directory listing
                    Utils.sendTo(server, getDirectoryListingPacket());
                }
            }
        }

        // Returns a packet containing the payload with the directory listing.
        static byte[] getDirectoryListingPacket() {

            byte[] packetOut = new Byte[Constants.PACKET_SIZE];
            for (int i = 0; i < Constants.PACKET_SIZE; i++)
                packetOut[i] = 0;

            packetOut[Constants.FIELD_PACKET_ID] = 0; // will need to loop over if the dir listing is large?

            byte[] directoryListingBytes = getDirectoryListing();
            for (int i = 0; i < directoryListingBytes.Length; i++) {
                packetOut[Constants.FIELD_PAYLOAD + i] = directoryListingBytes[i];
            }

            byte[] checksum = Utils.getChecksum(directoryListingBytes);
            for (int i = 0; i < checksum.Length; i++) {
                packetOut[Constants.FIELD_CHECKSUM + i] = checksum[i];
            }
            
            return packetOut;
        }

        static byte[] getDirectoryListing() {

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

        private static UdpClient launchServer() {
        
            UdpClient client = new UdpClient(Constants.SERVER_PORT);
            sendAddress = IPAddress.Parse(Constants.SEND_ADDRESS_STRING);
            remoteIPEndPoint = new IPEndPoint(sendAddress, Constants.CLIENT_PORT);
            client.Connect(remoteIPEndPoint);

            return client;
        }
    }
}
