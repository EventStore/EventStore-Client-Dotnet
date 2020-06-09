# Connecting to a cluster

For redundancy, you can run EventStoreDB in a cluster. In this scenario you should specify all the nodes in your cluster when connecting. This allows your client to discover which node it should be speaking to at any given time.

::: warning
Important: You should never hide your cluster behind a load balancer as this will stop your clients from connecting to the correct nodes.
:::

<<< @/samples/connecting-to-a-cluster/Program.cs#connecting-to-a-cluster
 
## Fine-tuning cluster connection

There are a number of additional settings that can be altered when connecting to a cluster.

<<< @/samples/connecting-to-a-cluster/Program.cs#connecting-to-a-cluster-complex

### DiscoveryInterval

### GossipTimeout (Default: 10sec)

The length of time the client will attempt to get a gossip request from a node. The gossip requests lets the client know the current state of any given node. To find out more see 

> TODO: Link to gossip documentation

### NodePreference (Default: NodePreference.Leader)

You can tell your client which node type you would prefer to connect to. The options are:

| Option | Purpose |
|:------ |:------- |
| `NodePreference.Leader` | Always try and connect to the leader node |
| `NodePreference.Follower` | Always try and connect to a follower node | 
| `NodePreference.Random` | Connect to a random node type (useful if you want to spread the connections around) |
| `NodePreference.ReadOnlyReplica` | Always try and connect to a read only replica |

To find out more about the different node types see

> TODO: Link to node types.

### MaxDiscoveryAttempts

> TODO: Detail max discovery attempts 


