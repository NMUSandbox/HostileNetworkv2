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
            if(Constants.DEBUG_PRINTING)Console.WriteLine("receiving file");
            FileMetadata meta = new FileMetadata(metadataAsBytes);
            byte[] receivedBytes = null;
            IPEndPoint dude = null;
            int currentPacket = 0;
            if (File.Exists(Encoding.Default.GetString(meta.filename)))
            {
                if (Constants.DEBUG_PRINTING) Console.WriteLine("file exists, deleting");
                bool success = false;
                while (!success)
                {
                    try
                    {
                        File.Delete(Encoding.Default.GetString(meta.filename));
                        success = true;
                        if (Constants.DEBUG_PRINTING) Console.WriteLine("file deleted");
                    }
                    catch (Exception e)
                    {
                        if(Constants.DEBUG_PRINTING) Console.WriteLine("delete failed!");

                    }
                }
            }
            while (currentPacket < meta.totalPackets)
            {
                if (Constants.DEBUG_PRINTING) Console.WriteLine("receiving a packet");
                receivedBytes = sender.Receive(ref dude);
                if (Constants.DEBUG_PRINTING) Console.WriteLine("Packet received");
                if (Constants.DEBUG_PRINTING) Console.WriteLine(Encoding.ASCII.GetString(receivedBytes));
                if (Utils.VerifyChecksum(receivedBytes))
                {
                    if (Constants.DEBUG_PRINTING) Console.WriteLine("checksum okay");
                    if (receivedBytes[Constants.FIELD_TYPE] == Constants.TYPE_FILE_DELIVERY)
                    {
                        if (Constants.DEBUG_PRINTING) Console.WriteLine("looks like a metadata, fuck that.");
                        SendAckTo(-1, sender);
                        return false;
                    }
                    Data datapack = new Data(receivedBytes);

                    if (datapack.id < meta.totalPackets - 1 && datapack.id == currentPacket)
                    {
                        if (Constants.DEBUG_PRINTING) Console.WriteLine("appending to disk");
                        bool success = false;
                        while (!success)
                        {
                            try
                            {
                                File.AppendAllText(Encoding.Default.GetString(meta.filename), Encoding.Default.GetString(datapack.payload), Encoding.Default);
                                success = true;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }
                        }
                        if (Constants.DEBUG_PRINTING) Console.WriteLine("HDD write done");

                        currentPacket++;
                        if (Constants.DEBUG_PRINTING) Console.WriteLine("incremented currentPacket: " + currentPacket);
                        SendAckTo(datapack.id, sender);
                    }
                    else if (datapack.id == meta.totalPackets - 1 && datapack.id == currentPacket)
                    {
                        if (Constants.DEBUG_PRINTING) Console.WriteLine("last packet");
                        int remainder = (meta.fileLength % Constants.PAYLOAD_SIZE);
                        byte[] finalBytes = new byte[remainder];
                        for (int i = 0; i < remainder; i++)
                        {
                            finalBytes[i] = receivedBytes[i + Constants.FIELD_PAYLOAD];
                        }
                        if (Constants.DEBUG_PRINTING) Console.WriteLine("appending");
                        File.AppendAllText(Encoding.Default.GetString(meta.filename), Encoding.Default.GetString(finalBytes), Encoding.Default);
                        if (Constants.DEBUG_PRINTING) Console.WriteLine("HDD write done");
                        
                        currentPacket++;
                        if (Constants.DEBUG_PRINTING) Console.WriteLine("incremented currentPacket: " + currentPacket);
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
            if (Constants.DEBUG_PRINTING) Console.WriteLine("Metadata packet ready.");
            bool sent = false;
            Stopwatch timeout = new Stopwatch();
            byte[] receivedBytes = null;

            while (!sent){
                Utils.SendTo(target, directoryMetadataPacket.MyPacketAsBytes);
                if (Constants.DEBUG_PRINTING) Console.WriteLine("Metadata sent");
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
                        if (Constants.DEBUG_PRINTING) Console.WriteLine("got something back with a valid ");
                        if (receivedBytes[Constants.FIELD_TYPE] == Constants.TYPE_DIRECTORY_REQUEST) {
                            if (Constants.DEBUG_PRINTING) Console.WriteLine("this looks and smells like a directory request. dumping and starting over");
                            return false; 
                        }

                        if (BitConverter.ToInt32(receivedBytes, Constants.FIELD_ACK_ID) == -1)
                        {
                            if (Constants.DEBUG_PRINTING) Console.WriteLine("This is the ack for our metadata, yay!");
                            sent = true;
                            break;
                        }
                    }
                }
                if (!sent) { if (Constants.DEBUG_PRINTING)   Console.WriteLine("Timeout"); }
            }

            if (Constants.DEBUG_PRINTING) Console.WriteLine("Okay... so metadata is sent. time to send the directory proper");
            if (Constants.DEBUG_PRINTING) Console.WriteLine("Sending in this many packets: " + totalPackets);
            int byteIndex = 0;
            for (int i = 0; i < totalPackets; i++)
            {
                byte[] stagedPayload =  new byte[Constants.PAYLOAD_SIZE];
                stagedPayload = Utils.InitializeArray(stagedPayload);
                for(int j=0; j<stagedPayload.Length;j++){
                    if (j < directoryListing.Length && byteIndex<directoryListing.Length)
                    {
                        stagedPayload[j] = directoryListing[byteIndex];
                        byteIndex++;
                    }
                }
                if (Constants.DEBUG_PRINTING) Console.WriteLine("payload staged");
                DataPacket stagedPacket = new DataPacket(stagedPayload, i);
                if (Constants.DEBUG_PRINTING) Console.WriteLine("packet ready, sending");
                sendUntilAck(stagedPacket, target);
                if (Constants.DEBUG_PRINTING) Console.WriteLine("sent");
            }
            return true;
        }
        public static bool StartReceiveDirectory(byte[] metadataAsBytes, UdpClient target) {
            if (!Utils.VerifyChecksum(metadataAsBytes))
            {
                if (Constants.DEBUG_PRINTING) Console.WriteLine("checksum failed right off the bat in ReceiveDirectoryFrom");
                return false;
            }
            AckPacket ack = new AckPacket(-1);
            if (Constants.DEBUG_PRINTING) Console.WriteLine("sending an ack");
            Utils.SendTo(target, ack.MyPacketAsBytes);
      // DirMetadata meta = new DirMetadata(metadataAsBytes);

            bool success = false;
            while (!success)
            {
                success = CatchMetadataResend(metadataAsBytes, target);
            }
            if (Constants.DEBUG_PRINTING) Console.WriteLine("receive directory complete");
            return true;

        }
        public static bool CatchMetadataResend(byte[] metadataAsBytes, UdpClient target){
            byte[] receivedBytes = null;
            IPEndPoint dude = null;
            if (Constants.DEBUG_PRINTING) Console.WriteLine("set to receive the first datapacket");
            DirMetadata meta = new DirMetadata(metadataAsBytes);
            bool received = false;
            while (!received)
            {
                receivedBytes = target.Receive(ref dude);
                if (Utils.VerifyChecksum(receivedBytes))
                {
                    if (receivedBytes[Constants.FIELD_TYPE] == Constants.TYPE_DIRECTORY_DELIVERY)
                    {
                        AckPacket ack = new AckPacket(-1);
                        if (Constants.DEBUG_PRINTING) Console.WriteLine("first expected data was actually metadata, acking, restarting");
                        Utils.SendTo(target, ack.MyPacketAsBytes);
                        return false;
                    }
                    received = true;
                }
                else
                {
                    if (Constants.DEBUG_PRINTING) Console.WriteLine("checksum failure");
                }

            }
            return ReceiveAllDirectoryPackets(receivedBytes, meta, target);
        }//end 
        public static bool ReceiveAllDirectoryPackets(byte[] first, DirMetadata meta, UdpClient target)
        {
            IPEndPoint IPRef = null;
            if(first == null){return false;}
            Data data = new Data(first);
            Console.WriteLine(Encoding.Default.GetString(data.payload));

            if (Constants.DEBUG_PRINTING) Console.WriteLine("sending ack for first packet");
            SendAckTo(data.id, target);
            if (Constants.DEBUG_PRINTING) Console.WriteLine("ready to receive the other packets. looking for: " + meta.totalPackets + "-1");
            int currentPacket = 1;
            while (currentPacket < meta.totalPackets)
            {
                byte[] recBytes = target.Receive(ref IPRef);
                if (Constants.DEBUG_PRINTING) Console.WriteLine("packet recieved!");
                if (!Utils.VerifyChecksum(recBytes)) {
                    if (Constants.DEBUG_PRINTING) Console.WriteLine("checksum failed on datagram");
                    continue; 
                }
                data = new Data(recBytes);
                if (data.id < currentPacket)
                {
                    SendAckTo(data.id, target);
                    if (Constants.DEBUG_PRINTING) Console.WriteLine("we recieved a previously seen packet. sending another ack and looing for a new one.");
                }
                else if (data.id == currentPacket && currentPacket != meta.totalPackets-1)
                {
                    if (Constants.DEBUG_PRINTING) Console.WriteLine("standard case.");
                    Console.WriteLine(Encoding.Default.GetString(data.payload));
                    if (Constants.DEBUG_PRINTING) Console.WriteLine("Acking: " + data.id);
                    SendAckTo(data.id, target);
                    currentPacket++;
                }
                else if (data.id == currentPacket && currentPacket == meta.totalPackets - 1)
                {
                    if (Constants.DEBUG_PRINTING) Console.WriteLine("last datagram special case");

                    byte[] finalBytes = new byte[(meta.dirLength % Constants.PAYLOAD_SIZE)];
                    for (int i = 0; i < finalBytes.Length; i++)
                    {
                        finalBytes[i] = data.payload[i + Constants.FIELD_PAYLOAD];
                    }
                    Console.WriteLine(Encoding.ASCII.GetString(finalBytes));
                    currentPacket++;
              
                    SendAckTo(data.id, target);
                }
                else
                {//lolwat
                    Console.WriteLine("something went wrong...");
                }
            }
            if (Constants.DEBUG_PRINTING) Console.WriteLine("all packets received");
            return true;
        }

    }//end of class
    public struct FileMetadata {
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
        //    if (Constants.DEBUG_PRINTING) Console.WriteLine("building file metadata, filenameLength =  " + filenameLength);
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
    public struct Data {
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
    public struct DirMetadata {
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