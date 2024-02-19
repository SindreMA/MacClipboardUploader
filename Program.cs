﻿// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using SharpHook;

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


hook.KeyPressed += async (a, b) =>
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
            await UploadClipboard();
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


async Task UploadClipboard()
{
    Console.WriteLine("Uploading clipboard...");

    var executabledPath = Path.Combine(Environment.CurrentDirectory, "External", "MacClipboardUpload");

    var arguments = @$"-server={settings.FtpServer} -username={settings.Username} -password={settings.Password} -webServer={settings.UploadUrl}";

    RunCliTool(executabledPath, arguments);
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