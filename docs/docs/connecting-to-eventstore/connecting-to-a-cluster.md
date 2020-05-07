# Connecting to a cluster

For redundancy you can run Event Store as a cluster. In this scenario you should specify all of the nodes in your cluster when connecting. This allows your client to discover which node it should be speaking to at any given time

> Important: You should never hide your cluster behind a load balancer as this will stop your clients from connecting to the correct nodes.

@[code transcludeWith=//connecting-to-a-cluster](@/samples/connecting-to-a-cluster/Program.cs)
 
## Fine tuning cluster connection

There are a number of additional settings that can be altered when connecting to a cluster.

@[code transcludeWith=//connecting-to-a-cluster-complex](@/samples/connecting-to-a-cluster/Program.cs)

### DiscoveryInterval

### GossipTimeout (Default: 10sec)

The length of time the client will attempt to get a gossip request from a node. The gossip requests lets the client know the current state of any given node. To find out more see 

> TODO: Link to gossip documentation

### NodePreference (Default: NodePreference.Leader)

You can tell your client which node type you would prefer to connect to. The options are:

- `NodePreference.Leader`: will always try and connect to the leader node
- `NodePreference.Follower`: will always try and connect to a follower node 
- `NodePreference.Random`: will connect to a random node type (useful if you want to spread the connections around)
- `NodePreference.ReadOnlyReplica`: will always try and connect to a read only replica

To find out more about the different node types see

> TODO: Link to node types.

### MaxDiscoveryAttempts

> TODO: Detail max discovery attempts 


