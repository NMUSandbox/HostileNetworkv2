using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using HostileNetworkUtils;
using System.Net.Sockets;
using System.Net;

namespace HostileNetwork {
    class HostileNetworkClient {

        static void Main() {

            string command = acceptCommand();
            string fileName = "";

            switch (command) {
                case "send":
                    fileName = getValidFileName();
                    break;
                case "get":
                    break;
                case "dir":
                    Console.WriteLine("Requesting directory listing from server...");
                    break;
                default:
                    break;
            }


            launchClient();
        }

        private static void launchClient() {
            
            string data = "";
          
            byte[] sendBytes = new Byte[1024];
            byte[] rcvPacket = new Byte[1024];
            UdpClient client = new UdpClient();
            IPAddress sendAddress = IPAddress.Parse(Constants.SEND_ADDRESS_STRING);
            client.Connect(sendAddress, Constants.PORT);
            IPEndPoint remoteIPEndPoint = new IPEndPoint(sendAddress, Constants.PORT);

            Console.WriteLine("Client is Started");
            Console.WriteLine("Type your message");

            while (true) {
                data = Console.ReadLine();
                sendBytes = Encoding.ASCII.GetBytes(DateTime.Now.ToString() + " " + data);

                Utils.sendTo(client, sendBytes);
                //client.Send(sendBytes, sendBytes.GetLength(0));
                rcvPacket = client.Receive(ref remoteIPEndPoint);

                string rcvData = Encoding.ASCII.GetString(rcvPacket);
                Console.WriteLine("Handling client at " + remoteIPEndPoint + " - ");

                Console.WriteLine("Message Received: " + rcvData);
            }
            Console.WriteLine("Close Port Command Sent");  //user feedback
            Console.ReadLine();
            client.Close();  //close connection
        }

        static string getValidFileName() {

            Console.Write("Please enter a filepath\\filename: ");
            string fileName = Console.ReadLine();

            while (fileName == "" || !Utils.fileExists(fileName)) {
                Console.WriteLine("Invalid file name or file does not exist.\n");
                Console.Write("Please enter a filepath\\filename: ");
                fileName = Console.ReadLine();
            }
            return fileName;
        }

        static string acceptCommand() {

            string command = "";

            Console.WriteLine("Usage: <get|send|dir>");
            Console.Write("Please enter a command: ");
            command = Console.ReadLine();

            while (command != "dir" && command != "get" && command != "send") {
                Console.WriteLine("Invalid command. Usage: <get|send|dir>\n");
                Console.Write("Please enter a command: ");
                command = Console.ReadLine();
            }

            return command;
        }
    }
}