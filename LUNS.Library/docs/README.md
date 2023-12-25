# LUNS.Library - Local UDP Network Simulation

The LUNS.Library package provides the core functionality to create simulated UDP networks for testing purpose.

## Purpose

You are developing a decentralized, distributed software or system which exchanges data via UDP and you want to automate testing with a large number of nodes? LUNS is the tool which helps you make it possible.

LUNS provides a network simulation for UDP traffic, including:
- Explicity route definition: which node can send data to which other node(s).
- Routing of unicast and multicast packets.
- Route quality settings: Define bitrate, packet loss rate and bit error rate for each route individually.
- Separation of applications and multicast endpoints: Keep them packets logically seperated.
- Custom port assignment: You are in control which listen ports are used.
- Lightweight: No special network configuration is required. LUNS runs all traffic on the localhost adresses.
- Transparency: Localhost traffic can be observed, e.g. using Wireshark.
- Scalability: Support for up to 254 multicast groups and 240 nodes.

LUNS helps developers write automated test routines in scenarios where several applications communicate with each other in a distributed network of nodes.

## Getting Started

A Demo project will be available soon.

## Roadmap

- The LUNS Library will undergo its first real-world testing phase until end of 2023.
- Source repository and ways of contributing will be made available in 2024.

## License

Copyright © Andreas Helbling

Distributed under the MIT License.
