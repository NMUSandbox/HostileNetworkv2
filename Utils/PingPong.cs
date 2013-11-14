using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Diagnostics;

/*
 * PINGPONG methods in this class all return true if they pass, false if they don't.
 * To use these methods, loop on them until they return true. If they hang it's because
 * something is really fucked network wise. Like, your server died. The sort of thing 
 * you just kinda give up on. Hanging until the user force-quits is a sort of giving
 * up.  
 */


namespace HostileNetworkUtils {
    public class PingPong {

        //send some packet over and over until you get a valid ACK
        //handles timeouts, handles a lack of replies. 
        //looks ugly as sin
        //also fuck you, give me access to Packet's ID param. :P
        public static void sendUntilAck(Packet sendingPacket, UdpClient target){
            IPEndPoint remoteTarget = null;
            bool sent = false;
            Stopwatch timeout = new Stopwatch();
            byte[] backup = new byte[Constants.PACKET_SIZE];
            for (int i = 0; i < Constants.PACKET_SIZE; i++)
            {
                backup[i] = sendingPacket.MyPacketAsBytes[i];
            }

            while (!sent)
            {
                for (int i = 0; i < Constants.PACKET_SIZE; i++)
                {
                    backup[i] = sendingPacket.MyPacketAsBytes[i];
                }
                Utils.SendTo(target, backup);
                timeout.Restart();
                while (timeout.ElapsedMilliseconds < Constants.PACKET_TIMEOUT_MILLISECONDS)
                {
                    if (target.Available != 0)
                    {
                        byte[] receivedBytes = target.Receive(ref remoteTarget);

                        if (Utils.VerifyChecksum(receivedBytes))
                        {
                            if (receivedBytes[Constants.FIELD_TYPE] == Constants.TYPE_ACK)
                            {
                                int receivedID = BitConverter.ToInt32(receivedBytes, Constants.FIELD_ACK_ID);
                                int sendingID = BitConverter.ToInt32(sendingPacket.MyPacketAsBytes, Constants.FIELD_PACKET_ID);
                                if (sendingPacket.getMyType() != Constants.TYPE_DATA)
                                {
                                    sendingID = -1;
                                }

                                if (receivedID == sendingID) {
                                    if (Constants.DEBUG_PRINTING)
                                    {
                                        Console.WriteLine("Packet sent and acknowledged: "+sendingID);
                                    }
                                    return; 
                                }
                            }
                        }
                        else
                        {
                            if(Constants.DEBUG_PRINTING)Console.WriteLine("Checksum failed on ack");
                        }
                    }
                }
                if (Constants.DEBUG_PRINTING) Console.WriteLine("Timeout");
            }
        }
        public static void SendAckTo(int id, UdpClient target)
        {
            AckPacket theAckToSend = new AckPacket(id);
            Utils.SendTo(target, theAckToSend.MyPacketAsBytes);
        }
        public static bool SendFileTo(string filename, UdpClient target) {
     
            FileInfo localMeta = new FileInfo(filename);
            
            byte[] fileNameAsBytes = Encoding.Default.GetBytes(filename);

            int totalPackets = (int)localMeta.Length / Constants.PAYLOAD_SIZE;

            if ((int)localMeta.Length % Constants.PAYLOAD_SIZE != 0 || totalPackets < 1)
            {
                totalPackets++;
            }


            FileMetadataPacket meta = new FileMetadataPacket(Constants.TYPE_FILE_DELIVERY, (int)localMeta.Length, filename.Length, fileNameAsBytes, totalPackets);
            sendUntilAck(meta, target);


            StreamReader localFile = new StreamReader(filename,Encoding.Default);
            for (int i = 0; i < totalPackets; i++)
            {

                char[] stagedPayload = new char[Constants.PAYLOAD_SIZE];
                localFile.Read(stagedPayload, 0, Constants.PAYLOAD_SIZE);
                byte[] encoded = Encoding.Default.GetBytes(stagedPayload);
                DataPacket stagedPacket = new DataPacket(Encoding.Default.GetBytes(stagedPayload), i);

                sendUntilAck(stagedPacket, target);

            }
            return true;
        }






