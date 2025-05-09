﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Provides the ability to sample the system for current resource utilization.
/// </summary>
[Obsolete(DiagnosticIds.Obsoletions.NonObservableResourceMonitoringApiMessage,
    DiagnosticId = DiagnosticIds.Obsoletions.NonObservableResourceMonitoringApiDiagId,
    UrlFormat = DiagnosticIds.UrlFormat)]
public interface IResourceMonitor
{
    /// <summary>
    /// Gets utilization for the specified time window.
    /// </summary>
    /// <param name="window">A <see cref="TimeSpan"/> representing the time window for which utilization is requested.</param>
    /// <returns>The utilization during the time window specified by <paramref name="window"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="window"/> is greater than the maximum window size
    /// configured while adding the service to the service collection.
    /// </exception>
    ResourceUtilization GetUtilization(TimeSpan window);
}
