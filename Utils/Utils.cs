using System;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace HostileNetworkUtils {
    public class Utils {

        public static int sendTo(UdpClient client, byte[] packet){

            double dropRate = 0.5; // ratio of packets that won't get sent
            double corruptionRate = 0.5; // ratio of packets that will be corrupted

            bool debugPrints = false; // when true, prints a message indicating corrupted or dropped packets
            bool packetBuster = false; // when true, will drop or corrupt packets. When false, will not molest packets. I write the best comments. 

            Random randomnessGenerator = new Random();  

	        if (randomnessGenerator.NextDouble() < dropRate && packetBuster) {
                if (debugPrints)
			        Console.WriteLine("Packet dropped.");

                return -1;
            }

            if (randomnessGenerator.NextDouble() < corruptionRate && packetBuster) {
                if (debugPrints)
                    Console.WriteLine("Packet corrupted.");

                for (int i = 0; i < 5; i++) {
                    packet[randomnessGenerator.Next(packet.GetLength(0))] = (byte)randomnessGenerator.Next(255);
                }

                client.Send(packet, packet.GetLength(0));
                return -2;
            }

            client.Send(packet, packet.GetLength(0));

            return 1;
        }//end sendTo

        public void sendFileTo(UdpClient udpTarget, string filename)
        {

            if (!File.Exists(filename))
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
            return MD5.Create().ComputeHash(input);
        }
    }
}