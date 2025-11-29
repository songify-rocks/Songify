using System;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace Songify_Slim.Util.General;

public static class PipeMessenger
{
    // Must match your server's pipe name
    public const string PipeName = "SongifyPipe";

    /// <summary>
    /// Sends one line message to the main instance.
    /// Returns true on success (server received/EOF), false otherwise.
    /// </summary>
    public static bool SendToExistingInstance(string message, int timeoutMs = 2000)
    {
        try
        {
            using NamedPipeClientStream client = new(".", PipeName, PipeDirection.Out);
            client.Connect(timeoutMs); // wait for server

            // IMPORTANT: Use WriteLine + Flush so server's ReadLine() completes
            using StreamWriter writer = new(client, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
            {
                AutoFlush = true
            };
            writer.WriteLine(message);
            // Client disposal closes the pipe → server continues after ReadLine
            return true;
        }
        catch (TimeoutException)
        {
            // No server (app not running)
            return false;
        }
        catch (IOException)
        {
            // Broken pipe / server died mid-send
            return false;
        }
        catch
        {
            return false;
        }
    }
}