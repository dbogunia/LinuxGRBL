using System.Runtime.InteropServices;
using System.Text;
using LaserGRBL.Platform.Contracts;
using LaserGRBL.Platform.Implementations;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class PtySerialHarnessTests
{
    [Fact]
    public async Task System_serial_connection_writes_to_unix_pseudo_terminal()
    {
        using var pty = UnixPty.Open();
        var port = new SerialPortDescriptor(pty.SlaveName, Path.GetFileName(pty.SlaveName), pty.SlaveName);
        await using var connection = new SystemSerialConnection(port, new SerialPortOptions(ReadTimeout: TimeSpan.FromMilliseconds(250)));

        await connection.OpenAsync();
        await connection.WriteAsync("G0 X1");

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var buffer = new byte[64];
        var read = await pty.Master.ReadAsync(buffer, timeout.Token);

        Assert.Contains("G0 X1", Encoding.ASCII.GetString(buffer, 0, read));
        Assert.True(connection.IsOpen);
    }

    private sealed class UnixPty : IDisposable
    {
        private UnixPty(FileStream master, string slaveName)
        {
            Master = master;
            SlaveName = slaveName;
        }

        public FileStream Master { get; }
        public string SlaveName { get; }

        public static UnixPty Open()
        {
            var name = new byte[256];
            if (openpty(out var masterFd, out var slaveFd, name, IntPtr.Zero, IntPtr.Zero) != 0)
                throw new IOException($"openpty failed with errno {Marshal.GetLastPInvokeError()}.");

            close(slaveFd);
            var length = Array.IndexOf(name, (byte)0);
            var slaveName = Encoding.UTF8.GetString(name, 0, length < 0 ? name.Length : length);
            var master = new FileStream(new SafeFileHandle(new IntPtr(masterFd), ownsHandle: true), FileAccess.ReadWrite);
            return new UnixPty(master, slaveName);
        }

        public void Dispose() => Master.Dispose();

        [DllImport("libc", SetLastError = true)]
        private static extern int openpty(out int amaster, out int aslave, byte[] name, IntPtr termp, IntPtr winp);

        [DllImport("libc", SetLastError = true)]
        private static extern int close(int fd);
    }
}
