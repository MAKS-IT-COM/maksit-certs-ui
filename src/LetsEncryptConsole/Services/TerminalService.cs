using System.Diagnostics;

namespace MaksIT.LetsEncryptConsole.Services;

public interface ITerminalService {
    void Exec(string cmd);
}

public class TerminalService : ITerminalService {
        
    public void Exec(string cmd) {
        var escapedArgs = cmd.Replace("\"", "\\\"");

        var pc = new Process {
            StartInfo = new ProcessStartInfo {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "/bin/bash",
                Arguments = $"-c \"{escapedArgs}\""
            }
        };

        pc.Start();
        pc.WaitForExit();
    }
}
