using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

class AbxClient
{
    const string Host = "127.0.0.1";
    const int Port = 3000;
    const int PacketSize = 17;

    class OrderPacket
    {
        public string Symbol { get; set; }
        public char BuySell { get; set; }
        public int Quantity { get; set; }
        public int Price { get; set; }
        public int Sequence { get; set; }
        public override string ToString()
        {
            return $"Seq:{Sequence} {Symbol} {BuySell} Qty:{Quantity} Price:{Price}";
        }
    }

    static void Main()
    {
        while (true)
        {
            Console.WriteLine("Select request type:");
            Console.WriteLine("0. Exit");
            Console.WriteLine("1. Stream all packets");
            Console.WriteLine("2. Resend packet by sequence number");
            Console.Write("Enter 1 or 2: ");
            var callType = Console.ReadLine();

            if (callType == "1") // Stream all packets
            {
                var packets = GetAllPackets();
                foreach (var pkt in packets.Values.OrderBy(p => p.Sequence))
                {
                    Console.WriteLine(pkt);
                }
            }
            else if (callType == "2") // Stream a specific packet
            {
                Console.Write("Enter the sequence number to request: ");
                if (int.TryParse(Console.ReadLine(), out int resendSeq) && resendSeq > 0 && resendSeq < 256)
                {
                    var pkt = RequestPacketBySequence(resendSeq);
                    if (pkt != null)
                        Console.WriteLine(pkt);
                    else
                        Console.WriteLine("No packet received for that sequence.");
                }
                else
                {
                    Console.WriteLine("Invalid sequence number.");
                }
            }
            else if (callType == "0")
            {
                Console.WriteLine("Exiting...");
                break;
            }
            else
            {
                Console.WriteLine("Invalid option.\n Choose 1 to stream all packets or 2 to resend a packet or 0 to Exit.");
            }
        }
    }

    static Dictionary<int, OrderPacket> GetAllPackets()
    {
        var packets = new Dictionary<int, OrderPacket>();
        HashSet<int> receivedSeqs = new HashSet<int>();
        int maxSeq = int.MinValue;

        try
        {
            using TcpClient client = new TcpClient(Host, Port);
            using NetworkStream stream = client.GetStream();
            stream.Write(new byte[] { 1, 0 }, 0, 2);
            byte[] buffer = new byte[PacketSize];
            int bytesRead;
            while ((bytesRead = ReadFull(stream, buffer, PacketSize)) == PacketSize)
            {
                var pkt = ParsePacket(buffer);
                packets[pkt.Sequence] = pkt;
                receivedSeqs.Add(pkt.Sequence);
                if (pkt.Sequence > maxSeq) maxSeq = pkt.Sequence;
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Network error: {ex.Message}");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"IO error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }

        // Find and request missing packets
        if (maxSeq > 0)
        {
            var allSeqs = Enumerable.Range(1, maxSeq);
            var missingSeqs = allSeqs.Where(seq => !receivedSeqs.Contains(seq)).ToList();
            foreach (var seq in missingSeqs)
            {
                var pkt = RequestPacketBySequence(seq);
                if (pkt != null)
                    packets[pkt.Sequence] = pkt;
            }
        }

        return packets;
    }

    static OrderPacket RequestPacketBySequence(int seq)
    {
        try
        {
            using TcpClient client = new TcpClient(Host, Port);
            using NetworkStream stream = client.GetStream();
            stream.Write(new byte[] { 2, (byte)seq }, 0, 2);
            byte[] buffer = new byte[PacketSize];
            int bytesRead = ReadFull(stream, buffer, PacketSize);
            if (bytesRead == PacketSize)
            {
                return ParsePacket(buffer);
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Network error {seq}: {ex.Message}");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"IO error {seq}: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error requesting packet {seq}: {ex.Message}");
        }
        return null;
    }

    static int ReadFull(Stream stream, byte[] buffer, int size)
    {
        int total = 0;
        while (total < size)
        {
            int read = stream.Read(buffer, total, size - total);
            if (read == 0)
                break;
            total += read;
        }
        return total;
    }

    static OrderPacket ValidateAndReturnData(string symbol, char buySell, int quantity, int price, int seq)
    {
        if (string.IsNullOrWhiteSpace(symbol) || symbol.Length != 4 || !symbol.All(char.IsLetter))
        {
            Console.WriteLine($"Invalid symbol: {symbol}");
            return null;
        }
        if (buySell != 'B' && buySell != 'S')
        {
            Console.WriteLine($"Invalid buy/sell indicator: {buySell}");
            return null;
        }
        if (quantity <= 0 || price <= 0 || seq <= 0 || seq > 255)
        {
            Console.WriteLine($"Invalid numeric values: Qty={quantity}, Price={price}, Seq={seq}");
            return null;
        }

        return new OrderPacket
        {
            Symbol = symbol,
            BuySell = buySell,
            Quantity = quantity,
            Price = price,
            Sequence = seq
        };
    }

    static OrderPacket ParsePacket(byte[] buffer)
    {
        if (buffer.Length != PacketSize)
        {
            Console.WriteLine("Invalid packet size received.");
            return null;
        }

        string symbol = Encoding.ASCII.GetString(buffer, 0, 4);
        char buySell = (char)buffer[4];
        int quantity = ReadInt32BE(buffer, 5);
        int price = ReadInt32BE(buffer, 9);
        int seq = ReadInt32BE(buffer, 13);
        return ValidateAndReturnData(symbol, buySell, quantity, price, seq);
    }

    static int ReadInt32BE(byte[] buffer, int offset)
    {
        return (buffer[offset] << 24) | (buffer[offset + 1] << 16) |
               (buffer[offset + 2] << 8) | buffer[offset + 3];
    }
}