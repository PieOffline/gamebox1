using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace GameBox.Utils
{
    /// <summary>
    /// Manages live updates and beta installation for the application
    /// </summary>
    public static class UpdateManager
    {
        /// <summary>
        /// Pull latest changes from the specified branch and restart the application
        /// </summary>
        public static async Task<UpdateResult> PullAndRestartAsync(string branch = "main")
        {
            try
            {
                // First, check if we're in a git repository
                if (!IsGitRepository())
                {
                    return new UpdateResult
                    {
                        Success = false,
                        Message = "This application is not running from a git repository.\nLive updates are only available for development builds."
                    };
                }

                // Check for uncommitted changes
                var hasChanges = await HasUncommittedChangesAsync();
                if (hasChanges)
                {
                    return new UpdateResult
                    {
                        Success = false,
                        Message = "There are uncommitted changes in the repository.\nPlease commit or stash your changes before updating."
                    };
                }

                // Fetch latest changes
                var fetchResult = await RunGitCommandAsync("fetch origin");
                if (!fetchResult.Success)
                {
                    return new UpdateResult
                    {
                        Success = false,
                        Message = $"Failed to fetch from remote:\n{fetchResult.Output}"
                    };
                }

                // Switch to the specified branch if needed
                var currentBranch = await GetCurrentBranchAsync();
                if (currentBranch != branch)
                {
                    var checkoutResult = await RunGitCommandAsync($"checkout {branch}");
                    if (!checkoutResult.Success)
                    {
                        return new UpdateResult
                        {
                            Success = false,
                            Message = $"Failed to switch to branch '{branch}':\n{checkoutResult.Output}"
                        };
                    }
                }

                // Pull latest changes
                var pullResult = await RunGitCommandAsync($"pull origin {branch}");
                if (!pullResult.Success)
                {
                    return new UpdateResult
                    {
                        Success = false,
                        Message = $"Failed to pull changes:\n{pullResult.Output}"
                    };
                }

                // Check if there were any changes
                if (pullResult.Output.Contains("Already up to date"))
                {
                    return new UpdateResult
                    {
                        Success = true,
                        Message = "Application is already up to date.",
                        RequiresRestart = false
                    };
                }

                // Build the application
                var buildResult = await RunDotnetCommandAsync("build --configuration Release");
                if (!buildResult.Success)
                {
                    return new UpdateResult
                    {
                        Success = false,
                        Message = $"Failed to build the application:\n{buildResult.Output}"
                    };
                }

                return new UpdateResult
                {
                    Success = true,
                    Message = $"Successfully updated to the latest version from '{branch}' branch.\nThe application will now restart.",
                    RequiresRestart = true
                };
            }
            catch (Exception ex)
            {
                return new UpdateResult
                {
                    Success = false,
                    Message = $"An error occurred during update:\n{ex.Message}"
                };
            }
        }

        /// <summary>
        /// Restart the application
        /// </summary>
        public static void RestartApplication()
        {
            try
            {
                // Get the current executable path (using Environment.ProcessPath for .NET 5+)
                var exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath))
                {
                    // Fallback to MainModule for older .NET versions
                    try
                    {
                        exePath = Process.GetCurrentProcess().MainModule?.FileName;
                    }
                    catch (Exception)
                    {
                        // Ignore security exceptions
                    }
                }

                if (string.IsNullOrEmpty(exePath))
                {
                    MessageBox.Show("Unable to determine application path for restart.", "Restart Failed",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Start a new instance
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                });

                // Close current instance
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to restart application:\n{ex.Message}", "Restart Failed",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static bool IsGitRepository()
        {
            try
            {
                var gitDir = Path.Combine(Directory.GetCurrentDirectory(), ".git");
                return Directory.Exists(gitDir) || File.Exists(gitDir);
            }
            catch
            {
                return false;
            }
        }

        private static async Task<bool> HasUncommittedChangesAsync()
        {
            var result = await RunGitCommandAsync("status --porcelain");
            return !string.IsNullOrWhiteSpace(result.Output);
        }

        private static async Task<string> GetCurrentBranchAsync()
        {
            var result = await RunGitCommandAsync("rev-parse --abbrev-ref HEAD");
            return result.Output.Trim();
        }

        private static async Task<CommandResult> RunGitCommandAsync(string arguments)
        {
            return await RunCommandAsync("git", arguments);
        }

        private static async Task<CommandResult> RunDotnetCommandAsync(string arguments)
        {
            return await RunCommandAsync("dotnet", arguments);
        }

        private static async Task<CommandResult> RunCommandAsync(string command, string arguments)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Directory.GetCurrentDirectory()
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                // Wait for process with timeout to prevent hanging
                var timeout = TimeSpan.FromSeconds(120);
                if (!process.WaitForExit((int)timeout.TotalMilliseconds))
                {
                    process.Kill();
                    return new CommandResult
                    {
                        Success = false,
                        Output = $"Command timed out after {timeout.TotalSeconds} seconds"
                    };
                }

                return new CommandResult
                {
                    Success = process.ExitCode == 0,
                    Output = string.IsNullOrEmpty(error) ? output : $"{output}\n{error}"
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Success = false,
                    Output = ex.Message
                };
            }
        }

        private class CommandResult
        {
            public bool Success { get; set; }
            public string Output { get; set; } = "";
        }
    }

    public class UpdateResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public bool RequiresRestart { get; set; } = true;
    }
}
