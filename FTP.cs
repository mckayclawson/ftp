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


        //My Global Readers and Writers
        public static String host;
        public static TcpClient conn;
        public static StreamReader reader;
        public static StreamWriter writer;
        public static TcpClient dataConn;
        public static Stream dataStream;
        public static StreamReader dataReader;
        public static Boolean isPassive = true;
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
            host = args[0];
            conn = new TcpClient(host, 21);
            Console.WriteLine("Trying " + Dns.GetHostEntry(host).AddressList[0] + "...");
            if (conn.Connected == true)
            {
                Console.WriteLine("Connected to " + host);
            }
            reader = new StreamReader(conn.GetStream());
            writer = new StreamWriter(conn.GetStream());
            Console.Write(getResponse(reader));
            login();

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
                            Console.Write(getResponse(reader));
                            break;

                        case BINARY:
                            sendCommand(writer, "Type I");
                            Console.Write(getResponse(reader));
                            break;

                        case CD:
                            String path = argv[1];
                            sendCommand(writer, "CWD " + path);
                            Console.Write(getResponse(reader));
                            break;

                        case CDUP:
                            sendCommand(writer, "CDUP");
                            Console.Write(getResponse(reader));
                            break;

                        case DEBUG:
                            break;

                        case DIR:
                            listDir();
                            break;

                        case GET:
                            String fileName = argv[1];
                            getFile(fileName);
                            break;

                        case HELP:
                            for (int i = 0; i < HELP_MESSAGE.Length; i++)
                            {
                                Console.WriteLine(HELP_MESSAGE[i]);
                            }
                            break;

                        case PASSIVE:
                            if (isPassive)
                            {
                                IPAddress localIP = null;
                                IPHostEntry bla = Dns.GetHostEntry(Dns.GetHostName());
                                foreach (IPAddress ip in bla.AddressList)
                                {
                                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                                    {
                                        localIP = ip;
                                    }
                                }
                                
                                TcpListener portListener = new TcpListener(localIP,0);
                                portListener.Start();
                                IPEndPoint portEndPoint= (IPEndPoint)portListener.LocalEndpoint;
                                String ip1 = portEndPoint.Address.ToString();
                                String sPort = portEndPoint.Port.ToString();
                                int port = int.Parse(sPort);
                                int portHi = ((port >> 8) & 0xff);
                                int portLo = ((port >> 0) & 0xff);
                                String[] ipBlock = Regex.Split(ip1,"\\.");
                                Console.WriteLine(ipBlock[0] + "," + ipBlock[1] + "," + ipBlock[2] + "," + ipBlock[3] + "," + portHi + "," + portLo);
                                sendCommand(writer,"PORT " + ipBlock[0]+","+ipBlock[1]+","+ipBlock[2]+","+ipBlock[3]+","+portHi+","+portLo);
                                //Console.Write(getResponse(reader));
                                
                            }
                            else
                            {
                                sendCommand(writer, "PASV");
                                Console.Write(getResponse(reader));
                            }
                            break;

                        case PUT:
                            ///not listed in the writeup
                            break;

                        case PWD:
                            sendCommand(writer, "PWD");
                            Console.Write(getResponse(reader));
                            break;

                        case QUIT:
                            logout();
                            eof = true;
                            break;

                        case USER:
                            String userName = argv[1];
                            sendCommand(writer, "USER " + userName);
                            Console.Write(getResponse(reader));
                            break;

                        default:
                            Console.WriteLine("Invalid command");
                            break;
                    }
                }
            } while (!eof);
        }

        static String getResponse(StreamReader r)
        {
            String response = "";
            while (!r.EndOfStream)
            {
                String line = r.ReadLine();
                response = response + line + "\n";
                string[] words = Regex.Split(line, "\\s+");
                if (!words[0].EndsWith("-"))
                {
                    r.DiscardBufferedData();
                    break;
                }
            }
            return response;
        }

        static void sendCommand(StreamWriter w, String command)
        {
            w.WriteLine(command);
            w.Flush();
        }

        static void prepareForDataTransfer()
        {
            sendCommand(writer, "PASV");
            String dataAddress = getResponse(reader);
            string[] addressParts = dataAddress.Split("()".ToCharArray())[1].Split(",".ToCharArray());
            string dataHost = addressParts[0] + "." + addressParts[1] + "." + addressParts[2] + "." + addressParts[3];
            int dataPort = int.Parse(addressParts[4]) * 256 + int.Parse(addressParts[5]);
            dataConn = new TcpClient(dataHost, dataPort);
            dataStream = dataConn.GetStream();
        }

        static void login()
        {
            String loginResponse = "";
            String loginResponseCode = "";
            String[] loginResponseList;
            while (loginResponseCode.Equals("230", StringComparison.CurrentCultureIgnoreCase)) ;
            {
                Console.Write("Username: ");
                String user = Console.ReadLine();
                Console.Write("Password: ");
                String pass = Console.ReadLine();
                sendCommand(writer, "USER " + user);
                Console.Write(getResponse(reader));
                sendCommand(writer, "PASS " + pass);
                loginResponse = getResponse(reader);
                Console.Write(loginResponse);
                loginResponseList = Regex.Split(loginResponse, "\\s+");
                loginResponseCode = loginResponseList[0];
            }
        }

        static void logout()
        {
            sendCommand(writer, "QUIT");
            Console.Write(getResponse(reader));
            reader.Close();
            writer.Close();
            conn.Close();
        }

        static void getFile(String fileName)
        {
            sendCommand(writer, "TYPE I");
            prepareForDataTransfer();
            sendCommand(writer, "RETR " + fileName);
            Console.Write(getResponse(reader));
            dataReader = new StreamReader(dataStream);
            FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
            byte[] b = new byte[100000];
            int n;
            while ((n = dataStream.Read(b, 0, b.Length)) > 0)
            {
                fs.Write(b, 0, n);
            }
            dataReader.Close();
            dataStream.Close();
            dataConn.Close();
            Console.Write(getResponse(reader));
        }

        static void listDir()
        {
            sendCommand(writer, "TYPE I");
            Console.Write(getResponse(reader));
            prepareForDataTransfer();
            sendCommand(writer, "LIST");
            Console.Write(getResponse(reader));
            dataReader = new StreamReader(dataStream);
            String dirLine;
            while ((dirLine = dataReader.ReadLine()) != null)
            {
                Console.WriteLine(dirLine);
            }
            dataReader.Close();
            dataStream.Close();
            dataConn.Close();
            Console.Write(getResponse(reader));
        }
    }
}
