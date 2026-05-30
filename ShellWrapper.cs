// ============================================================
// File: ShellWrapper.cs
// Project: OpticCli
// Namespace: OpticCli
// Description: Runs PowerShell commands in a hidden process and
//              returns the output. Cleans up CLIXML error format
//              and handles progress stream noise from PowerShell.
// ============================================================

using System;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpticCli
{
    public class ShellWrapper
    {
        public async Task<string> ExecuteCommandAsync(string command)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Silence PowerShell's progress stream to avoid polluting the error output
                    string safeCommand = $"$ProgressPreference = 'SilentlyContinue'; {command}";

                    var plainTextBytes = System.Text.Encoding.Unicode.GetBytes(safeCommand);
                    string encodedCommand = Convert.ToBase64String(plainTextBytes);

                    ProcessStartInfo processInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -EncodedCommand {encodedCommand}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = System.Environment.GetFolderPath(
                            System.Environment.SpecialFolder.UserProfile)
                    };

                    using (Process process = new Process())
                    {
                        process.StartInfo = processInfo;
                        process.Start();

                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        process.WaitForExit();

                        if (!string.IsNullOrEmpty(error))
                        {
                            if (error.Contains("#< CLIXML"))
                            {
                                // Extract ONLY the actual error text inside the <S S="Error"> tags
                                var errorMatches = Regex.Matches(error, @"<S S=""Error"">(.*?)</S>", RegexOptions.Singleline);

                                if (errorMatches.Count > 0)
                                {
                                    string cleanError = "";
                                    foreach (Match match in errorMatches)
                                    {
                                        cleanError += match.Groups[1].Value;
                                    }

                                    error = WebUtility.HtmlDecode(cleanError);
                                    error = error.Replace("_x000D__x000A_", "\n")
                                                 .Replace("_x000D_", "")
                                                 .Replace("_x000A_", "\n");
                                }
                                else
                                {
                                    error = string.Empty;
                                }
                            }

                            error = error.Trim();

                            if (!string.IsNullOrEmpty(error))
                                return $"[ERROR]\n{error}";
                        }

                        return string.IsNullOrEmpty(output)
                            ? "[Command Executed Successfully - No Output Returned]"
                            : output.Trim();
                    }
                }
                catch (Exception ex)
                {
                    return $"[SYSTEM EXCEPTION]\n{ex.Message}";
                }
            });
        }
    }
}