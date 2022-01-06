MeshCentral Discovery
=====================

For more information, [visit MeshCentral.com](http://www.meshcentral.com).

This is a small Windows tool that searches for MeshCentral servers on the local network that are in LAN mode or Hybrid mode. In LAN mode, MeshCentral does not have a fixed IP address or DNS name and MeshAgents will multicast on the local network to find their server. This tool does the same multicast search to find server and offers a quick way to open the web page.

In the settings section of the MeshCentral config.json, you can add this section to customize the strings that will show up on the discovery tool.

    "localDiscovery": {
      "name": "Local server name",
      "info": "Information about this server"
    }


License
-------

This software is licensed under [Apache 2.0](https://www.apache.org/licenses/LICENSE-2.0).
