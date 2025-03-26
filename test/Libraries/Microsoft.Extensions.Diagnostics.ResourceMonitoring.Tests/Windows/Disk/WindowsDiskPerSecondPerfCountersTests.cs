﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Versioning;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Test;
using Microsoft.Extensions.Time.Testing;
using Microsoft.TestUtilities;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Disk.Test;

[SupportedOSPlatform("windows")]
[OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX, SkipReason = "Windows specific.")]
public class WindowsDiskPerSecondPerfCountersTests
{
    private const string CategoryName = "LogicalDisk";

    [ConditionalFact]
    public void DiskReadsPerfCounter_Per60Seconds()
    {
        const string CounterName = WindowsDiskPerfCounterNames.DiskReadsCounter;
        var performanceCounterFactory = new Mock<IPerformanceCounterFactory>();
        var fakeTimeProvider = new FakeTimeProvider { AutoAdvanceAmount = TimeSpan.FromSeconds(60) };

        var perSecondPerfCounters = new WindowsDiskPerSecondPerfCounters(
            performanceCounterFactory.Object,
            fakeTimeProvider,
            CategoryName,
            CounterName,
            instanceNames: ["C:", "D:", "_Total"]);

        // Set up
        var counterC = new FakePerformanceCounter("C:", [0, 1, 1.5f, 2, 2.5f]);
        var counterD = new FakePerformanceCounter("D:", [0, 2, 2.5f, 3, 3.5f]);
        performanceCounterFactory.Setup(x => x.Create(CategoryName, CounterName, "C:")).Returns(counterC);
        performanceCounterFactory.Setup(x => x.Create(CategoryName, CounterName, "D:")).Returns(counterD);

        // Initialize the counters
        perSecondPerfCounters.InitializeDiskCounters();
        Assert.Equal(2, perSecondPerfCounters.TotalCountDict.Count);
        Assert.Equal(0, perSecondPerfCounters.TotalCountDict["C:"]);
        Assert.Equal(0, perSecondPerfCounters.TotalCountDict["D:"]);

        // Simulate the first tick
        perSecondPerfCounters.UpdateDiskCounters();
        Assert.Equal(60, perSecondPerfCounters.TotalCountDict["C:"]); // 1 * 60 = 60
        Assert.Equal(120, perSecondPerfCounters.TotalCountDict["D:"]); // 2 * 60 = 120

        // Simulate the second tick
        perSecondPerfCounters.UpdateDiskCounters();
        Assert.Equal(150, perSecondPerfCounters.TotalCountDict["C:"]); // 60 + 1.5 * 60 = 150
        Assert.Equal(270, perSecondPerfCounters.TotalCountDict["D:"]); // 120 + 2.5 * 60 = 270

        // Simulate the third tick
        perSecondPerfCounters.UpdateDiskCounters();
        Assert.Equal(270, perSecondPerfCounters.TotalCountDict["C:"]); // 150 + 2 * 60 = 270
        Assert.Equal(450, perSecondPerfCounters.TotalCountDict["D:"]); // 270 + 3 * 60 = 450

        // Simulate the fourth tick
        perSecondPerfCounters.UpdateDiskCounters();
        Assert.Equal(420, perSecondPerfCounters.TotalCountDict["C:"]); // 270 + 2.5 * 60 = 420
        Assert.Equal(660, perSecondPerfCounters.TotalCountDict["D:"]); // 450 + 3.5 * 60 = 660
    }

    [ConditionalFact]
    public void DiskWriteBytesPerfCounter_Per30Seconds()
    {
        const string CounterName = WindowsDiskPerfCounterNames.DiskWriteBytesCounter;
        var performanceCounterFactory = new Mock<IPerformanceCounterFactory>();
        var fakeTimeProvider = new FakeTimeProvider { AutoAdvanceAmount = TimeSpan.FromSeconds(30) };
        var perSecondPerfCounters = new WindowsDiskPerSecondPerfCounters(
            performanceCounterFactory.Object,
            fakeTimeProvider,
            CategoryName,
            counterName: CounterName,
            instanceNames: ["C:", "D:", "_Total"]);

        // Set up
        var counterC = new FakePerformanceCounter("C:", [0, 100, 150.5f, 20, 3.1416f]);
        var counterD = new FakePerformanceCounter("D:", [0, 2000, 2025, 0, 2.7183f]);
        performanceCounterFactory.Setup(x => x.Create(CategoryName, CounterName, "C:")).Returns(counterC);
        performanceCounterFactory.Setup(x => x.Create(CategoryName, CounterName, "D:")).Returns(counterD);

        // Initialize the counters
        perSecondPerfCounters.InitializeDiskCounters();
        Assert.Equal(2, perSecondPerfCounters.TotalCountDict.Count);
        Assert.Equal(0, perSecondPerfCounters.TotalCountDict["C:"]);
        Assert.Equal(0, perSecondPerfCounters.TotalCountDict["D:"]);

        // Simulate the first tick
        perSecondPerfCounters.UpdateDiskCounters();
        Assert.Equal(3000, perSecondPerfCounters.TotalCountDict["C:"]); // 100 * 30 = 3000
        Assert.Equal(60000, perSecondPerfCounters.TotalCountDict["D:"]); // 2000 * 30 = 60000

        // Simulate the second tick
        perSecondPerfCounters.UpdateDiskCounters();
        Assert.Equal(7515, perSecondPerfCounters.TotalCountDict["C:"]); // 3000 + 150.5 * 30 = 7515
        Assert.Equal(120750, perSecondPerfCounters.TotalCountDict["D:"]); // 60000 + 2.5 * 30 = 120750

        // Simulate the third tick
        perSecondPerfCounters.UpdateDiskCounters();
        Assert.Equal(8115, perSecondPerfCounters.TotalCountDict["C:"]); // 7515 + 20 * 30 = 8115
        Assert.Equal(120750, perSecondPerfCounters.TotalCountDict["D:"]); // 120750 + 0 * 30 = 120750

        // Simulate the fourth tick
        perSecondPerfCounters.UpdateDiskCounters();
        Assert.Equal(8209, perSecondPerfCounters.TotalCountDict["C:"]); // 8115 + 3.1416 * 30 = 8209
        Assert.Equal(120831, perSecondPerfCounters.TotalCountDict["D:"]); // 120750 + 3.5 * 30 = 120831
    }
}
