﻿using System;

namespace HostileNetworkUtils {
    public class AckPacket : Packet {

        public AckPacket(byte type, int id) : base(type, id) {

            MyPacketAsBytes = MakePacket();
        }

        public byte[] MakePacket() {

            byte[] packet = new Byte[Constants.PACKET_SIZE];
            packet = Utils.InitializeArray(packet);

            packet[Constants.FIELD_TYPE] = MyType;

            byte[] IDbytes = BitConverter.GetBytes(MyID);
            for (int i = 0; i < IDbytes.Length; i++) {
                packet[i + Constants.FIELD_ACK_ID] = IDbytes[i];
            }

            return packet;
        }
    }
}