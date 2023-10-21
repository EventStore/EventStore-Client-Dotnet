namespace EventStore.Client;

using System;

public static class OperatingSystemExtensions {
    public static bool IsWindows(this OperatingSystem operatingSystem) =>
        operatingSystem.Platform != PlatformID.Unix 
     && operatingSystem.Platform != PlatformID.MacOSX;
}