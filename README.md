# NetNoise — Network Security Monitor

NetNoise is a Windows desktop network security monitoring project developed with **C#**, **.NET 8**, and **Windows Forms**. It captures network packets, parses supported protocols, and raises alerts for suspicious traffic patterns. Developed as a university project for learning network protocols, object-oriented architecture, and intrusion detection.

## Features

- Live packet capture with selectable network interface
- TCP, HTTP, DNS (UDP), and ARP parsing
- Packet filtering, searching, counters, and payload viewer (including enlarged view)
- Alerts displayed in a dark-themed WinForms dashboard
- Alert export and signature-rule import (depending on the current build)

### Detection modules

| Module | Detection approach |
| --- | --- |
| SYN port-scan detection | Counts distinct destination ports for SYN-only packets within a time window |
| HTTP plaintext credential detection | Searches visible unencrypted HTTP POST content for credential fields |
| DNS tunneling heuristics | Checks query length, number of subdomains, and character entropy |
| IOC matching | Compares observed IP addresses and DNS query names against configured lists |
| Signature detection | Searches TCP payload text for configured patterns |
| ARP spoofing detection | Flags changes in observed IP-to-MAC mappings |

These are **heuristic detections**, not proof of malicious activity. False positives are possible. Encrypted HTTPS payloads cannot be read as plaintext without additional decryption capabilities.

## Requirements

### Run the application

- Windows 10 or Windows 11, x64
- **.NET 8 Desktop Runtime (Windows Desktop)**, unless published as self-contained
- **Npcap** installed from https://npcap.com/ with **WinPcap API-compatible Mode** enabled for SharpPcap's native `wpcap.dll` dependency
- Network adapter with packet-capture support
- Administrator privileges may be needed for capture

### Build from source

- Visual Studio 2022 (version supporting .NET 8) with **.NET desktop development** workload, or the .NET 8 SDK
- NuGet package restore / internet access for initial dependency installation
- Project references for the solution's UI, capture, core, and detection libraries
- SharpPcap and PacketDotNet packages (restore versions defined in project files)

### Optional test tools

- Nmap: https://nmap.org/download.html (not installed by Npcap)
- curl (included on many Windows installations)
- `nslookup` for DNS queries

## Getting started

1. Clone or download the repository.
2. Open the solution (`.sln`) in Visual Studio.
3. Restore NuGet packages and build the solution.
4. Set the **NetNoise** WinForms project as the startup project.
5. Install Npcap, selecting **WinPcap API-compatible Mode**.
6. Run the application, select the interface that carries the traffic you want to inspect, and click **Start**.
7. Generate authorized test traffic and inspect the packet feed and alerts.

CLI build (from the directory containing the solution):

```powershell
dotnet restore
dotnet build -c Release
```

To run from Visual Studio, press **F5**. The exact executable path depends on the project structure and build configuration.

## Example tests (your own device or authorized lab only)

### HTTP POST credential detection

Use a **local test HTTP endpoint** that accepts POST requests, replacing the example address and port with your lab server:

```powershell
curl.exe -X POST http://127.0.0.1:8080/login -d "username=demo&password=example-only"
```

> Loopback traffic may require capturing on the Npcap Loopback Adapter rather than the physical Ethernet adapter. Do not use real passwords or tokens in test traffic.

### DNS detection

```powershell
nslookup a.b.c.d.e.f.example.com 8.8.8.8
```

This can trigger the many-subdomains heuristic if the DNS query is captured and the detector is enabled. DNS capture depends on the actual network route and interface.

### SYN port-scan detection

```powershell
nmap -sS -p 1-100 192.168.178.113
```

Replace the IP with **your own authorized lab target**. Start capture before the scan. Detection depends on the configured threshold, time window, capture visibility, and permissions. In one implementation, the detector uses **20 distinct ports in 10 seconds**, tracked by source/destination IP pair.

### IOC and signature tests

Import your configured IOC and signature rules using the format supported by your current build. Generate traffic involving **safe, lab-controlled test indicators**; an IOC match depends on the exact values loaded. Signature detection checks visible TCP payload text, not encrypted HTTPS data.

## Architecture

```text
Network interface
    |
    v
SharpPcap (capture)
    |
    v
PacketDotNet (low-level packet decoding)
    |
    +--> Protocol parsers (TCP / HTTP / DNS / ARP)
    |         |
    |         v
    |     Packet models
    |         |
    |         v
    +--> Detection modules --> Detection alerts
    |
    v
WinForms dashboard (packet list / payload / alerts / statistics)
```

The implementation separates packet models, parsing, detection, and presentation. The capture callback schedules GUI updates on the UI thread via `BeginInvoke`.

## Troubleshooting

**`Unable to load DLL 'wpcap'`**  
Install or repair Npcap and enable WinPcap API-compatible Mode. Restart Visual Studio and the app.

**No packets captured**  
Check that capture has started, select the adapter carrying the traffic, and try running as administrator. WSL/Hyper-V, Ethernet, Wi-Fi, VPN, and loopback traffic can use different interfaces.

**DNS not visible**  
DNS usually uses UDP port 53, but can also use TCP or encrypted transports. A UDP-only DNS parser will not decode DNS-over-TCP or DNS-over-HTTPS. Ensure UDP packets are not discarded by an early TCP-only return.

**No alerts**  
Confirm that packets appear in the feed, rules are loaded, detectors are called, and their thresholds are met. An empty alert panel does not necessarily indicate a capture failure.

**`nmap` not recognized**  
Install Nmap separately and ensure its installation folder is on the Windows `PATH`.

## Scope and limitations

- Intended for educational and authorized network monitoring.
- Detection is based on observed traffic and configured rules; it is not a replacement for a production IDS.
- TCP application messages can span multiple packets; complete HTTP analysis requires stream reassembly.
- Some alerts may repeat or produce false positives without further correlation and suppression.
- ARP detection requires visibility into the relevant Layer-2 network; NAT-based WSL environments are often unsuitable for realistic ARP tests.

## Technology stack

C# · .NET 8 · WinForms · SharpPcap · PacketDotNet · JSON-based rules/IOCs

## License

Add the repository's chosen license here, if applicable.
