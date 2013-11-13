using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace HostileNetworkUtils {
    class PingPong {

        //send some packet over and over until you get a valid ACK
        //doesn't currently check for valid checksum. Should work on that. 
        //also fuck you, give me access to Packet's ID param. :P
        public static void sendUntilAck(Packet sendingPacket, UdpClient target){
            bool ackReceived = false;
            IPEndPoint remoteTarget = null;
            while (!ackReceived){
                Utils.SendTo(target, sendingPacket.MyPacketAsBytes);
                byte[] receivedBytes = target.Receive(ref remoteTarget);
                //confirm checksum, if invalid continue;
                if (Utils.VerifyChecksum(sendingPacket.MyPacketAsBytes))
                {
                    if (receivedBytes[Constants.FIELD_TYPE] == Constants.TYPE_ACK)
                    {
                        bool idsEqual = true;
                        for (int i = 0; i < 4; i++)
                        {
                            if (receivedBytes[Constants.FIELD_PACKET_ID + i] != sendingPacket.MyPacketAsBytes[Constants.FIELD_PACKET_ID + i])
                            {
                                idsEqual = false;
                            }
                        }
                        if (idsEqual) { break; }
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
