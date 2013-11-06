namespace HostileNetworkUtils {
    public class Constants {

        public const int CLIENT_PORT = 58008;
        public const int SERVER_PORT = 59008;
        public const string SEND_ADDRESS_STRING = "127.0.0.1";
        public const int PACKET_SIZE = 512;
        public const int MAX_FILENAME_SIZE = 485;
        public const int WINDOW_SIZE = 5;

        public const int OP_TIMEOUT_SECONDS = 5;
        public const int ACK_TIMEOUT_MILLISECONDS = 1000;


        //These are the first byte locations for each field in the metadata header
        public const int FIELD_TYPE = 0;
        public const int FIELD_TOTAL_PACKETS = 1;
        public const int FIELD_FILE_LENGTH = 5;
        public const int FIELD_FILENAME_LENGTH = 9;
        public const int FIELD_FILENAME = 11;
        public const int FIELD_CHECKSUM = 496;

        //These are the first byte locations for the Data packet header fields.
        //The Checksum field is the same for each packet type (496)
        //just use the same FIELD_CHECKSUM constant. 
        public const int FIELD_PACKET_ID = 0;
        public const int FIELD_PAYLOAD = 4;

        //the values for the "type" field in the metadata packet. 
        public const byte TYPE_FILE_REQUEST = 0x00;  //send to the system you want a specific file from
        public const byte TYPE_FILE_DELIVERY = 0x01; // send to a system before you send them the data of a file. Used by client to indicate a push, used by server to prepare client for a pull after the client sent a get request.
        public const byte TYPE_DIRECTORY_REQUEST = 0x02; // send to a system you want a directory list from 
        public const byte TYPE_DIRECTORY_DELIVERY = 0x03; // send to a system before you give them a directory listing
        public const byte TYPE_ACK = 0x04; //generic ack
    }
}