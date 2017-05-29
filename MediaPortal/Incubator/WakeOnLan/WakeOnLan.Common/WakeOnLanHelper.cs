﻿#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace WakeOnLan.Common
{
  public class WakeOnLanHelper
  {
    /// <summary>
    /// Determines whether the specified <paramref name="hwAddress"/> appears to be valid.
    /// </summary>
    /// <param name="hwAddress">Byte array containing the hardware (MAC) address.</param>
    /// <returns></returns>
    public static bool IsValidHardwareAddress(byte[] hwAddress)
    {
      return hwAddress != null && hwAddress.Length == 6 && hwAddress.Any(b => b != 0);
    }

    /// <summary>
    /// Asynchronously tries to wake the server with the given <paramref name="hwAddress"/>. If the server does not wake this
    /// method resends the WOL packet until either the server wakes or <paramref name="wakeTimeout"/> is reached.
    /// </summary>
    /// <param name="hostNameOrAddress">The hostname or IP address of the server, used to ping the server to verify whether it is awake.</param>
    /// <param name="hwAddress">The hardware (MAC) address of the server.</param>
    /// <param name="port">The UDP port to send the WOL packet to.</param>
    /// <param name="pingTimeout">The time in ms to wait for a ping response from the server.</param>
    /// <param name="wakeTimeout">The total time in ms to spend trying to wake the server. </param>
    /// <returns></returns>
    public static async Task<bool> WakeServer(string hostNameOrAddress, byte[] hwAddress, int port, int pingTimeout, int wakeTimeout)
    {
      if (!IsValidHardwareAddress(hwAddress))
        throw new ArgumentException(
            "Invalid hardware address.",
            "hwAddress",
            null);

      //See if the server is already awake
      if (await PingAsync(hostNameOrAddress, pingTimeout))
      {
        ServiceRegistration.Get<ILogger>().Debug("WakeOnLanHelper: Server is already awake");
        return true;
      }

      //Create the magic packet
      byte[] magicPacket = CreateMagicPacket(hwAddress);

      DateTime end = DateTime.Now.AddMilliseconds(wakeTimeout);
      do
      {
        //Send the magic packet
        await SendWOLPacketAsync(magicPacket, port);

        //See if the server is now awake
        if (await PingAsync(hostNameOrAddress, pingTimeout))
        {
          ServiceRegistration.Get<ILogger>().Debug("WakeOnLanHelper: Successfully woke server");
          return true;
        }
        //Retry until the timeout is reached
      } while (DateTime.Now < end);

      ServiceRegistration.Get<ILogger>().Warn("WakeOnLanHelper: Failed to wake server within timeout of {0}ms", wakeTimeout);
      return false;
    }

    /// <summary>
    /// Asynchronously pings the specified computer and returns whether the ping was successful.
    /// </summary>
    /// <param name="hostNameOrAddress">The host name or IP address of the computer to ping.</param>
    /// <param name="timeout">The maximum number of milliseconds to wait for a response.</param>
    /// <returns>True if a response was received from the specified computer.</returns>
    protected static async Task<bool> PingAsync(string hostNameOrAddress, int timeout)
    {
      using (Ping ping = new Ping())
        return (await ping.SendPingAsync(hostNameOrAddress, timeout)).Status == IPStatus.Success;
    }

    /// <summary>
    /// Asynchronously sends a Wake-On-LAN 'magic' packet to the computer with the specified hardware address.
    /// </summary>
    /// <param name="hwAddress">The hardware address of the computer to wake.</param>
    protected static async Task SendWOLPacketAsync(byte[] magicPacket, int port)
    {
      // WOL 'magic' packet is sent over UDP.
      using (UdpClient client = new UdpClient())
      {
        // Send to: 255.255.255.0 over UDP.
        client.Connect(IPAddress.Broadcast, port);

        // Send WOL 'magic' packet.
        await client.SendAsync(magicPacket, magicPacket.Length);
      }
    }

    /// <summary>
    /// Creates a WOL 'magic' packet from the specified <paramref name="hwAddress"/>.
    /// </summary>
    /// <param name="hwAddress">The hardware (MAC) address of the server to wake.</param>
    /// <returns></returns>
    protected static byte[] CreateMagicPacket(byte[] hwAddress)
    {
      // Two parts to a 'magic' packet:
      //     First is 0xFFFFFFFFFFFF,
      //     Second is 16 * MACAddress.
      byte[] packet = new byte[17 * 6];

      // Set to: 0xFFFFFFFFFFFF.
      for (int i = 0; i < 6; i++)
        packet[i] = 0xFF;

      // Set to: 16 * MACAddress
      for (int i = 1; i <= 16; i++)
        for (int j = 0; j < 6; j++)
          packet[i * 6 + j] = hwAddress[j];

      return packet;
    }
  }
}