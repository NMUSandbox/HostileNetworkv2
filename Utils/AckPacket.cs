namespace HostileNetworkUtils {
    class AckPacket {

        private int myID;
        public int getMyID {
            get { return myID; }
            set { myID = value; }
        }

        private byte myType;
        public byte getMyType {
            get { return myType; }
            set { myType = value; }
        }

        public AckPacket(int ID, byte type) {

            myID = ID;
            myType = type;
        }
    }
}