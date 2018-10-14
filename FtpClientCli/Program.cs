#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FtpClientBase;

#endregion

namespace FtpClientCli
{
    internal class Program
    {
        private const string HelpMessage = @"
Welcome to the YGGUB ftp client CLI!

You can use following commands:

- help                  Display this message.
- exit                  Exit this program.

- open                  Open a connection to a ftp server and login.
- close                 Close the connection to a ftp server.

- active                Set mode to active mode.
- passive               Set mode to passive mode.

- pwd                   Print current working directory.
- cd <pathname>         Change current working directory to `pathname`.
- ls [pathname]         List files and folders in `pathname`.
- mkdir <pathname>      Make a new folder named `pathname`.
- rmdir <pathname>      Remove an empty folder named `pathname`.

- get <remote_filepath> <local_filepath>
                        Download file `remote_filepath` to `local_filepath`.
- put <local_filepath> <remote_filepath>
                        Upload file `local_filepath` to `remote_filepath`.
";

        public static void Main(string[] args)
        {
            Console.WriteLine(HelpMessage);
            const string prompt = "> ";
            Connection globalConnection = null;

            while (true)
            {
                if (globalConnection != null && globalConnection.Connected)
                    Console.Write(globalConnection.Performer.GetCurrentDirectory() + " ");
                Console.Write(prompt);

                var command = Console.ReadLine();
                while (command.Split(new[] {" ", "\t"}, StringSplitOptions.RemoveEmptyEntries).Length == 0)
                    command = Console.ReadLine();

                var commandType = command.Split(new[] {" ", "\t"}, StringSplitOptions.RemoveEmptyEntries)[0];

                switch (commandType)
                {
                    case "help":
                        if (command.Trim() != "help") goto default;
                        Console.WriteLine(HelpMessage);
                        break;
                    case "exit":
                        if (command.Trim() != "exit") goto default;
                        if (globalConnection == null || !globalConnection.Connected) return;
                        try
                        {
                            globalConnection.Destory();
                        }
                        catch (Exception)
                        {
                            // ignored
                        }

                        return;
                    case "open":
                        if (command.Trim() != "open") goto default;
                        if (globalConnection != null && globalConnection.Connected)
                        {
                            Console.WriteLine("Already connected to another server, please close it first.");
                            break;
                        }
                        else
                        {
                            try
                            {
                                Console.Write("Hostname of the server: ");
                                var hostname = Console.ReadLine();
                                Console.Write("Port of the service (typically 21): ");
                                var port = int.Parse(Console.ReadLine());
                                Console.Write("Username: ");
                                var username = Console.ReadLine();
                                Console.Write("Password: ");

                                var origBg = Console.BackgroundColor;
                                var origFg = Console.ForegroundColor;
                                Console.BackgroundColor = ConsoleColor.Gray;
                                Console.ForegroundColor = ConsoleColor.Gray;
                                var password = Console.ReadLine();
                                Console.BackgroundColor = origBg;
                                Console.ForegroundColor = origFg;

                                globalConnection = new Connection(hostname, port);
                                globalConnection.Establish(username, password);

                                Console.WriteLine("Succussfully logged in!");
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                                Console.WriteLine("There may be something wrong with your information.");
                                Console.WriteLine("You can try again later.");
                            }

                            break;
                        }
                    case "close":
                    case "pwd":
                    case "cd":
                    case "ls":
                    case "mkdir":
                    case "rmdir":
                    case "get":
                    case "put":
                    case "active":
                    case "passive":
                        if (globalConnection == null || !globalConnection.Connected)
                        {
                            Console.WriteLine("There is no active connection currently.");
                            break;
                        }
                        else
                        {
                            try
                            {
                                switch (commandType)
                                {
                                    case "close":
                                        if (command.Trim() != "close") goto default;
                                        globalConnection.Destory();
                                        break;
                                    case "pwd":
                                        if (command.Trim() != "pwd") goto default;
                                        Console.WriteLine(globalConnection.Performer.GetCurrentDirectory());
                                        break;
                                    case "cd":
                                    {
                                        var match = Regex.Match(command, @"\s*cd\s+(.+?)\s*$");
                                        if (!match.Success) goto default;
                                        var pathname = match.Groups[1].Value;
                                        if (pathname.First() == '"' && pathname.Last() == '"' ||
                                            pathname.Count(x => x == ' ' || x == '\t') == 0)
                                        {
                                            globalConnection.Performer.ChangeDirectory(pathname);
                                            break;
                                        }

                                        goto default;
                                    }
                                    case "ls":
                                    {
                                        List<(bool IsDir, long Size, string LastModificationTime, string Name)>
                                            fileList;
                                        if (command.Trim() == "ls")
                                        {
                                            fileList = globalConnection.Performer.ListFiles();
                                        }
                                        else
                                        {
                                            var match = Regex.Match(command, @"\s*ls\s+(.+?)\s*$");
                                            if (!match.Success) goto default;
                                            var pathname = match.Groups[1].Value;
                                            if (pathname.First() == '"' && pathname.Last() == '"' ||
                                                pathname.Count(x => x == ' ' || x == '\t') == 0)
                                                fileList = globalConnection.Performer.ListFiles(pathname);
                                            else goto default;
                                        }

                                        // display
                                        var originalFg = Console.ForegroundColor;
                                        foreach (var (isDir, size, lastModificationTime, name) in fileList)
                                        {
                                            Console.Write(isDir ? "Dir  " : "File ");

                                            if (isDir) Console.ForegroundColor = ConsoleColor.Yellow;
                                            Console.Write(name.PadRight(32).Substring(0, 32) + " ");
                                            Console.ForegroundColor = originalFg;

                                            Console.Write(lastModificationTime.PadRight(20).Substring(0, 20) + " ");
                                            Console.Write(isDir ? "" : $"{size} Bytes");
                                            Console.WriteLine();
                                        }

                                        break;
                                    }
                                    case "mkdir":
                                    {
                                        var match = Regex.Match(command, @"\s*mkdir\s+(.+?)\s*$");
                                        if (!match.Success) goto default;
                                        var pathname = match.Groups[1].Value;
                                        if (pathname.First() == '"' && pathname.Last() == '"' ||
                                            pathname.Count(x => x == ' ' || x == '\t') == 0)
                                            globalConnection.Performer.MakeDirectory(pathname);
                                        else goto default;
                                        break;
                                    }
                                    case "rmdir":
                                    {
                                        var match = Regex.Match(command, @"\s*rmdir\s+(.+?)\s*$");
                                        if (!match.Success) goto default;
                                        var pathname = match.Groups[1].Value;
                                        if (pathname.First() == '"' && pathname.Last() == '"' ||
                                            pathname.Count(x => x == ' ' || x == '\t') == 0)
                                            globalConnection.Performer.RemoveDirectory(pathname);
                                        else goto default;
                                        break;
                                    }
                                    case "get":
                                    {
                                        var match = Regex.Match(command, @"\s*get\s+(.+?)\s*$");
                                        if (!match.Success) goto default;

                                        var parameter = match.Groups[1].Value;
                                        if (Regex.IsMatch(parameter, @"([^""].+?)\s(.+?)$"))
                                        {
                                            match = Regex.Match(parameter, @"([^""].+?)\s(.+?)$");
                                            globalConnection.Performer.DownloadFile(
                                                match.Groups[1].Value,
                                                match.Groups[2].Value.Trim('"')
                                            );
                                        }
                                        else if (Regex.IsMatch(parameter, @"("".+?"")\s(.+?)$"))
                                        {
                                            match = Regex.Match(parameter, @"("".+?"")\s(.+?)$");
                                            globalConnection.Performer.DownloadFile(
                                                match.Groups[1].Value,
                                                match.Groups[2].Value.Trim('"')
                                            );
                                        }

                                        break;
                                    }
                                    case "put":
                                    {
                                        var match = Regex.Match(command, @"\s*put\s+(.+?)\s*$");
                                        if (!match.Success) goto default;

                                        var parameter = match.Groups[1].Value;
                                        if (Regex.IsMatch(parameter, @"([^""].+?)\s(.+?)$"))
                                        {
                                            match = Regex.Match(parameter, @"([^""].+?)\s(.+?)$");
                                            globalConnection.Performer.UploadFile(
                                                match.Groups[2].Value,
                                                match.Groups[1].Value
                                            );
                                        }
                                        else if (Regex.IsMatch(parameter, @"("".+?"")\s(.+?)$"))
                                        {
                                            match = Regex.Match(parameter, @"("".+?"")\s(.+?)$");
                                            globalConnection.Performer.UploadFile(
                                                match.Groups[2].Value,
                                                match.Groups[1].Value.Trim('"')
                                            );
                                        }

                                        break;
                                    }
                                    case "active":
                                        globalConnection.Performer.ActiveMode = true;
                                        break;
                                    case "passive":
                                        globalConnection.Performer.ActiveMode = false;
                                        break;
                                    default:
                                        Console.WriteLine("Unknown command or syntax!");
                                        break;
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }

                            break;
                        }

                    default:
                        Console.WriteLine("Unknown command or syntax!");
                        break;
                }
            }
        }
    }
}