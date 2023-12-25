# LUNS - Local UDP Network Simulation

Create simulated UDP networks for testing purpose on your local computer.

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

## Common Pitfalls / Known Issues

Good to know before you start with LUNS:
- Multicast traffic is only simulated and split into multiple unicast routes. No real multicast IP addresses are used. Applications detecting multicast traffic via IP address will not behave correctly.
- Even though the random generator is managed centrally and a seed can be defined, simulations will currently not be repeatable beacause of non-deterministic multithreading.
- If you are using a protocol on top of UDP, keep in mind that bit errors will be introduced in the higher protocol header data as well. You may want to keep the bit errors at 0% until further notice.

## Getting Started

Check out the LUNS.Demo project to see LUNS in action.

## Roadmap

- Extended demo application to show more use cases.
- Logger support.
- Monitoring interface to see what's going on.
- Config file and/or config tool interface.

## License

Copyright © Andreas Helbling

Distributed under the MIT License.
