using System;
using System.Diagnostics;

namespace HostileNetworkUtils {

    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~TO DO:~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    //fill the rest of the metadata packet. 


    //open the file, get it's length in bytes
    //divide the file length by 485. If there's a remainder, add one but disregard decimals.
    //That resulting numebr is the total number of packets. Turn it into a byte array
    //Store that array in the packet at location Constants.FIELD_TOTAL

    //convert the file's length to a byte array
    //store that byte array in the packet at location Constants.FIELD_FILE_LENGTH

    public class MetadataPacket {

        protected byte packetType;

        private Stopwatch timer;
        public Stopwatch getTimer {
            get { return timer; }
            set { timer = value; }
        }
        private byte[] bytes;
        public byte[] getBytes {
            get { return bytes; }
            set { bytes = value; }
        }

        protected byte[] makeEmptyPacketWithType(byte type) {

            byte[] packetOut = new byte[Constants.PACKET_SIZE];
            for (int i = 0; i < Constants.PACKET_SIZE; i++) {
                packetOut[i] = 0;
            }
            packetOut[Constants.FIELD_TYPE] = type;

            return packetOut;
        }
    }

    public class DirectoryMetadataPacket : MetadataPacket {

        byte[] totalPackets,
            directoryLength;

        public DirectoryMetadataPacket(byte type) {

            this.getTimer = new Stopwatch();
            packetType = type;
            getBytes = makeEmptyPacketWithType(packetType);
        }
    }

    public class FileMetadataPacket : MetadataPacket {

        byte[] totalPackets,
            fileLength,
            fileNameLength,
            fileName,
            checksum;

        public FileMetadataPacket(byte type, byte[] totalPackets, byte[] fileLength,
            byte[] fileNameLength, byte[] fileName, byte[] checksum) {

            this.getTimer = new Stopwatch();
            packetType = type;
            this.totalPackets = totalPackets;
            this.fileLength = fileLength;
            this.fileNameLength = fileNameLength;
            this.fileName = fileName;
            this.checksum = checksum;

            getBytes = makeFileMetadataPacket();
        }

        public byte[] makeFileMetadataPacket() {

            byte[] packetOut = new Byte[Constants.PACKET_SIZE];

            if (fileName.Length > Constants.MAX_FILENAME_SIZE) {
                Console.WriteLine("ERROR: Filename length too big!");
                return null;
            }

            byte[] fileNameLength = BitConverter.GetBytes(fileName.Length);
            for (int i = 0; i < fileNameLength.Length; i++) {
                packetOut[Constants.FIELD_FILENAME_LENGTH + i] = fileNameLength[i];
            }

            byte[] filenameArray = fileName;
            for (int i = 0; i < filenameArray.Length; i++) {
                packetOut[Constants.FIELD_FILENAME + i] = filenameArray[i];
            }

            return packetOut;
        }
    }
}
