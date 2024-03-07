// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using SharpHook;
using TextCopy;

var settingsFilePath = "settings.json";
if (!File.Exists(settingsFilePath))
{
    File.WriteAllText(
        settingsFilePath + ".example",
        JsonSerializer.Serialize(new Settings(), new JsonSerializerOptions { WriteIndented = true })
    );
    throw new FileNotFoundException("Settings file not found, example file created.");
}

var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText("settings.json"));

var hook = new TaskPoolGlobalHook();

var shiftDown = false;
var cmdDown = false;


hook.KeyPressed += (a, b) =>
{
    var key = b.RawEvent.Keyboard.RawCode;
    if (key == 56)
    {
        shiftDown = true;
    }
    if (key == 55)
    {
        cmdDown = true;
    }

    if (shiftDown && cmdDown)
    {
        if (key == 18)
        {
            UploadClipboard();
        }
    }
};

hook.KeyReleased += (a, b) =>
{
    var key = b.RawEvent.Keyboard.RawCode;

    if (key == 56)
    {
        shiftDown = false;
    }
    if (key == 55)
    {
        cmdDown = false;
    }
};

hook.Run();


void UploadClipboard()
{
    Console.WriteLine("Uploading clipboard...");

    var executabledPath = Path.Combine(Environment.CurrentDirectory, "External", "MacClipboardUpload");

    var arguments = @$"-server={settings!.FtpServer} -username={settings.Username} -password={settings.Password} -webServer={settings.UploadUrl} -port={settings.Port}";

    RunCliTool(executabledPath, arguments);
    ShowNotification("File uploaded", ClipboardService.GetText()!);
    PlayNotificationSound(" /System/Library/Sounds/Glass.aiff");
}


string RunCliTool(string fileName, string arguments)
    {
        // Initialize the process start info
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName, // The path to the executable to run
            Arguments = arguments, // Command line arguments to pass
            UseShellExecute = false, // Do not use the shell to start the process
            RedirectStandardOutput = true, // Redirect output so we can read it
            RedirectStandardError = true, // Redirect error output as well
            CreateNoWindow = true // Do not create a window for the process
        };

        // Initialize a new process
        var process = new Process
        {
            StartInfo = startInfo
        };

        // StringBuilder to capture output
        var output = new StringBuilder();

        try
        {
            // Start the process
            process.Start();

            // Read the output stream first and then wait.
            output.Append(process.StandardOutput.ReadToEnd());
            output.Append(process.StandardError.ReadToEnd());

            System.Console.WriteLine(output.ToString());

            process.WaitForExit(); // Wait for the process to exit
        }
        catch (Exception ex)
        {
            // Handle any exceptions that occur during process execution
            output.Append("Error running CLI tool: " + ex.Message);
        }

        // Return the output from the CLI tool
        return output.ToString();
    }


void ShowNotification(string title, string message)
{
    // Escape double quotes in title and message
    title = title.Replace("\"", "\\\"");
    message = message.Replace("\"", "\\\"");

    // AppleScript to show notification
    var appleScript = $"display notification \\\"{message}\\\" with title \\\"{title}\\\"";

    // Prepare the osascript command
    var startInfo = new ProcessStartInfo
    {
        FileName = "/bin/bash",
        Arguments = $"-c \"osascript -e '{appleScript}'\"",
        UseShellExecute = false,
        CreateNoWindow = true,
    };

    // Execute the command
    using (var process = Process.Start(startInfo))
    {
        process!.WaitForExit();
    }
}

void PlayNotificationSound(string soundFilePath)
{
    try
    {
        using (var process = new Process())
        {
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "afplay",
                Arguments = soundFilePath,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            process.Start();
            process.WaitForExit(); // Wait for the sound to finish playing
        }
    }
    catch (Exception ex)
    {
        // Handle exceptions (e.g., file not found, afplay not available)
        Console.WriteLine($"Error playing sound: {ex.Message}");
    }
}