        public static bool ReceiveFileFrom(byte[] metadataAsBytes, UdpClient sender)
        {
            Console.WriteLine("receiving file");
            FileMetadata meta = new FileMetadata(metadataAsBytes);
            byte[] receivedBytes = null;
            IPEndPoint dude = null;
            int currentPacket = 0;
            if (File.Exists(Encoding.Default.GetString(meta.filename)))
            {
                File.Delete(Encoding.Default.GetString(meta.filename));
            }
            while (currentPacket < meta.totalPackets)
            {
                Console.WriteLine("receiving a packet");
                receivedBytes = sender.Receive(ref dude);
                Console.WriteLine("Packet received");
             //   Console.WriteLine(Encoding.ASCII.GetString(receivedBytes));
                if (Utils.VerifyChecksum(receivedBytes))
                {
                    Console.WriteLine( "checksum okay");
                    if (receivedBytes[Constants.FIELD_TYPE] == Constants.TYPE_FILE_DELIVERY)
                    {
                        Console.WriteLine("looks like a metadata, fuck that.");
                        SendAckTo(-1, sender);
                        return false;
                    }
                    Data datapack = new Data(receivedBytes);

                    if (datapack.id < meta.totalPackets - 1 && datapack.id == currentPacket)
                    {
                        Console.WriteLine("appending to disk");
                        File.AppendAllText(Encoding.Default.GetString(meta.filename), Encoding.Default.GetString(datapack.payload), Encoding.Default);
                        Console.WriteLine("HDD write done");

                        currentPacket++;
                        Console.WriteLine("incremented currentPacket: " + currentPacket);
                        SendAckTo(datapack.id, sender);
                    }
                    else if (datapack.id == meta.totalPackets - 1 && datapack.id == currentPacket)
                    {
                        Console.WriteLine("last packet");
                        int remainder = (meta.fileLength % Constants.PAYLOAD_SIZE);
                        byte[] finalBytes = new byte[remainder];
                        for (int i = 0; i < remainder; i++)
                        {
                            finalBytes[i] = receivedBytes[i + Constants.FIELD_PAYLOAD];
                        }
                        Console.WriteLine("appending");
                        File.AppendAllText(Encoding.Default.GetString(meta.filename), Encoding.Default.GetString(finalBytes), Encoding.Default);
                        Console.WriteLine("HDD write done");
                        
                        currentPacket++;
                        Console.WriteLine("incremented currentPacket: " +currentPacket);
                        SendAckTo(datapack.id, sender);
                    }
                    else if (datapack.id < currentPacket)
                    {
                        SendAckTo(datapack.id, sender);
                    }
                }
                else
                {
                    Console.WriteLine("checksum failure");
                }
            }
            return true;
        }









