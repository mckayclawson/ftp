/*
 * FTP Sample class
 * McKay Clawson (msc3254@rit.edu)
 */
using System;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace FTP
{
    /// <summary>
    /// FTP class for DataComm I Project 1
    /// Author McKay S. Clawson
    /// </summary>
    class FTP
    {
        // The prompt
        public const string PROMPT = "FTP> ";

        // Information to parse commands
        public static readonly string[] COMMANDS = { "ascii",
					      "binary",
					      "cd",
					      "cdup",
					      "debug",
					      "dir",
					      "get",
					      "help",
					      "passive",
                          "put",
                          "pwd",
                          "quit",
                          "user" };

        public const int ASCII = 0;
        public const int BINARY = 1;
        public const int CD = 2;
        public const int CDUP = 3;
        public const int DEBUG = 4;
        public const int DIR = 5;
        public const int GET = 6;
        public const int HELP = 7;
        public const int PASSIVE = 8;
        public const int PUT = 9;
        public const int PWD = 10;
        public const int QUIT = 11;
        public const int USER = 12;

        // Help message

        public static readonly String[] HELP_MESSAGE = {
	"ascii      --> Set ASCII transfer type",
	"binary     --> Set binary transfer type",
	"cd <path>  --> Change the remote working directory",
	"cdup       --> Change the remote working directory to the",
        "               parent directory (i.e., cd ..)",
	"debug      --> Toggle debug mode",
	"dir        --> List the contents of the remote directory",
	"get path   --> Get a remote file",
	"help       --> Displays this text",
	"passive    --> Toggle passive/active mode",
    "put path   --> Transfer the specified file to the server",
	"pwd        --> Print the working directory on the server",
    "quit       --> Close the connection to the server and terminate",
	"user login --> Specify the user name (will prompt for password" };





        static void Main(string[] args)
        {
            //Scanner in = new Scanner( System.in );
            bool eof = false;
            String input = null;

            // Handle the command line stuff

            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: [mono] Ftp server");
                Environment.Exit(1);
            }

            //Connect to host and login
            String server = args[0];
            TcpClient conn = new TcpClient(server, 21);
            Console.WriteLine("Trying " + Dns.GetHostEntry(server).AddressList[0] + "...");
            if (conn.Connected == true)
            {
                Console.WriteLine("Connected to " + server);
            }
            StreamReader reader = new StreamReader(conn.GetStream());
            StreamWriter writer = new StreamWriter(conn.GetStream());
            writeResponse(reader);
            Console.Write("Username: ");
            String user = Console.ReadLine();
            Console.Write("Password: ");
            String pass = Console.ReadLine();
            sendCommand(writer,"USER " + user);
            writeResponse(reader);
            sendCommand(writer, "PASS " + pass);
            writeResponse(reader);
            sendCommand(writer, "PASV");
            writeResponse(reader);

            do
            {
                try
                {
                    Console.Write(PROMPT);
                    input = Console.ReadLine();
                }
                catch (Exception e)
                {
                    eof = true;
                }

                // Keep going if we have not hit end of file
                if (!eof && input.Length > 0)
                {
                    int cmd = -1;
                    string[] argv = Regex.Split(input, "\\s+");

                    // What command was entered?
                    for (int i = 0; i < COMMANDS.Length && cmd == -1; i++)
                    {
                        if (COMMANDS[i].Equals(argv[0], StringComparison.CurrentCultureIgnoreCase))
                        {
                            cmd = i;
                        }
                    }

                    // Execute the command
                    switch (cmd)
                    {
                        case ASCII:
                            sendCommand(writer, "Type A");
                            writeResponse(reader);
                            break;

                        case BINARY:
                            sendCommand(writer, "Type I");
                            writeResponse(reader);
                            break;

                        case CD:
                            String path = argv[1];
                            sendCommand(writer, "CWD " + path);
                            writeResponse(reader);
                            break;

                        case CDUP:
                            sendCommand(writer, "CDUP");
                            writeResponse(reader);
                            break;

                        case DEBUG:
                            break;

                        case DIR:
                            TcpClient dataConn = new TcpClient(server, 21);
                            Stream dataStream = dataConn.GetStream();
                            StreamReader dataReader = new StreamReader(dataStream);
                            writeResponse(dataReader);
                            StreamWriter dataWriter = new StreamWriter(dataStream);
                            sendCommand(dataWriter,"USER " + user);
                            writeResponse(dataReader);
                            sendCommand(dataWriter, "PASS " + pass);
                            writeResponse(dataReader);
                            sendCommand(dataWriter, "LIST");
                            writeResponse(dataReader);
                            dataReader.Close();
                            dataConn.Close();
                            //writeResponse(reader);
                            break;

                        case GET:
                            String fileName = argv[1];
                            sendCommand(writer, "RETV " + fileName);
                            writeResponse(reader);
                            break;

                        case HELP:
                            for (int i = 0; i < HELP_MESSAGE.Length; i++)
                            {
                                Console.WriteLine(HELP_MESSAGE[i]);
                            }
                            break;

                        case PASSIVE:
                            break;

                        case PUT:
                            ///not listed in the writeup
                            break;

                        case PWD:
                            sendCommand(writer, "PWD");
                            writeResponse(reader);
                            break;

                        case QUIT:
                            sendCommand(writer, "QUIT");
                            writeResponse(reader);
                            reader.Close();
                            writer.Close();
                            conn.Close();
                            eof = true;
                            break;

                        case USER:
                            String userName = argv[1];
                            sendCommand(writer, "USER " + userName);
                            writeResponse(reader);
                            break;

                        default:
                            Console.WriteLine("Invalid command");
                            break;
                    }
                }
            } while (!eof);
        }

        static void writeResponse(StreamReader r)
        {
            while (!r.EndOfStream)
            {
                String line = r.ReadLine();
                Console.WriteLine(line);
                string[] words = Regex.Split(line, "\\s+");
                if (!words[0].EndsWith("-"))
                {
                    r.DiscardBufferedData();
                    break;
                }
            }
        }
        static void sendCommand(StreamWriter w, String command)
        {
            w.WriteLine(command);
            w.Flush();
        }
    }
}