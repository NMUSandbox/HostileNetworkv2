using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace HostileNetworkUtils {
    public class Utils {

        public static int GetDirectoryPacketsTotal() {

            int totalPackets = 0;
            byte[] directoryListingByteArray = GetDirectoryListing();

            totalPackets = directoryListingByteArray.Length / (Constants.PAYLOAD_SIZE);

            if (directoryListingByteArray.Length % (Constants.PAYLOAD_SIZE) != 0 || totalPackets < 1)
                totalPackets++;

            return totalPackets;
        }

        public static byte[] GetDirectoryListing() {

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
            }
            catch (UnauthorizedAccessException UAEx) {
                Console.WriteLine(UAEx.Message);
            }
            catch (PathTooLongException PathEx) {
                Console.WriteLine(PathEx.Message);
            }

            return Encoding.Default.GetBytes(directoryListingString);
        }

        public static byte[] InitializeArray(byte[] arr) {

            for (int i = 0; i < arr.Length; i++) {
                arr[i] = 0;
            }
            return arr;
        }

        public static byte[] GetChecksum(byte[] input) {

            return MD5.Create().ComputeHash(input);
        }

        public static bool VerifyChecksum(byte[] received) {

            byte[] inputChecksum = new Byte[16];
            for (int i = 0; i < inputChecksum.Length; i++) {
                inputChecksum[i] = received[i + Constants.FIELD_CHECKSUM];
                received[i + Constants.FIELD_CHECKSUM] = 0;
            }
            byte[] actualChecksum = Utils.GetChecksum(received);

            if (inputChecksum.Length != actualChecksum.Length) {
                return false;
            }

            for (int i = 0; i < inputChecksum.Length; i++) {
                if (inputChecksum[i] != actualChecksum[i]) {
                    return false;
                }
            }

            return true;
        }

        public static long GetFileSizeInBytes(string fileName) {

            FileInfo fileInfo = new FileInfo(fileName);

            return fileInfo.Length;
        }

        public static int SendTo(UdpClient target, byte[] packet) {

            Random randomnessGenerator = new Random(DateTime.Now.Millisecond);

            if (randomnessGenerator.NextDouble() < Constants.SIMULATION_DROP_RATE && Constants.DEBUG_DROP_AND_CORRUPT) {
                return -1;
            }

            if (randomnessGenerator.NextDouble() < Constants.SIMULATION_CORRPUTION_RATE && Constants.DEBUG_DROP_AND_CORRUPT) {
                for (int i = 0; i < 5; i++) {
                    packet[randomnessGenerator.Next(packet.GetLength(0))] = (byte)randomnessGenerator.Next(255);
                }
                target.Send(packet, packet.GetLength(0));
                return -2;
            }
            target.Send(packet, packet.GetLength(0));

            return 1;
        }

        public static void ReceiveFile(UdpClient udpSource, byte[] metadata) {

            int currentWorkingPacket = 0;
            int filenameSize = BitConverter.ToInt32(metadata, Constants.FIELD_FILENAME_LENGTH);
            int fileLength = BitConverter.ToInt32(metadata, Constants.FIELD_FILE_LENGTH);
            int packetTotal = BitConverter.ToInt32(metadata, Constants.FIELD_TOTAL_PACKETS);
            byte[] filenameBytes = new byte[filenameSize];
            for (int i = 0; i < filenameSize; i++) {
                filenameBytes[i] = metadata[i + Constants.FIELD_FILENAME];
            }
            string filename = Encoding.Unicode.GetString(filenameBytes);

            List<dataPacketBuffer> buffer = new List<dataPacketBuffer>();

            IPEndPoint remoteIPEndPoint = null;
            while (currentWorkingPacket < packetTotal) {
                byte[] receivedBytes = udpSource.Receive(ref remoteIPEndPoint);

                if (VerifyChecksum(receivedBytes)) {
                    byte[] payloadUnpacker = new byte[Constants.PAYLOAD_SIZE];
                    for (int i = 0; i < Constants.PAYLOAD_SIZE; i++) {
                        payloadUnpacker[i] = receivedBytes[i + Constants.FIELD_PAYLOAD];
                    }
                    dataPacketBuffer receivedPacket = new dataPacketBuffer(BitConverter.ToInt32(receivedBytes, Constants.FIELD_PACKET_ID), payloadUnpacker);

                    if (receivedPacket.getID() < currentWorkingPacket) {
                        AckPacket respond = new AckPacket(receivedPacket.getID());
                        SendTo(udpSource, respond.MakePacket());
                    }
                    else if (receivedPacket.getID() == currentWorkingPacket) {
                        File.AppendAllText(filename, Encoding.Default.GetString(payloadUnpacker));
                    }
                    else if (receivedPacket.getID() > currentWorkingPacket) {
                        buffer.Add(receivedPacket);
                    }
                }

                buffer.Sort(
                    delegate(dataPacketBuffer s1, dataPacketBuffer s2) {
                        return s1.getID().CompareTo(s2.getID());
                    }
                );
                foreach (dataPacketBuffer item in buffer) {
                    if (item.getID() == currentWorkingPacket) {
                        File.AppendAllText(filename, Encoding.Unicode.GetString(item.getPayload()));
                        buffer.Remove(item);
                        currentWorkingPacket++;
                    }
                }
            }
        }

        struct dataPacketBuffer {
            private int ID;
            private byte[] payload;
            public dataPacketBuffer(int inID, byte[] inPayload) {
                ID = inID;
                payload = inPayload;
            }
            public void setPayload(byte[] inArr) { payload = inArr; }
            public byte[] getPayload() { return payload; }
            public void setID(int newID) { ID = newID; }
            public int getID() { return ID; }
        };
    }
}