using System.Diagnostics;

namespace HostileNetworkUtils {

    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~TO DO:~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    //open the file, get it's length in bytes
    //divide the file length by Constants.PAYLOAD_SIZE. If there's a remainder, add one but disregard decimals.
    //That resulting numebr is the total number of packets. Turn it into a byte array
    //Store that array in the packet at location Constants.FIELD_TOTAL

    //convert the file's length to a byte array
    //store that byte array in the packet at location Constants.FIELD_FILE_LENGTH

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