        public static bool SendDirectoryTo(UdpClient target) 
        {

            IPEndPoint IPref=null;
            byte[] directoryListing = Utils.GetDirectoryListing();
            int totalPackets = Utils.GetDirectoryPacketsTotal();
            DirectoryMetadataPacket directoryMetadataPacket = new DirectoryMetadataPacket(Constants.TYPE_DIRECTORY_DELIVERY, totalPackets, directoryListing.Length);
           // Utils.SendTo(target, directoryMetadataPacket.MyPacketAsBytes);
            Console.WriteLine("Metadata packet ready.");
            bool sent = false;
            Stopwatch timeout = new Stopwatch();
            byte[] receivedBytes = null;

            while (!sent){
                Utils.SendTo(target, directoryMetadataPacket.MyPacketAsBytes);
                Console.WriteLine("Metadata sent");
                timeout.Restart();
                while (timeout.ElapsedMilliseconds < Constants.PACKET_TIMEOUT_MILLISECONDS)
                {
                    if (target.Available != 0)
                    {
                        receivedBytes = target.Receive(ref IPref);

                        if (!Utils.VerifyChecksum(receivedBytes))
                        {
                            continue;
                        }
                        if (receivedBytes[Constants.FIELD_TYPE] == Constants.TYPE_DIRECTORY_REQUEST) { return false; }

                        if (BitConverter.ToInt32(receivedBytes, Constants.FIELD_ACK_ID) == -1)
                        {
                            sent = true;
                            break;
                        }
                    }
                }
                Console.WriteLine("Timeout");
            }
            int byteIndex = 0;
            for (int i = 0; i < totalPackets-1; i++)
            {
                byte[] stagedPayload =  new byte[Constants.PAYLOAD_SIZE];
                stagedPayload = Utils.InitializeArray(stagedPayload);
                for(int j=0; j<stagedPayload.Length;j++){
                    if (j < directoryListing.Length)
                    {
                        stagedPayload[j] = directoryListing[byteIndex++];
                    }
                }
                DataPacket stagedPacket = new DataPacket(stagedPayload, i);
                sendUntilAck(stagedPacket, target);
            }
            return true;
        }
        public static bool ReceiveDirectoryFrom(byte[] metadataAsBytes, UdpClient sender) {
            if (!Utils.VerifyChecksum(metadataAsBytes))
            {
                Console.WriteLine("checksum failed right off the bat in ReceiveDirectoryFrom");
                return false;
            }
            DirMetadata meta = new DirMetadata(metadataAsBytes);
            AckPacket ack = new AckPacket(-1);
            Console.WriteLine("sending an ack");
            Utils.SendTo(sender, ack.MyPacketAsBytes);


            byte[] receivedBytes = null;
            IPEndPoint dude = null;
            int currentPacket = 0;
            Console.WriteLine("set to receive the packets");

            while (currentPacket < meta.totalPackets)
            {
                receivedBytes = sender.Receive(ref dude);
                if (Utils.VerifyChecksum(receivedBytes))
                {
                    if (receivedBytes[Constants.FIELD_TYPE] == Constants.TYPE_DIRECTORY_DELIVERY)
                    {
                        return false;
                    }
                    Data datapack = new Data(receivedBytes);

                    if (datapack.id < meta.totalPackets - 1 && datapack.id == currentPacket)
                    {
                        Console.WriteLine(Encoding.Default.GetString(datapack.payload));
                    }
                    else if (datapack.id == meta.totalPackets - 1 && datapack.id == currentPacket)
                    {
                        int remainder = (meta.dirLength % Constants.PAYLOAD_SIZE);
                        byte[] finalBytes = new byte[remainder];
                        for (int i = 0; i < remainder; i++)
                        {
                            finalBytes[i] = receivedBytes[i + Constants.FIELD_PAYLOAD];
                        }
                        Console.WriteLine(Encoding.Default.GetString(finalBytes)) ;
                    }
                    currentPacket++;

                    SendAckTo(datapack.id, sender);
                }
                else
                {
                    Console.WriteLine("checksum failure");
                }
            }
            return true;
        }

    }
    struct FileMetadata {
        public byte type;
        public int totalPackets;
        public int fileLength;
        public int filenameLength;
        public byte[] filename;
        public byte[] checksum;
        public FileMetadata(byte[] receivedBytes)
        {
            type = receivedBytes[Constants.FIELD_TYPE];
            totalPackets = BitConverter.ToInt32(receivedBytes, Constants.FIELD_TOTAL_PACKETS);
            fileLength = BitConverter.ToInt32(receivedBytes, Constants.FIELD_FILE_LENGTH);
            filenameLength = BitConverter.ToInt32(receivedBytes, Constants.FIELD_FILENAME_LENGTH);
            filename = new byte[filenameLength];
            for (int i = 0; i < filename.Length; i++)
            {
                filename[i] = receivedBytes[i + Constants.FIELD_FILENAME];
            }
            checksum = new byte[16];
            for (int i = 0; i < checksum.Length; i++)
            {
                checksum[i] = receivedBytes[i + Constants.FIELD_CHECKSUM];
            }
        }
    };
    struct Data {
        public int id;
        public byte[] payload;
        public byte[] checksum;

        public Data(byte[] receivedBytes)
        {
            id = BitConverter.ToInt32(receivedBytes, Constants.FIELD_PACKET_ID);
            payload = new byte[Constants.PAYLOAD_SIZE];
            for (int i = 0; i < payload.Length; i++)
            {
                payload[i] = receivedBytes[i + Constants.FIELD_PAYLOAD];
            } 
            checksum = new byte[16];
            for (int i = 0; i < checksum.Length; i++)
            {
                checksum[i] = receivedBytes[i + Constants.FIELD_CHECKSUM];
            }
        }
    };
    struct DirMetadata {
        public byte type;
        public int totalPackets;
        public int dirLength;
        public byte[] checksum;
        public DirMetadata(byte[] receivedBytes)
        {
            type = receivedBytes[Constants.FIELD_TYPE];
            totalPackets = BitConverter.ToInt32(receivedBytes, Constants.FIELD_TOTAL_PACKETS);
            dirLength = BitConverter.ToInt32(receivedBytes, Constants.FIELD_DIRECTORY_LENGTH);
            checksum = new byte[16];
            for (int i = 0; i < checksum.Length; i++)
            {
                checksum[i] = receivedBytes[i + Constants.FIELD_CHECKSUM];
            }
        }
    };
}