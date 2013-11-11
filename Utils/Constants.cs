namespace HostileNetworkUtils {
    public class Constants {

        public const int CLIENT_PORT = 58008;
        public const int SERVER_PORT = 59008;
        public const string SEND_ADDRESS_STRING = "127.0.0.1";
        public const int PACKET_SIZE = 512;
        public const int MAX_FILENAME_SIZE = 467;//512-4-4-4-1-32 = 512 - (13 header bytes) - (32 checksum) = 467 bytes
        public const int PAYLOAD_SIZE = 476; //id = 4 bytes, checksum = 32. 512-32-4 = 476
        public const int WINDOW_SIZE = 5;

        public const int OP_TIMEOUT_SECONDS = 5;
        public const int ACK_TIMEOUT_MILLISECONDS = 1000;


        //These are the first byte locations for each field in the file metadata header
        public const int FIELD_TYPE = 0; //one byte field
        public const int FIELD_TOTAL_PACKETS = 1;//4 bytes
        public const int FIELD_FILE_LENGTH = 5;//4 bytes
        public const int FIELD_FILENAME_LENGTH = 9;//4bytes
        public const int FIELD_FILENAME = 13;//512-4-4-4-1-32 = 512-45 = 467 bytes, extra as 0 padding
        public const int FIELD_CHECKSUM = 480; //MD5.Create().ComputeHash(input) returns 32 bytes, 512-32=480

        public const int FIELD_DIRECTORY_LENGTH = 5;

        //These are the first byte locations for the file data packet header fields.
        //The Checksum field is the same for each packet type (479)
        //just use the same FIELD_CHECKSUM constant. 
        public const int FIELD_PACKET_ID = 0;
        public const int FIELD_PAYLOAD = 4;

        //the values for the "type" field in the metadata packet. 
        public const byte TYPE_FILE_REQUEST = 0x00;  //send to the system you want a specific file from
        public const byte TYPE_FILE_DELIVERY = 0x01; // send to a system before you send them the data of a file. Used by client to indicate a push, used by server to prepare client for a pull after the client sent a get request.
        public const byte TYPE_DIRECTORY_REQUEST = 0x02; // send to a system you want a directory list from 
        public const byte TYPE_DIRECTORY_DELIVERY = 0x03; // send to a system before you give them a directory listing
        public const byte TYPE_ACK = 0x04; //generic ack


        public const double SIMULATION_DROP_RATE = 0.5; // ratio of packets that won't get sent
        public const double SIMULATION_CORRPUTION_RATE = 0.5; // ratio of packets that will be corrupted

        public const bool DEBUG_PRINTING = true; // when true, prints a message indicating corrupted or dropped packets
        public const bool DEBUG_DROP_AND_CORRUPT = false; // when true, will drop or corrupt packets. When false, will not molest packets. I write the best comments. 

    }
}