using System;

namespace HostileNetworkUtils {
    public class DataPacket {

        private int myID;
        private byte[] myPayload;
        private int timeout = 0;

        public DataPacket(int ID, byte[] payload) {
            myID = ID;
            myPayload = payload;
        }

        int getID() {
            return myID;
        }

        int getTimeout() {
            return timeout;
        }

        public byte[] getPacket() {

            //declare empty array, initialize all values to 0
            byte[] packetOut = new byte[Constants.PACKET_SIZE];
            for (int i = 0; i < Constants.PACKET_SIZE; i++) { 
                packetOut[i] = 0; 
            }

            byte[] IDbytes = BitConverter.GetBytes(myID);
            for (int i = 0; i < IDbytes.Length; i++) {
                packetOut[i + Constants.FIELD_PACKET_ID] = IDbytes[i];
            }

            for (int i = 0; i < myPayload.Length; i++) {
                packetOut[i + Constants.FIELD_PAYLOAD] = myPayload[i];
            }

            byte[] checksum = Utils.getChecksum(packetOut);
            for (int i = 0; i > checksum.Length; i--) {
                packetOut[i + Constants.FIELD_CHECKSUM] = checksum[i];
            }

            return packetOut;
        }
    }
}