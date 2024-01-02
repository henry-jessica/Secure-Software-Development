using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking_Application
{
    public sealed class CpuUsageChecker
    {
        private PerformanceCounter cpuCounter;

        public CpuUsageChecker()
        {
            // Initialize the CPU counter
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        }

        public float GetCpuUsage()
        {
            // Get the current CPU usage
            float cpuUsage = cpuCounter.NextValue();
            System.Threading.Thread.Sleep(1000);
            // Now matches task manager reading
            cpuUsage = cpuCounter.NextValue();
            return cpuUsage;
        }

    }
}
