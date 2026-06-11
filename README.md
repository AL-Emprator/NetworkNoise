# Netzwerk-Sicherheitsmonitor

Ein GUI-basiertes Tool zur Echtzeitüberwachung der Netzwerksicherheit, entwickelt mit C# und .NET.

## Funktionen

* Echtzeit-Paketerfassung
* TCP-, HTTP- und DNS-Analyse
* Überwachung von eingehendem und ausgehendem Datenverkehr
* Erkennung von Portscans
* Erkennung von ARP-Spoofing
* Erkennung von DNS-Tunneling
* Erkennung von Klartext-Zugangsdaten
* Erkennung bösartiger IP-Adressen und Domains
* Signaturbasierte Paketprüfung
* Alarmsystem mit Live-Benachrichtigungen in der GUI
* Multithread-Architektur für hohe Leistung

## Technologien

* C#
* .NET 8
* WinForms
* SharpPcap
* PacketDotNet

## Architektur

```txt
GUI
 ├── Core
 ├── Capture
 └── Detection

Capture
 └── Core

Detection
 └── Core
```
