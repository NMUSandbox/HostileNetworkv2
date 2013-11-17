namespace HostileNetworkUtils {
    public class Constants {

        public const bool DEBUG_PRINTING = false; // when true, prints a message indicating corrupted or dropped packets
        public const bool DEBUG_DROP_AND_CORRUPT = false; // when true, will drop or corrupt packets
        public const bool DEBUG_PING_PONG_ACTIVE = true; // when true, use ping pong to do everything

        public const int CLIENT_PORT = 58008;
        public const int SERVER_PORT = 59008;
        public const string SEND_ADDRESS_STRING = "127.0.0.1";
        public const string IGNORE_HOSTNAME_STRING = "turing.nmu.edu";
        //public const string SEND_ADDRESS_STRING = "euclid.nmu.edu";

        public const int PACKET_SIZE = 512;
        public const int MAX_FILENAME_SIZE = 483;//512-4-4-4-1-16 = 512 - (13 header bytes) - (16 checksum) = 483 bytes
        public const int PAYLOAD_SIZE = 491; //id = 4 bytes, checksum = 16, type = 1. 512-16-4-1 = 491

        public const int OP_TIMEOUT_SECONDS = 5;
        public const int PACKET_TIMEOUT_MILLISECONDS = 100;

        public const double SIMULATION_DROP_RATE = 0.05; // ratio of packets that won't get sent
        public const double SIMULATION_CORRPUTION_RATE = 0.05; // ratio of packets that will be corrupted


        //These are the first byte locations for each field in the file metadata header
        public const int FIELD_TYPE = 0;
        public const int FIELD_TOTAL_PACKETS = 1;
        public const int FIELD_FILE_LENGTH = 5;
        public const int FIELD_FILENAME_LENGTH = 9;
        public const int FIELD_FILENAME = 13;
        public const int FIELD_CHECKSUM = 496; //MD5.Create().ComputeHash(input) returns 16 bytes, 512-16=496

        public const int FIELD_DIRECTORY_LENGTH = 5;
        public const int FIELD_ACK_ID = 1;

        //These are the first byte locations for the file data packet header fields.
        //The Checksum field is the same for each packet type (495)
        //just use the same FIELD_CHECKSUM constant. 
        public const int FIELD_PACKET_ID = 1;
        public const int FIELD_PAYLOAD = 5;

        //the values for the "type" field in the metadata packet. 
        public const byte TYPE_FILE_REQUEST = 0x00;  //send to the system you want a specific file from
        public const byte TYPE_FILE_DELIVERY = 0x01; // send to a system before you send them the data of a file. Used by client to indicate a push, used by server to prepare client for a pull after the client sent a get request.
        public const byte TYPE_DIRECTORY_REQUEST = 0x02; // send to a system you want a directory list from 
        public const byte TYPE_DIRECTORY_DELIVERY = 0x03; // send to a system before you give them a directory listing
        public const byte TYPE_ACK = 0x04; //generic ack
        public const byte TYPE_DATA = 0x05;
    }
}