#pragma warning disable CS8321 // Local function is declared but never used

using System.Text.Json;
using Grpc.Core;

//// This sample demonstrate the projection management features.
//// It will create data in the target database.
//// It will create a series of projections with the following content
//// fromAll() .when({$init:function(){return {count:0};},$any:function(s, e){s.count += 1;}}).outputState();

const string connection = "esdb://localhost:2113?tls=false";

var managementClient = ManagementClient(connection);

Console.WriteLine("Populate data");
await Populate(connection, 100);
Console.WriteLine("RestartSubSystem");
await RestartSubSystem(managementClient);
await Task.Delay(500); // give time to the subsystem to restart

Console.WriteLine("Disable");
await Disable(managementClient);
Console.WriteLine("Disable Not Found");
await DisableNotFound(managementClient);

Console.WriteLine("Enable");
await Enable(managementClient);
Console.WriteLine("Enable Not Found");
await EnableNotFound(managementClient);

Console.WriteLine("Delete");
await Delete(managementClient);

Console.WriteLine("Abort");
await Abort(managementClient);
Console.WriteLine("Abort Not Found");
await Abort_NotFound(managementClient);

Console.WriteLine("Reset");
await Reset(managementClient);
Console.WriteLine("Reset Not Found");
await Reset_NotFound(managementClient);

Console.WriteLine("CreateContinuous");
await CreateContinuous(managementClient);
Console.WriteLine("CreateContinuous conflict");
await CreateContinuous_Conflict(managementClient);
//Console.WriteLine("CreateOneTime");
//await CreateOneTime(managementClient);
Console.WriteLine("Update");
await Update(managementClient);
Console.WriteLine("Update_NotFound");
await Update_NotFound(managementClient);
Console.WriteLine("ListAll");
await ListAll(managementClient);
Console.WriteLine("ListContinuous");
await ListContinuous(managementClient);
Console.WriteLine("GetStatus");
await GetStatus(managementClient);
// Console.WriteLine("GetState");
// await GetState(managementClient);
Console.WriteLine("GetResult");
await GetResult(managementClient);

return;

static KurrentProjectionManagementClient ManagementClient(string connection) {
	#region createClient

	var settings = KurrentClientSettings.Create(connection);
	settings.ConnectionName     = "Projection management client";
	settings.DefaultCredentials = new UserCredentials("admin", "changeit");
	var managementClient = new KurrentProjectionManagementClient(settings);

	#endregion createClient

	return managementClient;
}

static async Task RestartSubSystem(KurrentProjectionManagementClient managementClient) {
	#region RestartSubSystem

	await managementClient.RestartSubsystemAsync();

	#endregion RestartSubSystem
}

static async Task Disable(KurrentProjectionManagementClient managementClient) {
	#region Disable

	await managementClient.DisableAsync("$by_category");

	#endregion Disable
}

static async Task DisableNotFound(KurrentProjectionManagementClient managementClient) {
	#region DisableNotFound

	try {
		await managementClient.DisableAsync("projection that does not exists");
	}
	catch (RpcException e) when (e.StatusCode is StatusCode.NotFound) {
		Console.WriteLine(e.Message);
	}
	catch (RpcException e) when (e.Message.Contains("NotFound")) { // will be removed in a future release
		Console.WriteLine(e.Message);
	}

	#endregion DisableNotFound
}

static async Task Enable(KurrentProjectionManagementClient managementClient) {
	#region Enable

	await managementClient.EnableAsync("$by_category");

	#endregion Enable
}

static async Task EnableNotFound(KurrentProjectionManagementClient managementClient) {
	#region EnableNotFound

	try {
		await managementClient.EnableAsync("projection that does not exists");
	}
	catch (RpcException e) when (e.StatusCode is StatusCode.NotFound) {
		Console.WriteLine(e.Message);
	}
	catch (RpcException e) when (e.Message.Contains("NotFound")) { // will be removed in a future release
		Console.WriteLine(e.Message);
	}

	#endregion EnableNotFound
}

static Task Delete(KurrentProjectionManagementClient managementClient) {
	#region Delete

	// this is not yet available in the .net grpc client

	#endregion Delete

	return Task.CompletedTask;
}

