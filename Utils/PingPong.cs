using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Diagnostics;

namespace HostileNetworkUtils {
    class PingPong {

        //send some packet over and over until you get a valid ACK
        //handles timeouts, handles a lack of replies. 
        //looks ugly as sin
        //also fuck you, give me access to Packet's ID param. :P
        public static void sendUntilAck(Packet sendingPacket, UdpClient target){
            IPEndPoint remoteTarget = null;
            bool sent = false;
            Stopwatch timeout = new Stopwatch();
            while (!sent){
                Utils.SendTo(target, sendingPacket.MyPacketAsBytes);
                timeout.Restart();
                while (timeout.ElapsedMilliseconds < Constants.ACK_TIMEOUT_MILLISECONDS)
                {
                    if (target.Available != 0)
                    {
                        byte[] receivedBytes = target.Receive(ref remoteTarget);
                        if (Utils.VerifyChecksum(sendingPacket.MyPacketAsBytes))
                        {
                            if (receivedBytes[Constants.FIELD_TYPE] == Constants.TYPE_ACK)
                            {
                                bool idsEqual = true;
                                for (int i = 0; i < 4; i++)
                                {
                                    if (receivedBytes[Constants.FIELD_PACKET_ID + i] != sendingPacket.MyPacketAsBytes[Constants.FIELD_PACKET_ID + i])
                                    { // this is steves fault, I know it
                                        idsEqual = false;
                                    }
                                }
                                if (idsEqual) { sent = true; }
                            }
                        }
                    }
                }
            }
        }
        public static void sendFileTo(string filename, UdpClient target) {
            //inspect file, build metadatapacket
            //until ack received:
                //send metadata
                //wait for ack
            //start reading file, one block of bytes at a time
            //until all packets sent:
                //build datapacket
                //until ack'd:
                    //send data
                    //receive ack
            //done
        }
        public static void receiveFileFrom(byte[] metadata, UdpClient sender) {
            //open metadata, get filesize, get packettotal, get filename
            //until all packets received:
                //get packet (receive)
                //confirm checksum
                //append payload to file
                //ack
                  
        
        }
        public static void SendDirectoryTo(UdpClient target) {
            //getLocalDirectory
            //build metadata packet
            //until ack recieved:
                //send meta 
                //receive ack
            //send packets all ping pong style.  until all sent:
                //build packet
                //until ack'd
                    //send next packet
                    //receive ack
            //done
        }
        public static void ReceiveDirectoryFrom(byte[] metadata, UdpClient sender) { 
            //look at metadata, get ready to receive.
            //get number of packets to be sent.
            //until all packets received:
                //get packet (receive)
                //confirm checksum
                //print to screen
                //ack
        }
    }
}
