# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Fixed
- Get Certifications Path More Reliably [EventStore-Client-DotNet#178](https://github.com/EventStore/EventStore-Client-Dotnet/pull/178)
- Make Client More Backwards Compatibility Friendly [EventStore-Client-DotNet#125](https://github.com/EventStore/EventStore-Client-Dotnet/pull/125)
- Send correct writeCheckpoint option when disabling/aborting a projection [EventStore-Client-DotNet#116](https://github.com/EventStore/EventStore-Client-Dotnet/pull/116)

### Removed
- Remove autoAck from Persistent Subscriptions [EventStore-Client-DotNet#175](https://github.com/EventStore/EventStore-Client-Dotnet/pull/175)

### Added
- Detect Server Capabilities [EventStore-Client-DotNet#172](https://github.com/EventStore/EventStore-Client-Dotnet/pull/172)
- Implement Last/Next StreamPosition/Position [EventStore-Client-DotNet#151](https://github.com/EventStore/EventStore-Client-Dotnet/pull/151)
- Add filtered persistent subscriptions [EventStore-Client-DotNet#122](https://github.com/EventStore/EventStore-Client-Dotnet/pull/122)
- Implement persistent subscriptions to $all: [EventStore-Client-DotNet#108](https://github.com/EventStore/EventStore-Client-Dotnet/pull/108)
- Implement parameterless IComparable for StreamPosition and StreamRevision [EventStore-Client-DotNet#111](https://github.com/EventStore/EventStore-Client-Dotnet/pull/111)

### Changed
- Adjustments to Disposal [EventStore-Client-DotNet#189](https://github.com/EventStore/EventStore-Client-Dotnet/pull/189)
- send 'requires-leader' header based on NodePreference [EventStore-Client-DotNet#131](https://github.com/EventStore/EventStore-Client-Dotnet/pull/131)

## [21.2.0] - 2021-02-22

### Fixed
- Fix Default Keep Alive [EventStore-Client-DotNet#107](https://github.com/EventStore/EventStore-Client-Dotnet/pull/107)
- Check Disposal Before Invoking CheckpointReached [EventStore-Client-DotNet#105](https://github.com/EventStore/EventStore-Client-Dotnet/pull/105)
- Fixed Enumerator Exception Being Overridden w/ DeadlineExceeded [EventStore-Client-DotNet#100](https://github.com/EventStore/EventStore-Client-Dotnet/pull/100)

### Changed
- Use Grpc.Core for netcoreapp3.1 and net48 [EventStore-Client-DotNet#93](https://github.com/EventStore/EventStore-Client-Dotnet/pull/93)

## [20.10.0] - 2020-12-09

### Added
- Add Support for Single DNS Gossip Seed [EventStore-Client-DotNet#91](https://github.com/EventStore/EventStore-Client-Dotnet/pull/91)
- Add Connection String Overloads for DI Extensions [EventStore-Client-DotNet#83](https://github.com/EventStore/EventStore-Client-Dotnet/pull/83)
- Add projection reset to client [EventStore-Client-DotNet#79](https://github.com/EventStore/EventStore-Client-Dotnet/pull/79)

### Changed
- Increase gRPC Deadline to Infinite on Persistent Subscriptions [EventStore-Client-DotNet#84](https://github.com/EventStore/EventStore-Client-Dotnet/pull/84)

## [20.6.1] - 2020-09-30

### Added
- Add restarting persistent subscriptions [EventStore-Client-DotNet#68](https://github.com/EventStore/EventStore-Client-Dotnet/pull/68)
- Implement connection string [EventStore-Client-DotNet#49](https://github.com/EventStore/EventStore-Client-Dotnet/pull/49)
- Add GossipOverHttps option to EventStoreClientConnectivitySettings [EventStore-Client-DotNet#51](https://github.com/EventStore/EventStore-Client-Dotnet/pull/51)
- Add Service Collection Extensions to all Clients [EventStore-Client-DotNet#45](https://github.com/EventStore/EventStore-Client-Dotnet/pull/45)
- Add ChannelCredentials to EventStoreClientSettings [EventStore-Client-DotNet#46](https://github.com/EventStore/EventStore-Client-Dotnet/pull/46)

### Changed
- WrongExpectedVersionResult / WrongExpectedVersionException will use values from server [EventStore-Client-DotNet#73](https://github.com/EventStore/EventStore-Client-Dotnet/pull/73)
- Convert from StreamPosition to StreamRevision; StreamRevision on IWriteResult [EventStore-Client-DotNet#53](https://github.com/EventStore/EventStore-Client-Dotnet/pull/53)
- Use gRPC Auth Pipeline Instead of Metadata [EventStore-Client-DotNet#52](https://github.com/EventStore/EventStore-Client-Dotnet/pull/52)

## [20.6.0] - 2020-06-09

### Added
- Support infinite timeouts [EventStore-Client-DotNet#30](https://github.com/EventStore/EventStore-Client-Dotnet/pull/30)
- Do gossip requests over gRPC [EventStore-Client-DotNet#27](https://github.com/EventStore/EventStore-Client-Dotnet/pull/27)
- Support Bearer Tokens in User Credentials [EventStore-Client-DotNet#24](https://github.com/EventStore/EventStore-Client-Dotnet/pull/24)

### Changed
- Restructured stream name for future planned changes [EventStore-Client-DotNet#33](https://github.com/EventStore/EventStore-Client-Dotnet/pull/33)
- Rename HttpEndPointIp to HttpEndPointAddress [EventStore-Client-DotNet#32](https://github.com/EventStore/EventStore-Client-Dotnet/pull/32)

## [20.6.0-rc] - 2020-06-15

- Initial Release