static async Task Abort(KurrentProjectionManagementClient managementClient) {
	try {
		var js =
			"fromAll() .when({$init:function(){return {count:0};},$any:function(s, e){s.count += 1;}}).outputState();";

		await managementClient.CreateContinuousAsync("countEvents_Abort", js);
	}
	catch (RpcException e) when (e.StatusCode is StatusCode.Aborted) {
		// ignore was already created in a previous run
	}
	catch (RpcException e) when (e.Message.Contains("Conflict")) { // will be removed in a future release
		// ignore was already created in a previous run
	}

	#region Abort

	// The .net clients prior to version 21.6 had an incorrect behavior: they will save the checkpoint.
	await managementClient.AbortAsync("countEvents_Abort");

	#endregion Abort
}

static async Task Abort_NotFound(KurrentProjectionManagementClient managementClient) {
	#region Abort_NotFound

	try {
		await managementClient.AbortAsync("projection that does not exists");
	}
	catch (RpcException e) when (e.StatusCode is StatusCode.NotFound) {
		Console.WriteLine(e.Message);
	}
	catch (RpcException e) when (e.Message.Contains("NotFound")) { // will be removed in a future release
		Console.WriteLine(e.Message);
	}

	#endregion Abort_NotFound
}

static async Task Reset(KurrentProjectionManagementClient managementClient) {
	try {
		var js =
			"fromAll() .when({$init:function(){return {count:0};},$any:function(s, e){s.count += 1;}}).outputState();";

		await managementClient.CreateContinuousAsync("countEvents_Reset", js);
	}
	catch (RpcException e) when (e.StatusCode is StatusCode.Internal) {
		// ignore was already created in a previous run
	}
	catch (RpcException e) when (e.Message.Contains("Conflict")) { // will be removed in a future release
		// ignore was already created in a previous run
	}

	#region Reset

	// Checkpoint will be written prior to resetting the projection
	await managementClient.ResetAsync("countEvents_Reset");

	#endregion Reset
}

static async Task Reset_NotFound(KurrentProjectionManagementClient managementClient) {
	#region Reset_NotFound

	try {
		await managementClient.ResetAsync("projection that does not exists");
	}
	catch (RpcException e) when (e.StatusCode is StatusCode.NotFound) {
		Console.WriteLine(e.Message);
	}
	catch (RpcException e) when (e.Message.Contains("NotFound")) { // will be removed in a future release
		Console.WriteLine(e.Message);
	}

	#endregion Reset_NotFound
}

static async Task CreateOneTime(KurrentProjectionManagementClient managementClient) {
	const string js =
		"fromAll() .when({$init:function(){return {count:0};},$any:function(s, e){s.count += 1;}}).outputState();";

	await managementClient.CreateOneTimeAsync(js);
}

static async Task CreateContinuous(KurrentProjectionManagementClient managementClient) {
	#region CreateContinuous

	const string js = @"fromAll()
							    .when({
							        $init: function() {
							            return {
							                count: 0
							            };
							        },
							        $any: function(s, e) {
							            s.count += 1;
							        }
							    })
							    .outputState();";

	var name = $"countEvents_Create_{Guid.NewGuid()}";
	await managementClient.CreateContinuousAsync(name, js);

	#endregion CreateContinuous
}

static async Task CreateContinuous_Conflict(KurrentProjectionManagementClient managementClient) {
	const string js = @"fromAll()
							    .when({
							        $init: function() {
							            return {
							                count: 0
							            };
							        },
							        $any: function(s, e) {
							            s.count += 1;
							        }
							    })
							    .outputState();";

	var name = $"countEvents_Create_{Guid.NewGuid()}";

	#region CreateContinuous_Conflict

	await managementClient.CreateContinuousAsync(name, js);
	try {
		await managementClient.CreateContinuousAsync(name, js);
	}
	catch (RpcException e) when (e.StatusCode is StatusCode.AlreadyExists) {
		Console.WriteLine(e.Message);
	}
	catch (RpcException e) when (e.Message.Contains("Conflict")) { // will be removed in a future release
		var format = $"{name} already exists";
		Console.WriteLine(format);
	}

	#endregion CreateContinuous_Conflict
}

static async Task Update(KurrentProjectionManagementClient managementClient) {
	#region Update

	const string js = @"fromAll()
							    .when({
							        $init: function() {
							            return {
							                count: 0
							            };
							        },
							        $any: function(s, e) {
							            s.count += 1;
							        }
							    })
							    .outputState();";

	var name = $"countEvents_Update_{Guid.NewGuid()}";

	await managementClient.CreateContinuousAsync(name, "fromAll().when()");
	await managementClient.UpdateAsync(name, js);

	#endregion Update
}

