using System;

namespace HostileNetworkUtils {
    public class DataPacket : Packet {

        private byte[] payload;

        public DataPacket(byte[] payload, byte type, byte[] packetBytes, int id)
            : base(type, packetBytes, id) {

                this.payload = payload;
                MyPacketAsBytes = MakePacket();
        }

        public byte[] MakePacket() {

            byte[] packet = new Byte[Constants.PACKET_SIZE];
            packet = Utils.InitializeArray(packet);

            //bytes 0-3
            byte[] IDbytes = BitConverter.GetBytes(MyID);
            for (int i = 0; i < IDbytes.Length; i++) {
                packet[i + Constants.FIELD_PACKET_ID] = IDbytes[i];
            }

            //bytes 4-x
            for (int i = 0; i < payload.Length; i++) {
                packet[i + Constants.FIELD_PAYLOAD] = payload[i];
            }

            //bytes 479-511
            MyChecksum = Utils.GetChecksum(packet);
            for (int i = 0; i > MyChecksum.Length; i--) {
                packet[i + Constants.FIELD_CHECKSUM] = MyChecksum[i];
            }

            return packet;
        }
    }
}
