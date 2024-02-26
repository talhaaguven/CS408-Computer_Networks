using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace client
{
    public partial class Form1 : Form
    {

        bool terminating = false;
        bool connected = false;
        Socket clientSocket;

        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            InitializeComponent();
        }

        private void button_connect_Click(object sender, EventArgs e)
        {
            logs.Clear();
            textBox_ip.Enabled = false;
            textBox_name.Enabled = false;
            textBox_port.Enabled = false;
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string IP = textBox_ip.Text;

            int portNum;
            if(Int32.TryParse(textBox_port.Text, out portNum))
            {
                try
                {
                    clientSocket.Connect(IP, portNum);                                 

                    string username = textBox_name.Text;
                    username += ".u";

                    Byte[] buffer = Encoding.Default.GetBytes(username);
                    clientSocket.Send(buffer);
                    terminating = false;

                    Byte[] bfr = new Byte[64];
                    if (clientSocket.Receive(bfr) > 0)
                    {
                        string incomingMessage = Encoding.Default.GetString(bfr);
                        incomingMessage = incomingMessage.Substring(0, incomingMessage.IndexOf("\0"));

                        if (incomingMessage.Contains(".kick"))
                        {
                            logs.AppendText("Username is already taken,\nPlease change and reconnect!\n");

                            clientSocket.Close();
                            connected = false;
                            terminating = true;

                            textBox_name.Enabled = true;
                            textBox_port.Enabled = true;
                            textBox_ip.Enabled = true;
                            button_disconnect.Enabled = false;
                            button_connect.Enabled = true;
                            textBox_message.Enabled = false;
                            button_send.Enabled = false;
                        }

                        else if (incomingMessage.Contains(".full"))
                        {
                            logs.AppendText("The server is currently full!\n");                            

                            clientSocket.Close();
                            connected = false;
                            terminating = true;

                            button_disconnect.Enabled = false;
                            button_connect.Enabled = true;
                            textBox_message.Enabled = false;
                            button_send.Enabled = false;
                            button_connect.Enabled = false;
                        }

                        else
                        {
                            logs.AppendText("Connected to the server!\n");
                            button_connect.Enabled = false;
                            button_disconnect.Enabled = true;
                            connected = true;

                            Thread receiveThread = new Thread(Receive);
                            receiveThread.Start();
                        }
                    }

                }
                catch
                {
                    logs.AppendText("Could not connect to the server!\n");
                    textBox_ip.Enabled = true;
                    textBox_name.Enabled = true;
                    textBox_port.Enabled = true;
                }
            }
            else
            {
                logs.AppendText("Check the port\n");
            }

        }

        private void Receive()
        {
            while(connected)
            {
                try
                {
                    Byte[] buffer = new Byte[128];
                    clientSocket.Receive(buffer);

                    string incomingMessage = Encoding.Default.GetString(buffer);
                    incomingMessage = incomingMessage.Substring(0, incomingMessage.IndexOf("\0"));

                    if (incomingMessage.Contains(".ans"))
                    {
                        button_send.Enabled = true;
                        textBox_message.Enabled = true;
                        textBox_message.Text = "";
                    }

                    else if (incomingMessage.Contains(".ack"))
                    {
                        button_send.Enabled = false;
                        textBox_message.Enabled = false;
                    }

                    else if (incomingMessage.Contains(".disc"))
                    {
                        clientSocket.Close();
                        connected = false;
                        terminating = true;

                        button_connect.Enabled = true;
                        textBox_ip.Enabled = true;
                        textBox_name.Enabled = true;
                        textBox_port.Enabled = true;
                        textBox_message.Enabled = false;
                        button_send.Enabled = false;
                        button_disconnect.Enabled = false;

                        logs.AppendText("Disconnected from the server\n");
                    }
                    else
                        logs.AppendText(incomingMessage + "\n");
                }
                catch
                {
                    if (!terminating)
                    {
                        logs.AppendText("The server has disconnected\n");
                        button_disconnect.Enabled = false;
                        textBox_ip.Enabled = true;
                        textBox_name.Enabled=true;
                        textBox_port.Enabled = true;

                        button_connect.Enabled = true;
                        textBox_message.Enabled = false;
                        button_send.Enabled = false;
                    }

                    clientSocket.Close();
                    connected = false;
                }

            }
        }

        private void Form1_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            connected = false;
            terminating = true;
            Environment.Exit(0);
        }

        private void button_send_Click(object sender, EventArgs e)
        {
            string message = textBox_message.Text;

            if(message != "" && message.Length <= 64)
            {
                Byte[] buffer = Encoding.Default.GetBytes(message);
                clientSocket.Send(buffer);
            }
        }

        private void button_disconnect_Click(object sender, EventArgs e)
        {
            clientSocket.Close();
            connected = false;
            terminating = true;

            button_connect.Enabled = true;
            textBox_ip.Enabled = true;
            textBox_name.Enabled = true;
            textBox_port.Enabled = true;
            textBox_message.Enabled = false;
            button_send.Enabled = false;
            button_disconnect.Enabled = false;

            logs.AppendText("Disconnected from the server\n");
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }
    }
}
