using System.Diagnostics;

namespace HostileNetworkUtils {
    public abstract class Packet {

        private int myID;
        private byte myType;
        private byte[] myChecksum;
        private byte[] myPacketAsBytes;
        private Stopwatch myTimer = new Stopwatch();

        public Stopwatch MyTimer {
            get { return myTimer; }
            set { myTimer = value; }
        }

        protected int MyID {
            get { return myID; }
            set { myID = value; }
        }
        
        protected byte MyType {
            get { return myType; }
            set { myType = value; }
        }
        public byte getMyType() { return myType; }
        
        protected byte[] MyChecksum {
            get { return myChecksum; }
            set { myChecksum = value; }
        }
        
        public byte[] MyPacketAsBytes {
            get { return myPacketAsBytes; }
            set { myPacketAsBytes = value; }
        }

        public Packet(byte type) {

            myType = type;
        }
        public Packet(int id)
        {
            myID = id;
        }
        public Packet(byte type, int id) {

            myType = type;
            myID = id;
        }

        public Packet(byte type, byte[] packetBytes, int id = -1) {

            myType = type;
            myPacketAsBytes = packetBytes;
            myID = id;
        }
    }
}