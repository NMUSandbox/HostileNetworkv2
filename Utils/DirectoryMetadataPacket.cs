using System;

namespace HostileNetworkUtils {
    public class DirectoryMetadataPacket : Packet {

        private int directoryLength;

        private int myTotalPackets;
        public int MyTotalPackets {
            get { return myTotalPackets; }
            set { myTotalPackets = value; }
        }

        public DirectoryMetadataPacket(byte type, int totalPackets, int directoryLength, int id = -1)
            : base(type, id) {

            this.directoryLength = directoryLength;
            myTotalPackets = totalPackets;

            MyPacketAsBytes = MakePacket();
        }

        public DirectoryMetadataPacket(byte type)
            : base(type) {

            MyPacketAsBytes = MakePacket();
        }

        public byte[] MakePacket() {

            byte[] packet = new Byte[Constants.PACKET_SIZE];
            packet = Utils.InitializeArray(packet);

            //byte 0
            packet[Constants.FIELD_TYPE] = MyType;

            if (MyType == Constants.TYPE_DIRECTORY_DELIVERY) {

                //bytes 1-4
                byte[] totalPacketsLengthArray = BitConverter.GetBytes(MyTotalPackets);
                for (int i = 0; i < totalPacketsLengthArray.Length; i++) {
                    packet[Constants.FIELD_TOTAL_PACKETS + i] = totalPacketsLengthArray[i];
                }

                //bytes 5-9
                byte[] directoryLengthArray = BitConverter.GetBytes(directoryLength);
                for (int i = 0; i < directoryLengthArray.Length; i++) {
                    packet[Constants.FIELD_DIRECTORY_LENGTH + i] = directoryLengthArray[i];
                }
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