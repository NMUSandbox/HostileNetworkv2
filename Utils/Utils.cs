using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;

namespace HostileNetworkUtils {
    public class Utils {

        public static bool fileExists(string fileName) {

            return File.Exists(fileName);
        }//end fileExists

        //takes in a UDP socketpredefined to send to the server
        //takes a byte array of the packet to be sent
        public static int sendTo(UdpClient client, byte[] packet){

            double dropRate = 0.5; // ratio of packets that won't get sent
            double corruptionRate = 0.5; // ratio of packets that will be corrupted

            bool debugPrints = false; // when true, prints a message indicating corrupted or dropped packets
            bool packetBuster = false; // when true, will drop or corrupt packets. When false, will not molest packets. I write the best comments. 

            Random randomnessGenerator = new Random();  

	        if(randomnessGenerator.NextDouble() < dropRate && packetBuster){
                if(debugPrints){
			        Console.WriteLine("Packet dropped.");
                }
                return -1;
            }
            if(randomnessGenerator.NextDouble() < corruptionRate && packetBuster){
                if(debugPrints){
                    Console.WriteLine("Packet corrupted.");
                }
                for (int i = 0; i < 5; i++){
                    packet[randomnessGenerator.Next(packet.GetLength(0))] = (byte)randomnessGenerator.Next(255);
                }
                client.Send(packet, packet.GetLength(0));
               return -2;
            }
            client.Send(packet, packet.GetLength(0));
            return 1;
        }//end sendTo
        public void sendFileTo(UdpClient udpTarget, string filename){

            if (!fileExists(filename))
            {
                Console.WriteLine("FILE NOT FOUND!");
                return;
            }
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~TO DO:~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //send metadata packet for the file

            //Send the file. This is a complex series of tasks and I'm still figuring it all out. 
/*
 * Here's the vague theory. We have an array tracking all of our data packets. We send a group of these, called the <window>. 
 * This is how many we are sending at a time. When we send a packet, we set a timer for that packet. There's two ways to handle this. 
 *      a) Each packet has a timer in seconds, all timers tickdown at once, we look for the zeros in the array to determine timeouts
 *      b) Each packet gets an absolute timeout set when created. (ie: 1:01:53 PM on 10/30/13). Each loop we check to see who's been timed out. 
 * When a packet timeouts, we resend the packet, and reset it's timer. If an ack comes in for a given packet, we clear that one out of 
 * the array, setting it to null. We'll move on to the next part of the file after that. 
 * 
 * I've seen this done as: the whole file is loaded into a big ass array, and the window is just an int representing the window being sent
 * when the 'left' side of the window is cleared, shift the window down. 
 * 
 * This seems like it might get messy with large files. I'm thinking that the window should be the only array. When an element is cleared to 
 * null, we'll read the next part of the file in, turn it into a packet, and send it off in that new position. in this case, our window is 
 * an array of DataPacket objects. Each of these tracks it's timeout, and can generate it's 512 byte array to send to the reciever. Still 
 * doing some thinking as to how we'll track the progress of this method. 
 * 
 * You might also realize, this means we need to figure out how to recieve acks asynchronously. I guess we need a thread to handle acks 
 * that can null out packets when they've been recieved. The main process thread, and the ack thread both need to see the window array. 
 * 
 * 
 * 
 */

        //    int windowPosition = 0;
          //  DataPacket[] window = new DataPacket[Constants.WINDOW_SIZE];
        //    while((window.Count(s => s == null)) > 0) //will run if there are any null positions in the array. 
            


        }//end sendFileTo

        public static byte[] getChecksum(byte[] input)
        {
            return null;
        }
        public static byte[] getfileMetadataPacket(string filename, byte type) {
            if (filename.Length > 485)
            {
                Console.WriteLine("ERROR: Filename length too big!");
                return null;
            }
            byte[] packetOut = new byte[512];
            for (int i = 0; i < 512; i++) { packetOut[i] = 0; }
            packetOut[0] = type;
            byte[] filenameLength = BitConverter.GetBytes( filename.Length ) ;
            packetOut[Constants.FIELD_FILENAME_LENGTH] = filenameLength[0];
            packetOut[Constants.FIELD_FILENAME_LENGTH+1] = filenameLength[1];
            byte[] filenameArray = System.Text.Encoding.Unicode.GetBytes(filename); // This line is apparently the subject of a massive StackOverflow.com flame war... this *should* work, because all strings are held internally by .NET as unicode. 
            for (int i = 0; i < filenameArray.Length; i++)
            {
                packetOut[i + Constants.FIELD_FILENAME] = filenameArray[i];
            }
            if (type == Constants.TYPE_GET_REQUEST)
            {
                return packetOut;
            }
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~TO DO:~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //fill the rest of the metadata packet. 


            //open the file, get it's length in bytes
            //divide the file length by 485. If there's a remainder, add one but disregard decimals.
            //That resulting numebr is the total number of packets. Turn it into a byte array
            //Store that array in the packet at location Constants.FIELD_TOTAL

            //convert the file's length to a byte array
            //store that byte array in the packet at location Constants.FIELD_FILE_LENGTH



            return packetOut;
        }
    }
    class DataPacket
    {
        private int myID;
        private byte[] myPayload;
        private int timeout;
        public void DataPacket(int ID, byte[] payload)
        {
            myID = ID;
            myPayload = payload;
        }
        int getID() { return myID; }
        int getTimeout() { return timeout; }
        byte[] getPacket()
        {
            byte[] packetOut = new byte[512];
            for (int j = 0; j < 512; j++) { packetOut[j] = 0; }

            byte[] IDbytes = BitConverter.GetBytes(myID);
            for (int i = 0; i < IDbytes.Length; i++)
            {
                packetOut[i + Constants.FIELD_PACKET_ID] = IDbytes[i];
            }
            for (int i = 0; i < myPayload.Length; i++)
            {
                packetOut[i + Constants.FIELD_PAYLOAD] = myPayload[i];
            }
            byte[] checksum = Utils.getChecksum(packetOut);
            for (int j = 0; j > checksum.Length; j--)
            {
                packetOut[j+Constants.FIELD_CHECKSUM] = checksum[j];
            }
            return packetOut;
        }
    }
}
