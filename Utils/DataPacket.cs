using System;

namespace HostileNetworkUtils {
    public class DataPacket : Packet {

        private byte[] payload;

        public DataPacket(byte[] payload, int id)
            : base(id) {

            this.payload = payload;
            MyPacketAsBytes = MakePacket();
            MyType = Constants.TYPE_DATA;
        }

        public byte[] MakePacket() {

            byte[] packet = new Byte[Constants.PACKET_SIZE];
            packet = Utils.InitializeArray(packet);

            //byte 0
            packet[Constants.FIELD_TYPE] = MyType;

            //bytes 0-3
            byte[] IDbytes = BitConverter.GetBytes(MyID);
            for (int i = 0; i < IDbytes.Length; i++) {
                packet[i + Constants.FIELD_PACKET_ID] = IDbytes[i];
            }

            //bytes 4-x
            for (int i = 0; i < payload.Length; i++) {
                packet[i + Constants.FIELD_PAYLOAD] = payload[i];
            }

            //the last bytes
            MyChecksum = Utils.GetChecksum(packet);
            for (int i = 0; i < MyChecksum.Length; i++) {
                packet[Constants.FIELD_CHECKSUM + i] = MyChecksum[i];
            }

            return packet;
        }
    }
}
