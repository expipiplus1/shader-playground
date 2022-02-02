using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ShaderPlayground.Core.Util
{
    internal static class ProcessHelper
    {
        public static bool Run(string fileName, string arguments, out string stdOutput, out string stdError, Encoding textEncoding = null)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = Path.GetDirectoryName(fileName),
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            if (textEncoding != null)
            {
                processStartInfo.StandardOutputEncoding = textEncoding;
                processStartInfo.StandardErrorEncoding = textEncoding;
            }

            using (var process = Process.Start(processStartInfo))
            {
                var stdOutputTemp = string.Empty;
                process.OutputDataReceived += (sender, e) =>
                {
                    stdOutputTemp += e.Data + Environment.NewLine;
                };

                process.BeginOutputReadLine();

                stdError = process.StandardError.ReadToEnd();

                if (!process.WaitForExit(4000))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (InvalidOperationException)
                    {
                        // Process exited between calls to WaitForExit and Kill.
                    }
                }
                else
                {
                    // From the WaitForExit(timeout) docs:
                    // "To ensure that asynchronous event handling has been completed, 
                    // call the WaitForExit() overload that takes no parameter 
                    // after receiving a true from this overload."
                    process.WaitForExit();
                }

                stdOutput = stdOutputTemp;

                return process.ExitCode == 0;
            }
        }
    }
}
