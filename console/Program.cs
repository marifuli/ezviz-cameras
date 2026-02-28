using Hik.Api;
using Newtonsoft.Json;
using System;
using System.Linq;  // Add this for Skip()
using System.IO;    // Add this for file operations
using System.Threading; // Add this for Thread.Sleep
using System.Diagnostics;


namespace MyDotNetService
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
                    case "greet":
                        return HandleGreet(filteredArgs);
                    
                    case "calculate":
                        return HandleCalculate(filteredArgs);
                    
                    default:
                        Console.WriteLine($"Unknown command: {command}");
                        return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing command: {ex.Message}");
                return 1;
            }
        }

        // Rest of your command handlers remain the same...
        static int HandleGreet(string[] args)
        {
            // Default values
            string name = "World";
            int times = 1;
            bool shout = false;

            // Parse flags (starting from index 1 since index 0 is the command)
            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--name":
                    case "-n":
                        if (i + 1 < args.Length) name = args[++i];
                        break;
                    
                    case "--times":
                    case "-t":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int t))
                            times = t;
                        break;
                    
                    case "--shout":
                    case "-s":
                        shout = true;
                        break;
                    
                    case "--help":
                    case "-h":
                        Console.WriteLine("greet command options:");
                        Console.WriteLine("  --name, -n <name>  : Name to greet (default: World)");
                        Console.WriteLine("  --times, -t <num>  : Number of times to greet (default: 1)");
                        Console.WriteLine("  --shout, -s        : Shout the greeting in uppercase");
                        return 0;
                }
            }

            // Perform the greeting
            for (int i = 0; i < times; i++)
            {
                string greeting = $"Hello, {name}!";
                if (shout)
                    greeting = greeting.ToUpper();
                
                Console.WriteLine(greeting);
                
                // Simulate some work
                System.Threading.Thread.Sleep(500);
            }
            return 0;
        }

        // Add the other handlers here (calculate, file, monitor)...
        static int HandleCalculate(string[] args)
        {
           string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            HikApi.SetLibraryPath(currentDirectory);
            Console.WriteLine(currentDirectory);

            HikApi.Initialize(
                logLevel: 3, 
                logDirectory: "logs", 
                autoDeleteLogs: true,
                waitTimeMilliseconds: 5000, // Increase timeout for better reliability
                forceReinitialization: true // Force reinitialization to ensure clean state
            );

            // // Please update IP Address, port and user credentials
            var hikApi = HikApi.Login("138.252.14.97", 8010, "admin", "XWBDVR");
            Console.WriteLine("Login success");
            var device = hikApi.ConfigService.GetDeviceConfig();
            Console.WriteLine(JsonConvert.SerializeObject(device, Formatting.Indented));
            return 0;
        }

        // ... add file and monitor handlers here
    }
}