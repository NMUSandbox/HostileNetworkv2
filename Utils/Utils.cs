using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace HostileNetworkUtils {
    public class Utils {
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

        double dropRate = 0.5;
        double corruptionRate = 0.5;
        bool debug = true;
        bool doubleDebug = true;

        //takes in a UDP socketpredefined to send to the server
        //takes a byte array of the packet to be sent
        int sendTo(UdpClient client, byte[] packet){
            Random randomnessGenerator = new Random();  

	        if(randomnessGenerator.NextDouble() < dropRate && !doubleDebug){
                if(debug){
			        Console.WriteLine("Packet dropped.");
                }
                return -1;
            }
            if(randomnessGenerator.NextDouble() < corruptionRate && !doubleDebug){
                if(debug){
                    Console.WriteLine("Packet corrupted.");
                }
                return -2;
            }
            client.Send(packet, packet.GetLength(0));
            return 1;
        }
    }
}

        public static bool fileExists(string fileName) {

            return File.Exists(fileName);
        }
    }
}
