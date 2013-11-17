using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HostileNetworkUtils {
    public class PingPong {

        public static void sendUntilAck(Packet sendingPacket, UdpClient target) {
            IPEndPoint remoteIPEndPoint = null;
            bool sent = false;
            Stopwatch timeout = new Stopwatch();
            byte[] backup = new byte[Constants.PACKET_SIZE];
            for (int i = 0; i < Constants.PACKET_SIZE; i++) {
                backup[i] = sendingPacket.MyPacketAsBytes[i];
            }

            while (!sent) {
                for (int i = 0; i < Constants.PACKET_SIZE; i++) {
                    backup[i] = sendingPacket.MyPacketAsBytes[i];
                }
                Utils.SendTo(target, backup);
                timeout.Restart();
                while (timeout.ElapsedMilliseconds < Constants.PACKET_TIMEOUT_MILLISECONDS) {
                    if (target.Available != 0) {
                        byte[] receivedBytes = target.Receive(ref remoteIPEndPoint);

                        if (Utils.VerifyChecksum(receivedBytes)) {
                            if (receivedBytes[Constants.FIELD_TYPE] == Constants.TYPE_ACK) {
                                int receivedID = BitConverter.ToInt32(receivedBytes, Constants.FIELD_ACK_ID);
                                int sendingID = BitConverter.ToInt32(sendingPacket.MyPacketAsBytes, Constants.FIELD_PACKET_ID);
                                if (sendingPacket.getMyType() != Constants.TYPE_DATA) {
                                    sendingID = -1;
                                }

                                if (receivedID == sendingID) {
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void SendAckTo(int id, UdpClient target) {
            AckPacket theAckToSend = new AckPacket(id);
            Utils.SendTo(target, theAckToSend.MyPacketAsBytes);
        }

        public static bool SendFileTo(string filename, UdpClient target) {

            FileInfo localMeta;
            byte[] fileNameAsBytes;
            int totalPackets;

            try {
                localMeta = new FileInfo(filename);

                fileNameAsBytes = Encoding.Default.GetBytes(filename);

                totalPackets = (int)localMeta.Length / Constants.PAYLOAD_SIZE;

                if ((int)localMeta.Length % Constants.PAYLOAD_SIZE != 0 || totalPackets < 1) {
                    totalPackets++;
                }
            }
            catch (ArgumentException ex) {
                return false;
            }
            catch (FileNotFoundException ex) {

                throw new FileNotFoundException();
            }

            FileMetadataPacket meta = new FileMetadataPacket(Constants.TYPE_FILE_DELIVERY, (int)localMeta.Length, filename.Length, fileNameAsBytes, totalPackets);
            sendUntilAck(meta, target);

            try {
                StreamReader localFile = new StreamReader(filename, Encoding.Default);
                for (int i = 0; i < totalPackets; i++) {

                    char[] stagedPayload = new char[Constants.PAYLOAD_SIZE];
                    localFile.Read(stagedPayload, 0, Constants.PAYLOAD_SIZE);
                    byte[] encoded = Encoding.Default.GetBytes(stagedPayload);
                    DataPacket stagedPacket = new DataPacket(Encoding.Default.GetBytes(stagedPayload), i);

                    sendUntilAck(stagedPacket, target);

                }
                localFile.Close();
            }
            catch (UnauthorizedAccessException ex) {
                Console.WriteLine("Insufficient permissions to access specified file: " + ex.Message);
            }
            catch (IOException ex) {
                
            }
            
            return true;
        }

        public static bool ReceiveFileFrom(byte[] metadataAsBytes, UdpClient sender) {

            FileMetadata meta = new FileMetadata(metadataAsBytes);
            byte[] receivedBytes = null;
            IPEndPoint remoteIPEndPoint = null;
            int currentPacket = 0;
            if (File.Exists(Encoding.Default.GetString(meta.filename))) {
                bool success = false;
                while (!success) {
                    try {
                        File.Delete(Encoding.Default.GetString(meta.filename));
                        success = true;
                    }
                    catch (IOException ex) {
                        return false;
                    }
                    catch (Exception ex) {
                        return false;
                    }
                }
            }
            while (currentPacket < meta.totalPackets) {
                receivedBytes = sender.Receive(ref remoteIPEndPoint);
                if (Utils.VerifyChecksum(receivedBytes)) {
                    if (receivedBytes[Constants.FIELD_TYPE] == Constants.TYPE_FILE_DELIVERY) {
                        SendAckTo(-1, sender);
                        return false;
                    }
                    Data datapack = new Data(receivedBytes);

                    if (datapack.id < meta.totalPackets - 1 && datapack.id == currentPacket) {
                        bool success = false;
                        while (!success) {
                            try {
                                File.AppendAllText(Encoding.Default.GetString(meta.filename), Encoding.Default.GetString(datapack.payload), Encoding.Default);
                                success = true;
                            }
                            catch (Exception e) {
                                Console.WriteLine(e.Message);
                            }
                        }

                        currentPacket++;
                        SendAckTo(datapack.id, sender);
                    }
                    else if (datapack.id == meta.totalPackets - 1 && datapack.id == currentPacket) {
                        int remainder = (meta.fileLength % Constants.PAYLOAD_SIZE);
                        byte[] finalBytes = new byte[remainder];
                        for (int i = 0; i < remainder; i++) {
                            finalBytes[i] = receivedBytes[i + Constants.FIELD_PAYLOAD];
                        }
                        File.AppendAllText(Encoding.Default.GetString(meta.filename), Encoding.Default.GetString(finalBytes), Encoding.Default);

                        currentPacket++;
                        SendAckTo(datapack.id, sender);
                    }
                    else if (datapack.id < currentPacket) {
                        SendAckTo(datapack.id, sender);
                    }
                }
            }
            return true;
        }

        public static bool SendDirectoryTo(UdpClient target) {

            IPEndPoint remoteIPEndPoint = null;
            byte[] directoryListing = Utils.GetDirectoryListing();
            int totalPackets = Utils.GetDirectoryPacketsTotal();
            DirectoryMetadataPacket directoryMetadataPacket = new DirectoryMetadataPacket(Constants.TYPE_DIRECTORY_DELIVERY, totalPackets, directoryListing.Length);
            bool sent = false;
            Stopwatch timeout = new Stopwatch();
            byte[] receivedBytes = null;

            while (!sent) {
                Utils.SendTo(target, directoryMetadataPacket.MyPacketAsBytes);
                timeout.Restart();
                while (timeout.ElapsedMilliseconds < Constants.PACKET_TIMEOUT_MILLISECONDS) {
                    if (target.Available != 0) {
                        receivedBytes = target.Receive(ref remoteIPEndPoint);
                        if (!Utils.VerifyChecksum(receivedBytes)) {
                            continue;
                        }
                        if (receivedBytes[Constants.FIELD_TYPE] == Constants.TYPE_DIRECTORY_REQUEST) {
                            return false;
                        }

                        if (BitConverter.ToInt32(receivedBytes, Constants.FIELD_ACK_ID) == -1) {
                            sent = true;
                            break;
                        }
                    }
                }
            }

            int byteIndex = 0;
            for (int i = 0; i < totalPackets; i++) {
                byte[] stagedPayload = new byte[Constants.PAYLOAD_SIZE];
                stagedPayload = Utils.InitializeArray(stagedPayload);
                for (int j = 0; j < stagedPayload.Length; j++) {
                    if (j < directoryListing.Length && byteIndex < directoryListing.Length) {
                        stagedPayload[j] = directoryListing[byteIndex];
                        byteIndex++;
                    }
                }
                DataPacket stagedPacket = new DataPacket(stagedPayload, i);
                sendUntilAck(stagedPacket, target);
            }
            return true;
        }

        public static bool StartReceiveDirectory(byte[] metadataAsBytes, UdpClient target) {

            if (!Utils.VerifyChecksum(metadataAsBytes)) {
                return false;
            }
            AckPacket ack = new AckPacket(-1);
            Utils.SendTo(target, ack.MyPacketAsBytes);

            bool success = false;
            while (!success) {
                success = CatchMetadataResend(metadataAsBytes, target);
            }
            return true;

        }

        public static bool CatchMetadataResend(byte[] metadataAsBytes, UdpClient target) {

            byte[] receivedBytes = null;
            IPEndPoint remoteIPEndPoint = null;
            DirMetadata directoryMetadata = new DirMetadata(metadataAsBytes);
            bool received = false;
            while (!received) {
                receivedBytes = target.Receive(ref remoteIPEndPoint);
                if (Utils.VerifyChecksum(receivedBytes)) {
                    if (receivedBytes[Constants.FIELD_TYPE] == Constants.TYPE_DIRECTORY_DELIVERY) {
                        AckPacket ack = new AckPacket(-1);
                        Utils.SendTo(target, ack.MyPacketAsBytes);
                        return false;
                    }
                    received = true;
                }

            }
            return ReceiveAllDirectoryPackets(receivedBytes, directoryMetadata, target);
        }

        public static bool ReceiveAllDirectoryPackets(byte[] first, DirMetadata meta, UdpClient target) {

            IPEndPoint remoteIPEndPoint = null;
            if (first == null) { return false; }
            Data data = new Data(first);

            if (meta.dirLength < Constants.PAYLOAD_SIZE)
                Console.WriteLine(Encoding.Default.GetString(data.payload,0,meta.dirLength));
            else
                Console.WriteLine(Encoding.Default.GetString(data.payload));

            SendAckTo(data.id, target);
            int currentPacket = 1;
            while (currentPacket < meta.totalPackets) {
                byte[] recBytes = target.Receive(ref remoteIPEndPoint);
                if (!Utils.VerifyChecksum(recBytes)) {
                    continue;
                }
                data = new Data(recBytes);
                if (data.id < currentPacket) {
                    SendAckTo(data.id, target);
                }
                else if (data.id == currentPacket && currentPacket != meta.totalPackets - 1) {
                    Console.WriteLine(Encoding.Default.GetString(data.payload));
                    SendAckTo(data.id, target);
                    currentPacket++;
                }
                else if (data.id == currentPacket && currentPacket == meta.totalPackets - 1) {

                    byte[] finalBytes = new byte[(meta.dirLength % Constants.PAYLOAD_SIZE)];
                    for (int i = 0; i < finalBytes.Length; i++) {
                        finalBytes[i] = data.payload[i + Constants.FIELD_PAYLOAD];
                    }
                    Console.WriteLine(Encoding.Default.GetString(finalBytes));
                    currentPacket++;

                    SendAckTo(data.id, target);
                }
            }
            return true;
        }

    }

    public struct FileMetadata {
        public byte type;
        public int totalPackets;
        public int fileLength;
        public int filenameLength;
        public byte[] filename;
        public byte[] checksum;
        public FileMetadata(byte[] receivedBytes) {
            type = receivedBytes[Constants.FIELD_TYPE];
            totalPackets = BitConverter.ToInt32(receivedBytes, Constants.FIELD_TOTAL_PACKETS);
            fileLength = BitConverter.ToInt32(receivedBytes, Constants.FIELD_FILE_LENGTH);
            filenameLength = BitConverter.ToInt32(receivedBytes, Constants.FIELD_FILENAME_LENGTH);
            //    if (Constants.DEBUG_PRINTING) Console.WriteLine("building file metadata, filenameLength =  " + filenameLength);
            filename = new byte[filenameLength];
            for (int i = 0; i < filename.Length; i++) {
                filename[i] = receivedBytes[i + Constants.FIELD_FILENAME];
            }
            checksum = new byte[16];
            for (int i = 0; i < checksum.Length; i++) {
                checksum[i] = receivedBytes[i + Constants.FIELD_CHECKSUM];
            }
        }
    };
    public struct Data {
        public int id;
        public byte[] payload;
        public byte[] checksum;

        public Data(byte[] receivedBytes) {
            id = BitConverter.ToInt32(receivedBytes, Constants.FIELD_PACKET_ID);
            payload = new byte[Constants.PAYLOAD_SIZE];
            for (int i = 0; i < payload.Length; i++) {
                payload[i] = receivedBytes[i + Constants.FIELD_PAYLOAD];
            }
            checksum = new byte[16];
            for (int i = 0; i < checksum.Length; i++) {
                checksum[i] = receivedBytes[i + Constants.FIELD_CHECKSUM];
            }
        }
    };
    public struct DirMetadata {
        public byte type;
        public int totalPackets;
        public int dirLength;
        public byte[] checksum;
        public DirMetadata(byte[] receivedBytes) {
            type = receivedBytes[Constants.FIELD_TYPE];
            totalPackets = BitConverter.ToInt32(receivedBytes, Constants.FIELD_TOTAL_PACKETS);
            dirLength = BitConverter.ToInt32(receivedBytes, Constants.FIELD_DIRECTORY_LENGTH);
            checksum = new byte[16];
            for (int i = 0; i < checksum.Length; i++) {
                checksum[i] = receivedBytes[i + Constants.FIELD_CHECKSUM];
            }
        }
    };
}