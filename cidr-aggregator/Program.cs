using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

class Program
{
    static void Main()
    {
        List<string> cidrs = new List<string> { "213.199.180.192/27", "213.199.183.0/24" };

        string aggregatedCidr = AggregateCIDRs(cidrs);

        Console.WriteLine("Aggregated CIDR:");
        Console.WriteLine(aggregatedCidr);
    }

    static string AggregateCIDRs(List<string> cidrs)
    {
        if (cidrs == null || cidrs.Count == 0) throw new ArgumentException("CIDR list cannot be null or empty.");

        uint minIp = uint.MaxValue;
        uint maxIp = uint.MinValue;

        foreach (var cidr in cidrs)
        {
            IPNetwork network = IPNetwork.Parse(cidr);
            uint networkAddress = BitConverter.ToUInt32(network.Network.GetAddressBytes().Reverse().ToArray(), 0);
            uint broadcastAddress = networkAddress | ~BitConverter.ToUInt32(network.SubnetMask.GetAddressBytes().Reverse().ToArray(), 0);

            if (networkAddress < minIp) minIp = networkAddress;
            if (broadcastAddress > maxIp) maxIp = broadcastAddress;
        }
        return FindSmallestCIDR(minIp, maxIp);
    }

    static string FindSmallestCIDR(uint minIp, uint maxIp)
    {
        int cidrPrefix = 32;

        while (cidrPrefix > 0)
        {
            uint mask = uint.MaxValue << (32 - cidrPrefix);
            if ((minIp & mask) == (maxIp & mask)) break;
            cidrPrefix--;
        }

        IPAddress ip = new IPAddress(BitConverter.GetBytes(minIp & (uint.MaxValue << (32 - cidrPrefix))).Reverse().ToArray());
        return $"{ip}/{cidrPrefix}";
    }
}

public class IPNetwork
{
    public IPAddress Network { get; }
    public IPAddress SubnetMask { get; }
    public int Cidr { get; }

    private IPNetwork(IPAddress network, IPAddress subnetMask, int cidr)
    {
        Network = network;
        SubnetMask = subnetMask;
        Cidr = cidr;
    }

    public static IPNetwork Parse(string cidr)
    {
        var parts = cidr.Split('/');
        var network = IPAddress.Parse(parts[0]);
        var cidrLength = int.Parse(parts[1]);
        var subnetMask = GetSubnetMask(cidrLength);

        return new IPNetwork(network, subnetMask, cidrLength);
    }

    private static IPAddress GetSubnetMask(int cidrLength)
    {
        uint mask = cidrLength == 0 ? 0 : uint.MaxValue << (32 - cidrLength);
        return new IPAddress(BitConverter.GetBytes(mask).Reverse().ToArray());
    }
}
