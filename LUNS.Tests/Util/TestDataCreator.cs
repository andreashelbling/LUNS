using System;
using System.Collections.Generic;
using System.Linq;

namespace LUNS.Tests.Util
{
    internal class TestDataCreator
    {
        public static byte[] CreateTestData(int length)
        {
            byte[] buf = new byte[length];
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] = (byte)(i % 256);
            }
            return buf;
        }

        public static List<byte[]> CreateTestData(uint length, uint segmentSize)
        {
            if (segmentSize % 2 != 0)
                throw new NotSupportedException("Segments must be multiple of 2 bytes");

            var data = new List<byte[]>();
            uint numSegments = length / segmentSize;
            uint rest = length % segmentSize;
            if (numSegments > 65535)
                throw new NotSupportedException("Only up to 65535 packets can be created as test data");

            for (uint seg = 0; seg < numSegments; seg++)
            {
                data.Add(CreateBuffer((ushort)seg, segmentSize));
            }

            if (rest > 0)
            {
                data.Add(CreateBuffer((ushort)numSegments, rest));
            }

            return data;
        }

        private static byte[] CreateBuffer(ushort value, uint length)
        {
            var buf = new byte[length];
            byte[] valueBuf = BitConverter.GetBytes(value);
            for (int numWords = 0; numWords < length / sizeof(ushort); numWords++)
            {
                Array.Copy(valueBuf, 0, buf, numWords * sizeof(ushort), valueBuf.Length);
            }
            return buf;
        }

        public static ushort GetPacketNumber(byte[] data)
        {
            // count the occurence of all 2-byte-values
            int[] histogram = new int[65536];
            for (int pos = 0; pos < data.Length; pos += 2)
            {
                ushort value = BitConverter.ToUInt16(data, pos);
                histogram[value]++;
            }

            // find packet number
            int occurrence = histogram.Max();
            ushort packetNumber = 0;
            for (int i = 0; i < 65536; i++)
            {
                if (histogram[i] == occurrence)
                {
                    packetNumber = (ushort)i;
                    break;
                }
            }

            return packetNumber;
        }

        /// <summary>
        /// Works only if data was created using CreateTestData(uint, uint) method.
        /// </summary>
        /// <param name="receivedData"></param>
        /// <returns></returns>
        public static int ComputeBitErrors(List<byte[]> receivedData)
        {
            // Assuming the bit error rate was less than 50%, most bytes should still be intact
            // Each packet contains the repeated packet number -> find most frequent byte
            int bitErrors = 0;
            foreach (byte[] data in receivedData)
            {
                ushort packetNumber = GetPacketNumber(data);

                // detect wrong bits
                for (int pos = 0; pos < data.Length; pos += 2)
                {
                    ushort value = BitConverter.ToUInt16(data, pos);
                    if (value != packetNumber)
                    {
                        // count number of inverted bits
                        ushort diffBits = (ushort)(value ^ packetNumber);
                        bitErrors += System.Numerics.BitOperations.PopCount(diffBits);
                    }
                }
            }

            return bitErrors;
        }
    }
}
