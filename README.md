# BlackSP
*Barebone Library for Anonymous Containerized Key-value Stream Processing*

## Concepts
- **Vertex**
An abstract concept that represents a compute unit in an operator graph, each Vertex has an OperatorShell with Operator and Input/OutputEndpoints depending on their connections in the graph. A vertex is sharded by default (no sharding = 1 shard).
- **OperatorShell**
Core element that hosts user defined Operators and consumes events provided by InputEndpoints, invokes the Operator on them and forwards the result to connected OutputEndpoints. Implementations vary from simple ordered operating for mapping and filtering to windowed variants for aggregates and joins.
- **Operator**
Container for user defined functions that perform various operations on events. Can utilize local state in the form of class properties.
- **InputEndpoint**
Core element that ingresses multiple streams from one sharded Vertex. Messages are sequentially read from the incoming stream, deserialized in order and enqueued in the local OperatorShell. One InputEndpoint consumes incoming streams for all shards of the sending Vertex.
- **OutputEndpoint**
Core element that egresses multiple streams to one sharded Vertex. Events that are enqueued in the OutputEndpoint by the OperatorShell are serialized to messages and partitioned to reach their relevant remote vertex shard. One OutputEndpoint consumes outgoing streams for all shards of the receiving Vertex.
- **Events**
A notion for the data passing through the Stream Processor. These are basically simple objects containing data.
- **Infrastructure**
A general notion for the context on which BlackSP is running, currently these take the shape of an in-memory infrastructure and there is a CRA binding to run BlackSP on CRA workers.

## Modules
This section describes the modules present in BlackSP and some of their primary responsibilities/functionalities.

**TODO: INSERT MODULE DEPENDENCY DIAGRAM**

### BlackSP.Kernel
**Type: Class library**
Defines application-wide interfaces and primarily serves as a root dependency for other modules.

### BlackSP.Core
**Type: Class library**
Implements OperatorShell and Endpoint interfaces as defined in **BlackSP.Kernel**. Provides abstract and concrete implementations for regular and windowed operators, for example: Source-/Sink-/Join-/Map-OperatorSocket. Each OperatorSocket consumes an Operator which is user defined and adheres to relevant interfaces. For example the MapOperator which requires a Map method to be implemented. These Operator classes are ment to be implemented by the consumer of the library and can be statefull through class properties.

The Endpoint implementations include the primary streaming and partitioning logic. It depends on a serializer to perform the actual serialization. Endpoints expect to be connected to one-another by the infrastructure they run on.

No concrete implementatations of events are required, the system only expect the user-defined events to adhere to the IEvent interface to be consumable by the library.

### BlackSP.Serialization
**Type: Class library**
Implements Serializers and was primarily introduced as a module to separate this from the Core. Initially several serializers were implemented for testing purposes (tried libraries: Newtonsoft.Json, ZeroFormatter, ApexSerializer, Protobuf). Once Protobuf proved by far the most efficient for this use-case the others were removed. This library therefore only wraps the Protobuf dependency in the interface defined in **BlackSP.Kernel**.

Note: due to the annotation requirements posed by the Protobuf-net dependency the end user defining an IEvent class needs to annotate it with Protobuf attributes. Sadly this cannot be abstracted away from the end-user in its current form and will probably be left as is.

### BlackSP.Infrastructure
**Type: Class library**
This module brings together the root modules of BlackSP. It cannot run on its own but it does define the public configurator API which enables compile-time typechecking and storing the user defined operator graph to be consumed by a dependency.

As the library has been written with Dependency Injection in mind (for testability and ease of growth), Autofac container setup code specifically for BlackSP is also defined here.

### BlackSP.InMemory
**Type: Class library**
Consumes the **BlackSP.Infrastructure** module to provide launching a BlackSP operator graph in memory on multiple threads. The module has been written such that different operators cannot reach eachother in any way that in a distributed system would not be possible. There is no network involved but the endpoints still communicate via streams like they would on the CRA infrastructure. Each operator is launched the same way they are on CRA to ensure that when a BlackSP implementation works on this infrastructure it will also on **BlackSP.CRA**.

Note: On this infrastructure two endpoints will have to fight for locks on the stream they are reading/writing to, this limits throughput but as this infrastructures purpose is for testing and development purposes it is currently not considered a problem.

### BlackSP.CRA
**Type: Class library**
Consumes the **BlackSP.Infrastructure** module to provide launching a BlackSP operator graph on CRA. It uses the configurator API to provide translation from the user-defined operator graph to a CRA vertex graph. Furthermore it includes kubernetes utilities to automatically write the configuration to a deployment file which can then be launched on any kubernetes cluster. Due to CRA's nature we can always use the same container, the code is uploaded to an Azure Storage account and each CRA vertex pulls in its configuration and relevant binaries to execute.

When specific runtime arguments are passed to the Launcher of this module it will launch a local CRA vertex, this enables debugging a single operator instance in a real distributed setting.

### BlackSP.ThroughputExperiment
**Type: Console application**
An example of an end-user implementation of BlackSP. It contains some operator implementations and a graph configuration with can be passed to the launcher of either  **BlackSP.InMemory** for local operation or **BlackSP.CRA** to prepare launch on a kubernetes cluster with CRA workers.

### BlackSP.*.UnitTests
**Type: Test application**
Each of these contain unit tests for the module with matching name. Currently a few tests are failing due to recent performance improvements, these will be updated to be in line with the changes that were made.