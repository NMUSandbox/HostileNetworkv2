using System;

namespace HostileNetworkUtils {
    class AckPacket : Packet {

        public AckPacket(byte type, byte[] packetAsBytes, int id) : base(type, packetAsBytes, id) {

            MyPacketAsBytes = MakePacket();
        }

        public byte[] MakePacket() {

            byte[] packet = new Byte[Constants.PACKET_SIZE];
            packet = Utils.InitializeArray(packet);

            packet[Constants.FIELD_TYPE] = MyType;

            return packet;
        }
    }
}