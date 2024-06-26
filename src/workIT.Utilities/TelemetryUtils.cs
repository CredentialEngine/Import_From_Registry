using System;
using System.Diagnostics;
using System.IO;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.VisualBasic.Logging;

namespace workIT.Utilities
{
    public class TelemetryUtils
    {
        public static void InitializeTelemetry(string roleName)
        {
            TelemetryConfiguration.Active.TelemetryInitializers.Add(new ServiceNameInitializer(roleName));
        }

        public static TraceListener GetFileLogListener(string baseFileName)
        {
            return new FileLogTraceListener()
            {
                MaxFileSize = 1 * 1024 * 1024 * 1024,
                Append = true,
                DiskSpaceExhaustedBehavior = DiskSpaceExhaustedOption.DiscardMessages,
                AutoFlush = true,
                LogFileCreationSchedule = LogFileCreationScheduleOption.Daily,
                Location = LogFileLocation.TempDirectory,
                BaseFileName = baseFileName,
                Delimiter = ",",
                TraceOutputOptions = TraceOptions.DateTime
                    | TraceOptions.ProcessId
                    | TraceOptions.ThreadId
            };
        }

        internal class ServiceNameInitializer : ITelemetryInitializer
        {
            private readonly string roleName;

            public ServiceNameInitializer(string roleName) 
            {
                this.roleName = roleName;
            }

            public void Initialize(ITelemetry telemetry)
            {
                telemetry.Context.Cloud.RoleName = this.roleName;
                // RoleInstance property modifies the app name in the telmetry dashboard
                telemetry.Context.Cloud.RoleInstance = Environment.MachineName;
                telemetry.Context.GlobalProperties["appName"] = this.roleName;
            }
        }
    }
}
