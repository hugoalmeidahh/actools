﻿using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using JetBrains.Annotations;

namespace AcTools.WheelAngles.Implementations {
    [UsedImplicitly]
    internal abstract class LogitechG923 : LogitechG25 {
        public override string ControllerName => "Logitech G923";

        public override bool Test(string productGuid) {
            return string.Equals(productGuid, "C266046D-0000-0000-0000-504944564944", StringComparison.OrdinalIgnoreCase);
        }

        protected override string GetRegistryPath() {
            // TODO: Check
            return @"Software\Logitech\Gaming Software\GlobalDeviceSettings\G923";
        }

        public override WheelOptionsBase GetOptions() {
            return null;
        }

        public override bool Apply(int steerLock, bool isReset, out int appliedValue) {
            if (!LoadLogitechSteeringWheelDll()) {
                appliedValue = 0;
                return false;
            }

            appliedValue = Math.Min(Math.Max(steerLock, MinimumSteerLock), MaximumSteerLock);
            return SetValue(appliedValue);
        }

        private static IntPtr GetHandle() {
            return (Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher).Invoke(() => {
                var mainWindow = Application.Current?.MainWindow;
                return mainWindow == null ? IntPtr.Zero : new WindowInteropHelper(mainWindow).Handle;
            });
        }

        private static bool SetValue(int value) {
            var handle = GetHandle();
            if (handle == IntPtr.Zero) {
                AcToolsLogging.Write("Main window not found, cancel");
                return false;
            }

            Initialize(handle);
            for (var i = 0; i < 4; i++) {
                var result = LogiSetOperatingRange(i, value);
                AcToolsLogging.Write($"Set operating range for #{i}: {result}");
            }

            Apply();
            return true;
        }
    }
}