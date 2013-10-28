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
        }

        //takes in a UDP socketpredefined to send to the server
        //takes a byte array of the packet to be sent
        public static int sendTo(UdpClient client, byte[] packet){

            double dropRate = 0.5;
            double corruptionRate = 0.5;
            bool debugPrints = false;
            bool packetBuster = false;

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
                return -2;
            }
            client.Send(packet, packet.GetLength(0));
            return 1;
        }
    }
}
