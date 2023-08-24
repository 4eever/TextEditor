﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Xml.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace TextEditorClient
{
    public class TextEditorClient
    {
        private readonly string _serverIp;
        private readonly int _serverPort;
        private Socket _socket;
        private bool _isConnected;

        static void Main(string[] args) { }

        public TextEditorClient(string serverIp, int serverPort)
        {
            _serverIp = serverIp;
            _serverPort = serverPort;
        }

        public bool Connect()
        {
            if (_isConnected) return true;

            try
            {
                var ipEndPoint = new IPEndPoint(IPAddress.Parse(_serverIp), _serverPort);
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                _socket.Connect(ipEndPoint);

                _isConnected = true;

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection error occurred: {ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            if (!_isConnected) return;

            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
            _isConnected = false;
            Console.WriteLine("Disconnected from server");
        }

        public void SendInt(int value)
        {
            var data = BitConverter.GetBytes(value);
            _socket.Send(data);
        }

        public string ReceiveString()
        {
            var buffer = new byte[256];
            var size = 0;
            var data = new StringBuilder();
            try
            {
                do
                {
                    size = _socket.Receive(buffer);
                    data.Append(Encoding.UTF8.GetString(buffer, 0, size));
                }
                while (_socket.Available > 0);
            }
            catch
            {
                Disconnect();
            }
            return data.ToString();
        }

        public void SendData(string value)
        {
            var data = Encoding.UTF8.GetBytes(value);
            _socket.Send(data);
        }

        public int ReceiveInt()
        {
            byte[] buffer = new byte[sizeof(int)];
            _socket.Receive(buffer);
            return BitConverter.ToInt32(buffer, 0);
        }

        //метод для шифрования данных
        public byte[] EncryptData(byte[] data, RSAParameters publicKey)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(publicKey);
                return rsa.Encrypt(data, true);
            }
        }

        // Метод для дешифрования данных
        public byte[] DecryptData(byte[] data, RSAParameters privateKey)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(privateKey);
                return rsa.Decrypt(data, true);
            }
        }

        public byte[] ReceiveBytes()
        {
            var buffer = new byte[128];
            var size = 0;
            var data = new StringBuilder();
            try
            {
                do
                {
                    size = _socket.Receive(buffer);
                    data.Append(Encoding.UTF8.GetString(buffer, 0, size));
                }
                while (_socket.Available > 0);
            }
            catch
            {
                Disconnect();
            }
            return buffer;
        }

        public void SendBytes(byte[] value)
        {
            _socket.Send(value);
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

        public void SendRSAParameters(RSAParameters value)
        {
            _socket.Send(RSAParametersToBytes(value));
        }

        public static RSAParameters BytesToRSAParameters(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            {
                var formatter = new BinaryFormatter();
                return (RSAParameters)formatter.Deserialize(stream);
            }
        }

        public RSAParameters ReceiveRSAParameters()
        {
            var buffer = new byte[256];
            var size = 0;
            var data = new StringBuilder();
            do
            {
                size = _socket.Receive(buffer);
                data.Append(Encoding.UTF8.GetString(buffer, 0, size));
            }
            while (_socket.Available > 0);
            return BytesToRSAParameters(buffer);
        }
    }
}