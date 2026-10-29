using System;
using System.IO;
using System.Text;
using LanControl.Core.Models;

namespace LanControl.Core.Network
{
    public static class PacketFramer
    {
        public const byte MAGIC1 = 0x4E; // 'N'
        public const byte MAGIC2 = 0x53; // 'S'
        public const int HEADER_SIZE = 11; // 2 magic + 1 type + 4 jsonLen + 4 rawLen

        public static byte[] Encode(Packet packet)
        {
            byte[] jsonBytes = string.IsNullOrEmpty(packet.PayloadJson) 
                ? new byte[0] 
                : Encoding.UTF8.GetBytes(packet.PayloadJson);

            byte[] rawBytes = packet.RawBytes ?? new byte[0];

            int totalSize = HEADER_SIZE + jsonBytes.Length + rawBytes.Length;
            byte[] buffer = new byte[totalSize];

            buffer[0] = MAGIC1;
            buffer[1] = MAGIC2;
            buffer[2] = (byte)packet.Type;

            byte[] jsonLenBytes = BitConverter.GetBytes(jsonBytes.Length);
            Buffer.BlockCopy(jsonLenBytes, 0, buffer, 3, 4);

            byte[] rawLenBytes = BitConverter.GetBytes(rawBytes.Length);
            Buffer.BlockCopy(rawLenBytes, 0, buffer, 7, 4);

            if (jsonBytes.Length > 0)
            {
                Buffer.BlockCopy(jsonBytes, 0, buffer, HEADER_SIZE, jsonBytes.Length);
            }

            if (rawBytes.Length > 0)
            {
                Buffer.BlockCopy(rawBytes, 0, buffer, HEADER_SIZE + jsonBytes.Length, rawBytes.Length);
            }

            return buffer;
        }

        public static Packet Decode(byte[] headerBuffer, Stream stream)
        {
            if (headerBuffer[0] != MAGIC1 || headerBuffer[1] != MAGIC2)
            {
                throw new InvalidDataException("Invalid magic packet header");
            }

            PacketType type = (PacketType)headerBuffer[2];
            int jsonLen = BitConverter.ToInt32(headerBuffer, 3);
            int rawLen = BitConverter.ToInt32(headerBuffer, 7);

            if (jsonLen < 0 || jsonLen > 50 * 1024 * 1024)
                throw new InvalidDataException("Invalid json payload length: " + jsonLen);

            if (rawLen < 0 || rawLen > 50 * 1024 * 1024)
                throw new InvalidDataException("Invalid raw data length: " + rawLen);

            string json = string.Empty;
            if (jsonLen > 0)
            {
                byte[] jsonBuffer = ReadExactBytes(stream, jsonLen);
                json = Encoding.UTF8.GetString(jsonBuffer);
            }

            byte[] rawBytes = null;
            if (rawLen > 0)
            {
                rawBytes = ReadExactBytes(stream, rawLen);
            }

            return new Packet(type, json, rawBytes);
        }

        public static byte[] ReadExactBytes(Stream stream, int count)
        {
            byte[] buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read <= 0)
                    throw new EndOfStreamException("Socket connection closed prematurely while reading packet body");
                offset += read;
            }
            return buffer;
        }
    }
}
