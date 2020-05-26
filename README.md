# PMI PubSub Module

PubSub Module was created by PMI to publish OPC UA data and events coming from configured TMC OPC UA server.

## Repository content

- Stack: source code of OPC UA Stack (provided by OPC Foundation)
- Opc.Ua.PubSub*: source code of OPC UA PubSub implementation based on prototyping provided by OPC Foundation 
- PMIE.PubSubOpcUaServer: source code of the PubSub OPC UA server aka PubSub Module

## Requirements

To compile and run the code, the following items are required:
- Visual Studio 2017+ with .NET Core development feature
- .NET Core SDK 3.1+

### Usage

Build:
> dotnet publish ./PMIE.ACC.OpcUaServer/PMIE.ACC.OpcUaServer.csproj -c Release -o out

Run:
> dotnet out/PMIE.PubSubOpcUaServer.dll

### ENV variables

> ENV KEYFRAMECOUNT [value]

Specify frame interval for keyframe to be sent; "1" means every frame

> ENV OPCUA_SERVER_URL [opc.tcp://localhost:48030]

Uri of the source OPC UA server

> ENV BROKER_IP [ipaddress/hostname]

IP address or host name of the MQTT broker

> ENV BROKER_PORT 1883

Port number of the MQTT broker

> ENV BROKER_SECURITY [value]

Authentication mode for connecting to MQTT broker
* 0: none
* 1: user/pass
* 2: certificate

> ENV BROKER_USERNAME [username]

The username used for authenticating when BROKER_SECURITY is 1

> ENV BROKER_PASSWORD cryptedpassword

The password used for authenticating when BROKER_SECURITY is 1.
Password must be encrypted using AES_128_CBC and 'ipaddress_username' as key.
https://encode-decode.com/aes-128-cbc-encrypt-online/

> ENV MQTT_CLIENT_CERT AppData/Certs/cert.crt
> ENV MQTT_CLIENT_CA_CERT AppData/Certs/cert.crt

Certificates used for authenticating when BROKER_SECURITY is 2