using System;

namespace HostileNetworkUtils {
    public class FileMetadataPacket : Packet {

        byte[] fileName;
        int fileLength;
        int fileNameLength;
        private byte[] myTotalPackets;

        public byte[] MyTotalPackets {
            get { return myTotalPackets; }
            set { myTotalPackets = value; }
        }

        public FileMetadataPacket(byte type, byte[] packetAsBytes, int fileLength, 
            int fileNameLength, byte[] fileName, byte[] totalPackets, int id = -1) 
            : base(type, packetAsBytes, id) {

            this.fileLength = fileLength;
            this.fileNameLength = fileNameLength;
            this.fileName = fileName;
            myTotalPackets = totalPackets;

            MyPacketAsBytes = MakePacket();
        }

        public byte[] MakePacket() {

            //maybe move this?
            if (fileName.Length > Constants.MAX_FILENAME_SIZE) {
                Console.WriteLine("ERROR: Filename length too big!");
                return null;
            }

            byte[] packet = new Byte[Constants.PACKET_SIZE];
            packet = Utils.InitializeArray(packet);

            //byte 0
            packet[Constants.FIELD_TYPE] = MyType;

            //bytes 1-4
            for (int i = 0; i < MyTotalPackets.Length; i++) {
                packet[Constants.FIELD_TOTAL_PACKETS + i] = MyTotalPackets[i];
            }

            //bytes 5-8
            byte[] fileLengthArray = BitConverter.GetBytes(fileLength);
            for (int i = 0; i < fileLengthArray.Length; i++) {
                packet[Constants.FIELD_FILE_LENGTH + i] = fileLengthArray[i];
            }

            //bytes 9-13
            byte[] fileNameLengthArray = BitConverter.GetBytes(fileNameLength);
            for (int i = 0; i < fileNameLengthArray.Length; i++) {
                packet[Constants.FIELD_FILENAME_LENGTH + i] = fileNameLengthArray[i];
            }

            //bytes 13-x
            for (int i = 0; i < fileName.Length; i++) {
                packet[Constants.FIELD_FILENAME + i] = fileName[i];
            }

            //bytes 479-511
            MyChecksum = Utils.GetChecksum(packet);
            for (int i = 0; i < MyChecksum.Length; i++) {
                packet[Constants.FIELD_CHECKSUM + i] = MyChecksum[i];
            }

            return packet;
        }
    }
}