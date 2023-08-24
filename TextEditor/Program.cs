using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TextEditorServer
{
    internal class Program
    {
        static readonly List<Socket> ConnectedClients = new List<Socket>();
        static RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();


        static void Main(string[] args)
        {
            const string ip = "127.0.0.1";
            const int port = 8080;

            var tcpEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

            var tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);



            tcpSocket.Bind(tcpEndPoint);
            tcpSocket.Listen(5);
            Console.WriteLine($"Server is listening on {ip}:{port}");

            while (true)
            {
                var listener = tcpSocket.Accept();
                ConnectedClients.Add(listener);

                Console.WriteLine($"New client connected. Total clients: {ConnectedClients.Count}");

                _ = Task.Run(() => HandleClient(listener)); // Запуск обработки клиента в отдельном потоке
            }
        }

        private static void HandleClient(Socket client)
        {
            string clientName = "Client" + (ConnectedClients.Count).ToString();
            //string clientName = client.RemoteEndPoint.ToString();

            RSAParameters publicKey = rsa.ExportParameters(false);
            RSAParameters privateKey = rsa.ExportParameters(true);
            SendRSAParameters(client, publicKey);
            RSAParameters publicClientKey = ReceiveRSAParameters(client);

            while (true)
            {
                bool logOut = false;
                int fileNumber = ReceiveInt(client);
;                switch (fileNumber)
                {
                    case 1:
                        //выгрузка имен файлов из папки и отправка клиенту
                        string folderPath = @"D:\pnyavuC#\TextEditor\TextEditor\bin\Debug\files";
                        string fileNamesString = GetFileNamesStringFromFolder(folderPath);
                        byte[] encryptData = EncryptData(Encoding.UTF8.GetBytes(fileNamesString), publicClientKey);
                        SendBytes(client, encryptData);
                        logOut = false;
                        break;
                    case 2:
                        //Open => Save/AutoSave/Save and Log out
                        byte[] byteFileName = ReceiveBytes(client);
                        byteFileName = DecryptData(byteFileName, privateKey);
                        string fileName = Encoding.UTF8.GetString(byteFileName);
                        string file = System.IO.File.ReadAllText($@"D:\pnyavuC#\TextEditor\TextEditor\bin\Debug\files\{fileName}.txt");

                        byte[] byteText = Encoding.UTF8.GetBytes(file);
                        byteText = EncryptData(byteText, publicClientKey);
                        SendBytes(client, byteText);
                        while (true)
                        {
                            int permission = ReceiveInt(client);
                            string editedText;
                            byte[] byteEditedText;

                            if (permission == 0)
                            {
                                byteEditedText = ReceiveBytes(client);
                                byteEditedText = DecryptData(byteEditedText, privateKey);
                                editedText = Encoding.UTF8.GetString(byteEditedText);
                                System.IO.File.WriteAllText($@"D:\pnyavuC#\TextEditor\TextEditor\bin\Debug\files\{fileName}.txt", editedText);
                                logOut = false;
                                break;
                            }
                            else if (permission == 1)
                            {
                                byteEditedText = ReceiveBytes(client);
                                byteEditedText = DecryptData(byteEditedText, privateKey);
                                editedText = Encoding.UTF8.GetString(byteEditedText);
                                System.IO.File.WriteAllText($@"D:\pnyavuC#\TextEditor\TextEditor\bin\Debug\files\{fileName}.txt", editedText);
                                logOut = false;
                            }
                        }
                        break;
                    case 3:
                        //View
                        byte[] fileNameForViewBytes = ReceiveBytes(client);
                        fileNameForViewBytes = DecryptData(fileNameForViewBytes, privateKey);
                        string fileNameForView = Encoding.UTF8.GetString(fileNameForViewBytes);
                        string fileForView = System.IO.File.ReadAllText($@"D:\pnyavuC#\TextEditor\TextEditor\bin\Debug\files\{fileNameForView}.txt");

                        byte[] fileForViewBytes = Encoding.UTF8.GetBytes(fileForView);
                        fileForViewBytes = EncryptData(fileForViewBytes, publicClientKey);
                        SendBytes(client, fileForViewBytes);
                        logOut = false;
                        break;
                    case 4:
                        Console.WriteLine($"{clientName} disconnected.");
                        ConnectedClients.Remove(client);
                        client.Shutdown(SocketShutdown.Both);
                        client.Close();
                        logOut = true;
                        break;
                    case 5:
                        //Create
                        byte[] createFileBytes = ReceiveBytes(client);
                        createFileBytes = DecryptData(createFileBytes, privateKey);
                        string createFile = Encoding.UTF8.GetString(createFileBytes);
                        string path = $@"D:\pnyavuC#\TextEditor\TextEditor\bin\Debug\files\{createFile}.txt";
                        if (!File.Exists(path))
                        {
                            File.Create(path);
                        }
                        logOut = false;
                        break;
                    case 6:
                        //Delete
                        byte[] deleteFileBytes = ReceiveBytes(client);
                        deleteFileBytes = DecryptData(deleteFileBytes, privateKey);
                        string deleteFile = Encoding.UTF8.GetString(deleteFileBytes);
                        string pathForDelete = $@"D:\pnyavuC#\TextEditor\TextEditor\bin\Debug\files\{deleteFile}.txt";
                        if (File.Exists(pathForDelete))
                        {
                            File.Delete(pathForDelete);
                        }
                        logOut = false;
                        break;
                    default:
                        Console.WriteLine("Wrong choice");
                        break;
                }
                if (logOut == true)
                    break;
            }
        }

        private static string ReceiveData(Socket client)
        {
            var buffer = new byte[256];
            var size = 0;
            var data = new StringBuilder();
            do
            {
                size = client.Receive(buffer);
                data.Append(Encoding.UTF8.GetString(buffer, 0, size));
            } while (client.Available > 0);
            return data.ToString();
        }

        private static void SendData(Socket client, string data)
        {
            var buffer = Encoding.UTF8.GetBytes(data);
            client.Send(buffer);
        }

        private static int ReceiveInt(Socket client)
        {
            byte[] buffer = new byte[sizeof(int)];
            client.Receive(buffer);
            return BitConverter.ToInt32(buffer, 0);
        }

        static string GetFileNamesStringFromFolder(string folderPath)
        {
            string[] fileNames = Directory.GetFiles(folderPath);

            for (int i = 0; i < fileNames.Length; i++)
            {
                fileNames[i] = Path.GetFileNameWithoutExtension(fileNames[i]);
            }

            return string.Join(Environment.NewLine, fileNames);
        }

        static void SendBytes(Socket client, byte[] data)
        {
            client.Send(data);
        }

        static byte[] ReceiveBytes(Socket client)
        {
            byte[] buffer = new byte[128];
            var size = 0;
            var data = new StringBuilder();
            do
            {
                size = client.Receive(buffer);  
                data.Append(Encoding.UTF8.GetString(buffer, 0, size));
            } while (client.Available > 0);
            return buffer;
        }

        // Метод для шифрования данных
        static byte[] EncryptData(byte[] data, RSAParameters publicKey)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(publicKey);
                return rsa.Encrypt(data, true);
            }
        }

        // Метод для дешифрования данных
        static byte[] DecryptData(byte[] data, RSAParameters privateKey)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(privateKey);
                return rsa.Decrypt(data, true);
            }
        }

        static void SendRSAParameters(Socket client, RSAParameters key)
        {
            client.Send(RSAParametersToBytes(key));
        }

        public static byte[] RSAParametersToBytes(RSAParameters parameters)
        {
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, parameters);
                return stream.ToArray();
            }
        }

        public static RSAParameters BytesToRSAParameters(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            {
                var formatter = new BinaryFormatter();
                return (RSAParameters)formatter.Deserialize(stream);
            }
        }

        static RSAParameters ReceiveRSAParameters(Socket client)
        {
            byte[] buffer = new byte[256];
            var size = 0;
            var data = new StringBuilder();
            do
            {
                size = client.Receive(buffer);
                data.Append(Encoding.UTF8.GetString(buffer, 0, size));
            } while (client.Available > 0);
            return BytesToRSAParameters(buffer);
        }
    }

}