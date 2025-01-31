﻿using System;
#if DEBUG
using System.Diagnostics;
#endif

/// <summary>
/// A container for all shared <see cref="AppContext"/> configuration switches for ComputeSharp.
/// </summary>
internal static class Configuration
{
    /// <summary>
    /// The configuration property name for <see cref="IsDebugOutputEnabled"/>.
    /// </summary>
    private const string EnableDebugOutput = "COMPUTESHARP_ENABLE_DEBUG_OUTPUT";

    /// <summary>
    /// The configuration property name for <see cref="IsDeviceRemovedExtendedDataEnabled"/>.
    /// </summary>
    private const string EnableDeviceRemovedExtendedDataInfo = "COMPUTESHARP_ENABLE_DEVICE_REMOVED_EXTENDED_DATA";

    /// <summary>
    /// Indicates whether or not the debug output is enabled.
    /// </summary>
    public static readonly bool IsDebugOutputEnabled = GetConfigurationValue(EnableDebugOutput);

    /// <summary>
    /// Indicates whether or not the debug output is enabled.
    /// </summary>
    public static readonly bool IsDeviceRemovedExtendedDataEnabled = GetConfigurationValue(EnableDeviceRemovedExtendedDataInfo);

    /// <summary>
    /// Gets a configuration value for a specified property.
    /// </summary>
    /// <param name="propertyName">The property name to retrieve the value for.</param>
    /// <returns>The value of the specified configuration setting.</returns>
    private static bool GetConfigurationValue(string propertyName)
    {
#if DEBUG
        if (Debugger.IsAttached)
        {
            return true;
        }
#endif

        if (AppContext.TryGetSwitch(propertyName, out bool isEnabled))
        {
            return isEnabled;
        }

        return false;
    }
}