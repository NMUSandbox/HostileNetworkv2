using System;

namespace HostileNetworkUtils {
    public class DirectoryMetadataPacket : Packet {

        private int directoryLength;

        private byte[] myTotalPackets;
        public byte[] MyTotalPackets {
            get { return myTotalPackets; }
            set { myTotalPackets = value; }
        }

        public DirectoryMetadataPacket(byte type, byte[] totalPackets, int directoryLength, int id = -1) 
            : base(type, totalPackets, id) {

            this.directoryLength = directoryLength;
            MyPacketAsBytes = MakePacket();
        }

        public byte[] MakePacket() {

            byte[] packet = new Byte[Constants.PACKET_SIZE];
            packet = Utils.InitializeArray(packet);

            //byte 0
            packet[Constants.FIELD_TYPE] = MyType;

            //bytes 1-4
            for (int i = 0; i < MyTotalPackets.Length; i++) {
                packet[Constants.FIELD_TOTAL_PACKETS + i] = MyTotalPackets[i];
            }

            //bytes 5-9
            byte[] directoryLengthArray = BitConverter.GetBytes(directoryLength);
            for (int i = 0; i < directoryLength; i++) {
                packet[Constants.FIELD_DIRECTORY_LENGTH + i] = directoryLengthArray[i];
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
