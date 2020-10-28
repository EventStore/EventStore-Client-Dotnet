# Adding a gRPC interceptor

gRPC clients allow you to create [Interceptors](https://grpc.github.io/grpc/csharp/api/Grpc.Core.Interceptors.Interceptor.html) that can intercept the gRPC requests. 

The EventStoreDB client allows you to add one of these to the connection that can be used for logging all requests made or other such things. 

To create one, add a new class, which inherits from `Interceptor` and override the methods that you want to intercept.

<<< @/samples/connecting-to-a-single-node/DemoInterceptor.cs#interceptor

Then add this to the array of interceptors on the `EventStoreClientSettings` class.

<<< @/samples/connecting-to-a-single-node/Program.cs#adding-an-interceptor
