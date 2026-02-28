using Hik.Api;
using Newtonsoft.Json;
using System;
using System.Linq;  // Add this for Skip()
using System.IO;    // Add this for file operations
using System.Threading; // Add this for Thread.Sleep
using System.Diagnostics;
using System.Threading.Tasks;
using ConsoleApp.Services;

namespace ConsoleApp
{
    class Program
    {
        static int processId; 
        static int Main(string[] args)
        {
            processId = Process.GetCurrentProcess().Id;
            // Filter out the '--' if it appears as first argument
            var filteredArgs = args;
            
            // If first arg is "--", remove it
            if (args.Length > 0 && args[0] == "--")
            {
                filteredArgs = args.Skip(1).ToArray();
            }
            
            // Parse the command from first argument
            if (filteredArgs.Length == 0)
            {
                Console.WriteLine("Error: No command specified");
                return 1;
            }

            string command = filteredArgs[0].ToLower();

            try
            {
                switch (command)
                {
                    case "camera":
                        return HandleCamera(filteredArgs).Result;
                    
                    default:
                        Console.WriteLine($"Unknown command: {command}");
                        Console.WriteLine("Available commands: camera");
                        return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing command: {ex.Message}");
                return 1;
            }
        }

        // Camera service handler
        static async Task<int> HandleCamera(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Camera command usage:");
                Console.WriteLine("  camera test <ip> [port] [username] [password]   - Test camera connection");
                Console.WriteLine("  camera list <ip> [port] [username] [password] <start> <end> <savepath>  - Get video file list");
                Console.WriteLine("  camera download <ip> [port] [username] [password] <filename> <savepath>  - Download file");
                return 1;
            }

            string subCommand = args[1].ToLower();

            try
            {
                switch (subCommand)
                {
                    case "test":
                        return await HandleCameraTest(args);

                    case "list":
                        return await HandleCameraList(args);

                    case "download":
                        return await HandleCameraDownload(args);

                    default:
                        Console.WriteLine($"Unknown camera subcommand: {subCommand}");
                        return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Camera command error: {ex.Message}");
                return 1;
            }
        }

        static async Task<int> HandleCameraTest(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: camera test <ip> [port] [username] [password]");
                return 1;
            }

            var credentials = new CameraService.CameraCredentials
            {
                IpAddress = args[2],
                Port = args.Length > 3 && int.TryParse(args[3], out int port) ? port : 8000,
                Username = args.Length > 4 ? args[4] : "admin",
                Password = args.Length > 5 ? args[5] : ""
            };

            var cameraService = new CameraService();
            var result = await cameraService.CheckCameraConnectionAsync(credentials);

            if (result.IsSuccess)
            {
                var jsonResponse = JsonConvert.SerializeObject(result, Formatting.Indented);
                Console.WriteLine($"Result: {jsonResponse}");
                return 0;
            }
            else
            {
                Console.WriteLine($"Error: {result.Message}");
                return 1;
            }
        }

        static async Task<int> HandleCameraList(string[] args)
        {
            if (args.Length < 7)
            {
                Console.WriteLine("Usage: camera list <ip> [port] [username] [password] <start_datetime> <end_datetime> <savepath>");
                Console.WriteLine("Example: camera list 192.168.1.100 8000 admin password123 \"2024-01-01 00:00:00\" \"2024-01-01 23:59:59\" \"video_list.json\"");
                return 1;
            }

            var credentials = new CameraService.CameraCredentials
            {
                IpAddress = args[2],
                Port = args.Length > 3 && int.TryParse(args[3], out int port) ? port : 8000,
                Username = args.Length > 4 ? args[4] : "admin",
                Password = args.Length > 5 ? args[5] : ""
            };

            if (!DateTime.TryParse(args[args.Length - 3], out DateTime startTime))
            {
                Console.WriteLine("Invalid start datetime format");
                return 1;
            }

            if (!DateTime.TryParse(args[args.Length - 2], out DateTime endTime))
            {
                Console.WriteLine("Invalid end datetime format");
                return 1;
            }

            string savePath = args[args.Length - 1];

            var cameraService = new CameraService();
            var result = await cameraService.GetVideoFileListAsync(credentials, startTime, endTime, savePath);

            if (result.IsSuccess)
            {
                var jsonResponse = JsonConvert.SerializeObject(result, Formatting.Indented);
                Console.WriteLine($"Result: {jsonResponse}");
                return 0;
            }
            else
            {
                Console.WriteLine($"Error: {result.Message}");
                return 1;
            }
        }

        static async Task<int> HandleCameraDownload(string[] args)
        {
            if (args.Length < 6)
            {
                Console.WriteLine("Usage: camera download <ip> [port] [username] [password] <filename> <savepath>");
                Console.WriteLine("Example: camera download 192.168.1.100 8000 admin password123 \"video001.h264\" \"downloads/video001.mp4\"");
                return 1;
            }

            var credentials = new CameraService.CameraCredentials
            {
                IpAddress = args[2],
                Port = args.Length > 3 && int.TryParse(args[3], out int port) ? port : 8000,
                Username = args.Length > 4 ? args[4] : "admin",
                Password = args.Length > 5 ? args[5] : ""
            };

            string fileName = args[args.Length - 2];
            string savePath = args[args.Length - 1];

            var cameraService = new CameraService();
            
            Console.WriteLine($"Downloading: {fileName}");
            Console.WriteLine($"Save path: {savePath}");

            var result = await cameraService.DownloadFileAsync(
                credentials,
                fileName,
                savePath,
                progress => Console.WriteLine($"\rProgress: {progress}%")
            );

            Console.WriteLine(); // New line after progress

            if (result.IsSuccess)
            {
                var jsonResponse = JsonConvert.SerializeObject(result, Formatting.Indented);
                Console.WriteLine($"Result: {jsonResponse}");
                return 0;
            }
            else
            {
                Console.WriteLine($"Error: {result.Message}");
                return 1;
            }
        }
    }
}
