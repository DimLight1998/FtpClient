using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace FtpClientBase
{
    public class CommandSender
    {
        private readonly Socket _socket;

        private (int Code, string Response) SendAndGetResponse(string content)
        {
            Debug.Print(content);
            _socket.Send(Encoding.ASCII.GetBytes(content));

            return GetNextResponse();
        }

        public (int Code, string Response) GetNextResponse()
        {
            var buffer = new byte[1024];
            var response = "";

            while (true)
            {
                var size = _socket.Receive(buffer);
                response += Encoding.ASCII.GetString(buffer, 0, size);
                if (IsFullResponse(response)) break;
            }

            Debug.Print(response);
            return (ExtractCode(response), response);
        }

        public static bool IsFullResponse(string response)
        {
            if (!response.EndsWith("\r\n")) return false;
            if (!Regex.IsMatch(response, @"^\d\d\d", RegexOptions.Multiline)) return false;
            var code = response.Substring(0, 3);

            // if the response has only one line
            if (
                Regex.Matches(response, @"\r\n").Count == 1 &&
                Regex.IsMatch(response, @"^\d\d\d .*\r\n$", RegexOptions.Multiline)
            )
                return true;

            // if the response has multiple lines
            var slices = response.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
            return slices.First().StartsWith(code + "-") && slices.Last().StartsWith(code + " ");
        }

        private static int ExtractCode(string response)
        {
            return int.Parse(response.Substring(0, 3));
        }

        public CommandSender(Socket socket)
        {
            _socket = socket;
        }

        public (int Code, string Response) User(string username)
        {
            return SendAndGetResponse($"USER {username}\r\n");
        }

        public (int Code, string Response) Pass(string password)
        {
            return SendAndGetResponse($"PASS {password}\r\n");
        }

        public (int Code, string Response) Retr(string pathname)
        {
            return SendAndGetResponse($"RETR {pathname}\r\n");
        }

        public (int Code, string Response) Stor(string pathname)
        {
            return SendAndGetResponse($"STOR {pathname}\r\n");
        }

        public (int Code, string Response) Quit()
        {
            return SendAndGetResponse("QUIT\r\n");
        }

        public (int Code, string Response) Syst()
        {
            return SendAndGetResponse("SYST\r\n");
        }

        public (int Code, string Response) Type()
        {
            // only binary type is supported
            return SendAndGetResponse("TYPE I\r\n");
        }

        public (int Code, string Response) Port(IPEndPoint ipEndPoint)
        {
            var ip = ipEndPoint.Address;
            var port1 = ipEndPoint.Port / 256;
            var port2 = ipEndPoint.Port % 256;
            var ipSections = (from section in ip.ToString().Split('.') select int.Parse(section)).ToList();
            return SendAndGetResponse(
                $"PORT {ipSections[0]},{ipSections[1]},{ipSections[2]},{ipSections[3]},{port1},{port2}\r\n");
        }

        public (int Code, string Response) Pasv()
        {
            return SendAndGetResponse("PASV\r\n");
        }

        public (int Code, string Response) Mkd(string pathname)
        {
            return SendAndGetResponse($"MKD {pathname}\r\n");
        }

        public (int Code, string Response) Cwd(string pathname)
        {
            return SendAndGetResponse($"CWD {pathname}\r\n");
        }

        public (int Code, string Response) Pwd()
        {
            return SendAndGetResponse("PWD\r\n");
        }

        public (int Code, string Response) List(string pathname = null)
        {
            return SendAndGetResponse(pathname != null ? $"LIST {pathname}\r\n" : "LIST\r\n");
        }

        public (int Code, string Response) Rmd(string pathname)
        {
            return SendAndGetResponse($"RMD {pathname}\r\n");
        }
    }
}