using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace HostileNetworkUtils {
    public class Utils {

        public static byte[] InitializeArray(byte[] arr) {

            for (int i = 0; i < arr.Length; i++) {
                arr[i] = 0;
            }
            return arr;
        }

        public static byte[] GetChecksum(byte[] input) {

            return MD5.Create().ComputeHash(input);
        }

        public static int SendTo(UdpClient target, byte[] packet){

            Random randomnessGenerator = new Random();  

	        if (randomnessGenerator.NextDouble() < Constants.SIMULATION_DROP_RATE && Constants.DEBUG_DROP_AND_CORRUPT) {
                if (Constants.DEBUG_PRINTING){
                    Console.WriteLine("Packet dropped.");
                }
                return -1;//return now, don't send packet
            }

            if (randomnessGenerator.NextDouble() < Constants.SIMULATION_CORRPUTION_RATE && Constants.DEBUG_DROP_AND_CORRUPT) {
                if (Constants.DEBUG_PRINTING){
                    Console.WriteLine("Packet corrupted.");
                }
                for (int i = 0; i < 5; i++) {//molest packet
                    packet[randomnessGenerator.Next(packet.GetLength(0))] = (byte)randomnessGenerator.Next(255);
                }
                target.Send(packet, packet.GetLength(0));
                return -2;
            }

            target.Send(packet, packet.GetLength(0));

            return 1;
        }//end sendTo

        public static void ReceiveFile(UdpClient udpSource, byte[] metadata) {
            int currentWorkingPacket = 0;
  /*    x      //get filename out of metadata packet (use filename size)
        x    //get filesize out
        x    //get packet num out

        --    //open a new file with the filename
        x    //loop{
        x        //recieve a packet. 
        x        //copy last 32 bytes into a variable
        x        //0 out the checksum in the packet. 
        x        //run md5 on the packet. 
        x        //compare checksums. if !=: do nothing.
        x        //if == and ID == currentworkingpacket, write that mother to the disk
        x        //if == and ID < currentWorkingPacket, ack that packet.
        x        //if == and ID > currentWorkingPacket{
        o            //if ID > recieverwindow (constant?), do nothing
        o            //if ID < recieverWindow, store this packet for later
        x        //sort the stored packets. 
        x        //loop through them. 
        x        //as long as the lowest ID in the list == currentWorkingID, write it to disk and remove from list. 
            //  loop}*/
            int filenameSize = BitConverter.ToInt32(metadata, Constants.FIELD_FILENAME_LENGTH);
            int fileLength = BitConverter.ToInt32(metadata, Constants.FIELD_FILE_LENGTH);
            int packetTotal = BitConverter.ToInt32(metadata, Constants.FIELD_TOTAL_PACKETS);
            byte[] filenameBytes = new byte[filenameSize];
            for (int i = 0; i < filenameSize; i++)
            {
                filenameBytes[i] = metadata[i + Constants.FIELD_FILENAME];
            }
            string filename = Encoding.Unicode.GetString(filenameBytes);
            
            
            List<dataPacketBuffer> buffer = new List<dataPacketBuffer>();

            IPEndPoint remoteIPEndPoint = null;
            while (currentWorkingPacket < packetTotal){
                byte[] receivedBytes = udpSource.Receive(ref remoteIPEndPoint); //start with a new packet
                byte[] receivedChecksumHash = new byte[32];
                for (int i = 0; i < 32; i++) { 
                    receivedChecksumHash[i] = receivedBytes[i + Constants.FIELD_CHECKSUM];
                    receivedBytes[i + Constants.FIELD_CHECKSUM] = 0;
                }

                if (CompareHash(receivedBytes, receivedChecksumHash)) { //valid checksum
                    byte[] payloadUnpacker = new byte[Constants.PAYLOAD_SIZE];
                    for (int i = 0; i < Constants.PAYLOAD_SIZE; i++) { 
                        payloadUnpacker[i] = receivedBytes[i + Constants.FIELD_PAYLOAD];
                    }
                    dataPacketBuffer receivedPacket = new dataPacketBuffer(BitConverter.ToInt32(receivedBytes, Constants.FIELD_PACKET_ID), payloadUnpacker);

                    if (receivedPacket.getID() < currentWorkingPacket) {
                        AckPacket respond = new AckPacket(Constants.TYPE_ACK, null, receivedPacket.getID());
                        SendTo(udpSource, respond.MakePacket());
                        //return an ack
                    }
                    else if (receivedPacket.getID() == currentWorkingPacket) {
                        File.AppendAllText(filename, Encoding.Unicode.GetString(payloadUnpacker));
                        //write to file
                    }
                    else if(receivedPacket.getID() > currentWorkingPacket){
                        buffer.Add(receivedPacket); // there is currently no upper limit to the number of packets stored in the buffer
                    }
                }

                buffer.Sort(
                    delegate(dataPacketBuffer s1, dataPacketBuffer s2){
                        return s1.getID().CompareTo(s2.getID());
                    }
                );
                foreach (dataPacketBuffer item in buffer) {
                    if (item.getID() == currentWorkingPacket)
                    {
                        File.AppendAllText(filename, Encoding.Unicode.GetString(item.getPayload()));
                        buffer.Remove(item);
                        currentWorkingPacket++;
                    }
                }
            }
        }
        struct dataPacketBuffer{
                private int ID;
                private byte[] payload;
                public dataPacketBuffer(int inID, byte[] inPayload) { 
                    ID = inID; 
                    payload = inPayload; 
                }
                public void setPayload(byte[] inArr){ payload = inArr; }
                public byte[] getPayload() { return payload; }
                public void setID(int newID) { ID = newID; }
                public int getID() { return ID; }
        };
        public static bool CompareHash(byte[] dataToBeHashed, byte[] comparisonHash){
            byte[] computedHash = GetChecksum(dataToBeHashed);
            for (int i = 0; i < comparisonHash.Length; i++){
                if (computedHash[i] != comparisonHash[i]) return false;
            }
            return true;
        }
        public static void SendFileTo(UdpClient udpTarget, string filename)
        {

            if (!File.Exists(filename)) {
                Console.WriteLine("FILE NOT FOUND!");
                return;
            }
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~TO DO:~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //send metadata packet for the file
            //Send the file. This is a complex series of tasks and I'm still figuring it all out. 
/*
 * Here's the vague theory. We have an array tracking all of our data packets. We send a group of these, called the <window>. 
 * This is how many we are sending at a time. When we send a packet, we set a timer for that packet. 
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
 */

        //    int windowPosition = 0;
        //    DataPacket[] window = new DataPacket[Constants.WINDOW_SIZE];
        //    while((window.Count(s => s == null)) > 0) //will run if there are any null positions in the array. 
            
        }//end sendFileTo
    }
}