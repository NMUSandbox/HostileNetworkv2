namespace HostileNetworkUtils {
    public class Constants {

        public const int PORT = 58008;
        public const string SEND_ADDRESS_STRING = "127.0.0.1";
        public const int PACKET_SIZE = 512;

        //These are the first byte locations for each field in the metadata header
        public const int FIELD_TYPE = 0;
        public const int FIELD_TOTAL = 1;
        public const int FIELD_FILE_LENGTH = 5;
        public const int FIELD_FILENAME_LENGTH = 9;
        public const int FIELD_FILENAME = 11;
        public const int FIELD_CHECKSUM = 496;

        //These are the first byte locations for the Data packet header fields.
        //The Checksum field is the same for each packet type (496)
        //just use the same FIELD_CHECKSUM constant. 
        public const int FIELD_PACKET_ID = 0;
        public const int FIELD_PAYLOAD = 4;
    }
}