static async Task Update_NotFound(KurrentProjectionManagementClient managementClient) {
	#region Update_NotFound

	try {
		await managementClient.UpdateAsync("Update Not existing projection", "fromAll().when()");
	}
	catch (RpcException e) when (e.StatusCode is StatusCode.NotFound) {
		Console.WriteLine(e.Message);
	}
	catch (RpcException e) when (e.Message.Contains("NotFound")) { // will be removed in a future release
		Console.WriteLine("'Update Not existing projection' does not exists and can not be updated");
	}

	#endregion Update_NotFound
}

static async Task ListAll(KurrentProjectionManagementClient managementClient) {
	#region ListAll

	var details = managementClient.ListAllAsync();
	await foreach (var detail in details)
		Console.WriteLine(
			$@"{detail.Name}, {detail.Status}, {detail.CheckpointStatus}, {detail.Mode}, {detail.Progress}"
		);

	#endregion ListAll
}

static async Task ListContinuous(KurrentProjectionManagementClient managementClient) {
	#region ListContinuous

	var details = managementClient.ListContinuousAsync();
	await foreach (var detail in details)
		Console.WriteLine(
			$@"{detail.Name}, {detail.Status}, {detail.CheckpointStatus}, {detail.Mode}, {detail.Progress}"
		);

	#endregion ListContinuous
}

static async Task GetStatus(KurrentProjectionManagementClient managementClient) {
	const string js =
		"fromAll().when({$init:function(){return {count:0};},$any:function(s, e){s.count += 1;}}).outputState();";

	var name = $"countEvents_status_{Guid.NewGuid()}";

	#region GetStatus

	await managementClient.CreateContinuousAsync(name, js);
	var status = await managementClient.GetStatusAsync(name);
	Console.WriteLine(
		$@"{status?.Name}, {status?.Status}, {status?.CheckpointStatus}, {status?.Mode}, {status?.Progress}"
	);

	#endregion GetStatus
}

static async Task GetState(KurrentProjectionManagementClient managementClient) {
	// will have to wait for the client to be fixed before we import in the doc 

	#region GetState

	const string js =
		"fromAll().when({$init:function(){return {count:0};},$any:function(s, e){s.count += 1;}}).outputState();";

	var name = $"countEvents_State_{Guid.NewGuid()}";

	await managementClient.CreateContinuousAsync(name, js);
	//give it some time to process and have a state.
	await Task.Delay(500);

	var stateDocument = await managementClient.GetStateAsync(name);
	var result        = await managementClient.GetStateAsync<Result>(name);

	Console.WriteLine(DocToString(stateDocument));
	Console.WriteLine(result);

	static async Task<string> DocToString(JsonDocument d) {
		await using var stream = new MemoryStream();
		var             writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });
		d.WriteTo(writer);
		await writer.FlushAsync();
		return Encoding.UTF8.GetString(stream.ToArray());
	}

	#endregion GetState
}

static async Task GetResult(KurrentProjectionManagementClient managementClient) {
	#region GetResult

	const string js = @"fromAll()
							    .when({
							        $init: function() {
							            return {
							                count: 0
							            };
							        },
							        $any: function(s, e) {
							            s.count += 1;
							        }
							    })
							    .outputState();";

	var name = $"countEvents_Result_{Guid.NewGuid()}";

	await managementClient.CreateContinuousAsync(name, js);
	await Task.Delay(500); //give it some time to have a result.

	// Results are retrieved either as  JsonDocument or a typed result 
	var document = await managementClient.GetResultAsync(name);
	var result   = await managementClient.GetResultAsync<Result>(name);

	Console.WriteLine(DocToString(document));
	Console.WriteLine(result);

	static string DocToString(JsonDocument d) {
		using var stream = new MemoryStream();
		using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });
		d.WriteTo(writer);
		writer.Flush();
		return Encoding.UTF8.GetString(stream.ToArray());
	}

	#endregion GetResult
}

static async Task Populate(string connection, int numberOfEvents) {
	var settings = KurrentClientSettings.Create(connection);
	settings.DefaultCredentials = new UserCredentials("admin", "changeit");
	var client = new KurrentClient(settings);
	var messages = Enumerable.Range(0, numberOfEvents).Select(
		number =>
			new EventData(
				Uuid.NewUuid(),
				"eventtype",
				Encoding.UTF8.GetBytes($@"{{ ""Id"":{number} }}")
			)
	);

	await client.AppendToStreamAsync("sample", StreamState.Any, messages);
}

public class Result {
	public int count { get; set; }

	public override string ToString() => $"count= {count}";
};
