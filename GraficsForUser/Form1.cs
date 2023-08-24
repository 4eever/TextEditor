using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TextEditorClient;
using System.Security.Cryptography;
using System.Xml.Serialization;

namespace GraficsForUser
{
    public partial class Form1 : Form
    {
        static RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
        RSAParameters publicKeyServer;
        RSAParameters publicKey;
        RSAParameters privateKey;
        TextEditorClient.TextEditorClient client;
        bool _isconnected = false;
        bool autotimer = false;
        Timer autoSaveTimer = new Timer();
        Timer button1Timer = new Timer();
        Timer viewTimer = new Timer();
        bool view = false;

        public Form1()
        {
            InitializeComponent();
            label1.Text = "ip";
            label2.Text = "port";
            label3.Text = ".txt";
            button5.Enabled = false;
            button6.Enabled = false;
            button7.Enabled = false;
            button4.Enabled = false;
            button8.Enabled = false;
            button3.Enabled = false;
            button2.Enabled = false;
            textBox1.Enabled = false;
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e, string str)
        {
            richTextBox1.Text = str;
        }

        private void richTextBox2_TextChanged()
        {
            //список файлов
            client.SendInt(1);
            byte[] list = client.ReceiveBytes();
            list = client.DecryptData(list, privateKey);
            richTextBox2.ReadOnly = true;//
            richTextBox2.Text = Encoding.UTF8.GetString(list);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Connect/Disconnect

            if (_isconnected == true)
            {
                button1Timer.Stop();
                client.SendInt(4);
                client.Disconnect();
                button1.Text = "Connect";
                richTextBox2.Text = "";
                _isconnected = false;

                textBox2.Text = "";
                textBox3.Text = "";
                textBox2.Enabled = true;
                textBox3.Enabled = true;

                button5.Enabled = false;
                button6.Enabled = false;
                button7.Enabled = false;
                button4.Enabled = false;
                button8.Enabled = false;
                button3.Enabled = false;
                button2.Enabled = false;
                textBox1.Enabled = false;
            }
            else if (_isconnected == false)
            {
                string serverIp = textBox2.Text;
                int serverPort = int.Parse((textBox3.Text));
                textBox2.Enabled = false;
                textBox3.Enabled = false;

                client = new TextEditorClient.TextEditorClient(serverIp, serverPort);
                client.Connect();
                
                //создание публичного и приватного ключей, получение приватного ключа сервера
                publicKey = rsa.ExportParameters(false);
                privateKey = rsa.ExportParameters(true);
                publicKeyServer = client.ReceiveRSAParameters();
                client.SendRSAParameters(publicKey);
                
                button1.Text = "Disconnect";
                richTextBox2.Visible = true;
                button1Timer.Interval = 1000;
                _isconnected = true;

                button4.Enabled = true;
                button8.Enabled = true;
                button3.Enabled = true;
                button2.Enabled = true;
                textBox1.Enabled = true;

                button1Timer.Tick += (timerSender, timerArgs) =>
                {
                    richTextBox2_TextChanged();
                };
                button1Timer.Start();
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            //ввод файла
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //Create
            button1Timer.Stop();
            client.SendInt(5);
            string fileName = textBox1.Text;
            byte[] encryptFileName = client.EncryptData(Encoding.UTF8.GetBytes(fileName), publicKeyServer);
            client.SendBytes(encryptFileName);
            textBox1.Text = "";
            button1Timer.Start();

        }

        private void button4_Click(object sender, EventArgs e)
        {
            //Open
            button1Timer.Stop();
            client.SendInt(2);
            string fileName = textBox1.Text;
            byte[] bytesFileName = Encoding.UTF8.GetBytes(fileName);
            bytesFileName = client.EncryptData(bytesFileName, publicKeyServer);
            client.SendBytes(bytesFileName);

            byte[] byteText = client.ReceiveBytes();
            byteText = client.DecryptData(byteText, privateKey);
            string text = Encoding.UTF8.GetString(byteText);
            richTextBox1_TextChanged(this, EventArgs.Empty, text);

            textBox1.Text = "";
            textBox1.Enabled = false;
            button1.Enabled = false;
            button8.Enabled = false;
            button3.Enabled = false;
            button2.Enabled = false;
            button4.Enabled = false;

            button5.Enabled = true;
            button6.Enabled = true;
            button7.Enabled = true;


        }

        private void button2_Click(object sender, EventArgs e)
        {
            //Delete
            button1Timer.Stop();
            client.SendInt(6);
            string fileName = textBox1.Text;
            byte[] encryptFileName = client.EncryptData(Encoding.UTF8.GetBytes(fileName), publicKeyServer);
            client.SendBytes(encryptFileName);
            textBox1.Text = "";
            button1Timer.Start();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //Save and Log out
            client.SendInt(0);
            string editedText = richTextBox1.Text;
            byte[] bytesEditedText = Encoding.UTF8.GetBytes(editedText);
            bytesEditedText = client.EncryptData(bytesEditedText, publicKeyServer);
            client.SendBytes(bytesEditedText);
            richTextBox1.Text = "";
            button1Timer.Start();

            textBox1.Enabled = true;
            button1.Enabled = true;
            button8.Enabled = true;
            button3.Enabled = true;
            button2.Enabled = true;
            button4.Enabled = true;
            
            button5.Enabled = false;
            button6.Enabled = false;
            button7.Enabled = false;

        }

        private void button7_Click(object sender, EventArgs e)
        {
            //Autosave
            if (autotimer == false)
            {

                button5.Enabled = false;
                button6.Enabled = false;

                button7.Text = "Stop";
                // Устанавливаем интервал таймера на 1 секунду
                autoSaveTimer.Interval = 1000;
                autotimer = true;

                autoSaveTimer.Tick += (timerSender, timerArgs) =>
                {
                    // Сохраняем
                    client.SendInt(1);
                    string editedText = richTextBox1.Text;
                    byte[] bytesEditedText = Encoding.UTF8.GetBytes(editedText);
                    bytesEditedText = client.EncryptData(bytesEditedText, publicKeyServer);
                    client.SendBytes(bytesEditedText);
                };

                // Запускаем таймер
                autoSaveTimer.Start();
            }

            else if (autotimer == true)
            {
                button5.Enabled = true;
                button6.Enabled = true;

                autoSaveTimer.Stop();
                button7.Text = "AutoSave";
                client.SendInt(1);
                string editedText = richTextBox1.Text;
                byte[] bytesEditedText = Encoding.UTF8.GetBytes(editedText);
                bytesEditedText = client.EncryptData(bytesEditedText, publicKeyServer);
                client.SendBytes(bytesEditedText);
                autotimer = false;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            //Save
            client.SendInt(1);
            string editedText = richTextBox1.Text;
            byte[] bytesEdietdText = Encoding.UTF8.GetBytes(editedText);
            bytesEdietdText = client.EncryptData(bytesEdietdText, publicKeyServer);
            client.SendBytes(bytesEdietdText);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            //View
            if (view == false)
            {
                button4.Enabled = false;
                button1.Enabled = false;
                button3.Enabled = false;
                button2.Enabled = false;
                textBox1.Enabled = false;

                string fileName = textBox1.Text;
                richTextBox1.ReadOnly = true;
                button8.Text = "Stop";

                viewTimer.Interval = 1000;
                view = true;

                viewTimer.Tick += (timerSender, timerArgs) =>
                {
                    client.SendInt(3);
                    byte[] encryptFileName = client.EncryptData(Encoding.UTF8.GetBytes(fileName), publicKeyServer);
                    client.SendBytes(encryptFileName);

                    byte[] textBytes = client.ReceiveBytes();
                    textBytes = client.DecryptData(textBytes, privateKey);
                    string text = Encoding.UTF8.GetString(textBytes);
                    richTextBox1.Text = text;
                };

                viewTimer.Start();
            }
            else if (view == true)
            {
                button4.Enabled = true;
                button1.Enabled = true;
                button3.Enabled = true;
                button2.Enabled = true;
                textBox1.Enabled = true;

                viewTimer.Stop();
                textBox1.Text = "";
                button8.Text = "View";
                view = false;
                richTextBox1.ReadOnly = false;
                richTextBox1.Text = "";

            }

        }

        private void label1_Click(object sender, EventArgs e)
        {
            //ip
        }

        private void label2_Click(object sender, EventArgs e)
        {
            //port
        }

        private void label3_Click(object sender, EventArgs e)
        {
            //(.txt)
        }
    }
}