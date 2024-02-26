using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

//   TO-DO:
// - finishing touches

namespace server
{
    public partial class Server : Form
    {

        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        List<string> questions = new List<string>();   // keeps questions
        List<double> answers = new List<double>();     // keeps answers
        List<Client> clients = new List<Client>();     // keeps clients (see client class at the bottom for details)
        List<string> usernames = new List<string>();   // keeps usernames for scoreboard

        bool terminating = false;
        bool listening = false;
        int q = 0;
        bool questionNumsSet = false;
        int questionNumber = 0;
        int questions_in_file = 0;
        int currentQuestion = 0;

        List<string> list = new List<string>();

            
        private void readQuestions()
        {
            string[] lines = File.ReadAllLines("questions.txt");
            int count = 0;

            foreach (string line in lines)
            {
                if (count % 2 == 0)
                {
                    questions.Add(line);
                    questions_in_file++;
                     count++;
                }
                else
                {
                    answers.Add(int.Parse(line));
                    count++;
                }
            }
        }

        private void showScores() // this will use a sorting algorithm in step 2
        {
            int count = clients.Count();
            List<string> scoreboard = new List<string>();
            try
            {
                if (clients.ElementAt(0).Score > clients.ElementAt(1).Score)
                {
                    scoreboard.Add(clients.ElementAt(0).Username + ": " + clients.ElementAt(0).Score);
                    scoreboard.Add(clients.ElementAt(1).Username + ": " + clients.ElementAt(1).Score);
                }

                else if (clients.ElementAt(1).Score > clients.ElementAt(0).Score)
                {
                    scoreboard.Add(clients.ElementAt(1).Username + ": " + clients.ElementAt(1).Score);
                    scoreboard.Add(clients.ElementAt(0).Username + ": " + clients.ElementAt(0).Score);
                }

                else
                {
                    scoreboard.Add(clients.ElementAt(0).Username + ": " + clients.ElementAt(0).Score);
                    scoreboard.Add(clients.ElementAt(1).Username + ": " + clients.ElementAt(1).Score);
                }
                sendMsg("\nSCOREBOARD:");
                for (int i = 0; i < count; i++)
                {
                    sendMsg($"{scoreboard.ElementAt(i)}");
                }
            }
            catch
            {
                scoreboard.Add(clients.ElementAt(0).Username + ": " + clients.ElementAt(0).Score);
                foreach (string username in usernames)
                {
                    if (username != clients.ElementAt(0).Username)
                        scoreboard.Add(username + ": 0");
                }
                sendMsg("\nSCOREBOARD:");
                for (int i = 0; i < usernames.Count(); i++)
                {
                    sendMsg($"{scoreboard.ElementAt(i)}");
                }
                return;
            }
        }

        private bool checkUserName(string name)
        {
            if (!clients.Any(client => client.Username == name))           
                return true; // username is eligible            
            return false; // username is taken
        }

        private void sendMsg(string message)
        {
            if (message != "" && message.Length <= 128)
            {
                Byte[] buffer = Encoding.Default.GetBytes(message);
                if (!message.Contains("."))
                logs.AppendText(message + "\n");
                foreach (Client client in clients)
                {
                    try
                    {
                        client.Socket.Send(buffer);
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            Thread.Sleep(100);
        }

        private void askQuestion()
        {
            q = currentQuestion % questions_in_file; // modulo operation allows to loop through question list
            sendMsg($"\n\nQ{currentQuestion + 1}) {questions.ElementAt(q)}");
            sendMsg(".ans");
        }

        private void giveAnswer()
        {

            sendMsg("The correct answer is " + answers.ElementAt(q).ToString()); // send everyone the answer          
            foreach (Client client in clients) // resets the answer check for the next questions
            {           
                client.Answered = false;
                sendMsg($"{client.Username}'s answer: {client.Answer}");
            }
        }

        private void collectAnswers()
        {
            int answersCollected = 0;
            while (answersCollected < clients.Count()) // wait for all the answers
            {
                try
                {
                    foreach (Client client in clients)
                    {
                        if (clients.Count != 2)
                            return;
                        if (client.Answered)
                        {
                            answersCollected++;
                            client.Answered = false;
                            string messageToSend = ".ack"; // send acknowledgment for the answer
                            byte[] msgBuffer = Encoding.Default.GetBytes(messageToSend);
                            client.Socket.Send(msgBuffer);
                        }
                    }                 
                }
                catch
                {
                    return;
                }

            }
            return;
        }

        private void assignScores()
        {
            try
            {
                double player0diff = Math.Abs(clients.ElementAt(0).Answer - answers.ElementAt(q));
                double player1diff = Math.Abs(clients.ElementAt(1).Answer - answers.ElementAt(q));

                if (player0diff > player1diff)
                {
                    clients.ElementAt(1).Score += 1;
                }

                else if (player0diff < player1diff)
                {
                    clients.ElementAt(0).Score += 1;
                }

                else
                {
                    clients.ElementAt(0).Score += 0.5;
                    clients.ElementAt(1).Score += 0.5;
                }
            }
            catch
            {
                return;
            }              
        }

        private void endGame() // winner selection will be looped in step 2
        {
            Client winner;
            if (clients.Count() == 0)
            {
                sendMsg("No player left to play the game!");
            }
            else if (clients.Count() == 1)
            {
                winner = clients.ElementAt(0);
                sendMsg("There is only one player left!");
                Thread.Sleep(1000);
                sendMsg($"{winner.Username} wins the game!");
                Thread.Sleep(1000);
                showScores();
                Thread.Sleep(1000);
            }
            else if (clients.ElementAt(0).Score > clients.ElementAt(1).Score)
            {
                winner = clients.ElementAt(0);
                sendMsg("Game Over!");
                Thread.Sleep(1000);
                sendMsg($"{winner.Username} wins the game!");
                Thread.Sleep(1000);
            }
            else if (clients.ElementAt(1).Score > clients.ElementAt(0).Score)
            {
                winner = clients.ElementAt(1);
                sendMsg("Game Over!");
                Thread.Sleep(1000);
                sendMsg($"{winner.Username} wins the game!");
                Thread.Sleep(1000);
            }
            else
            {
                sendMsg("Game Over!");
                Thread.Sleep(1000);
                sendMsg("It's a tie!");
                Thread.Sleep(1000);
                showScores();
                Thread.Sleep(1000);
            }
            sendMsg(".disc");
            foreach (Client client in clients)
                client.Socket.Close();          
            currentQuestion = 0;
            questionNumsSet = false;
            clients.Clear();
            usernames.Clear();
           
            button_set.Enabled = true; 
            textBox_port.Enabled = true;
            textBox_qnum.Enabled = true;
        }

        private void game()
        {
            while (clients.Count() != 2 || !questionNumsSet) // wait for 2 players to join
            {
                continue;
            }
            Thread.Sleep(100);
            sendMsg("\n------------------------------------------------");
            sendMsg($"The game has started\n{questionNumber} questions will be asked.");
            Thread.Sleep(500);

            while (currentQuestion < questionNumber && clients.Count() == 2) // actual game loop
            {
                askQuestion();
                collectAnswers();
                if (clients.Count() != 2)
                    break;
                assignScores();
                giveAnswer();
                showScores();
                currentQuestion++; // move to next question                
                Thread.Sleep(500);
            }
            endGame();
        }

        //////////////////////////////////////////////////////////////////////////////////

        public Server()
        {



            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            InitializeComponent();
            readQuestions();
        }

        private void button_listen_Click(object sender, EventArgs e)
        {
            int serverPort;
            logs.Clear();
            if(Int32.TryParse(textBox_port.Text, out serverPort))
            {
                textBox_port.Enabled = false;
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, serverPort);
                serverSocket.Bind(endPoint);
                serverSocket.Listen(3);

                listening = true;
                button_listen.Enabled = false;
                textBox_qnum.Enabled = true;
                button_set.Enabled = true;

                Thread acceptThread = new Thread(Accept);
                acceptThread.Start();

                logs.AppendText("Started listening on port: " + serverPort + "\n");

            }
            else
            {
                logs.AppendText("Please check port number \n");
            }
        }

        private void Accept()
        {
            while(listening)
            {
                try
                {
                    Socket newClient = serverSocket.Accept();
                    Byte[] bfr = new Byte[64];

                    if (newClient.Receive(bfr) > 0)
                    {
                        string message = Encoding.Default.GetString(bfr);
                        message = message.Substring(0, message.IndexOf("\0"));
                        if (message.Contains(".u")) // get username
                        {
                            string username = message.Substring(0, (message.Length - 2));

                            if(clients.Count() == 2) // don't allow new players if server is full
                            {
                                string messageToSend = ".full";
                                Byte[] errBuffer = Encoding.Default.GetBytes(messageToSend);
                                newClient.Send(errBuffer);
                                newClient.Close();
                            }

                            else if (!checkUserName(username)) // kick client if username is taken
                            {
                                string messageToSend = ".kick";
                                Byte[] errBuffer = Encoding.Default.GetBytes(messageToSend);
                                newClient.Send(errBuffer);
                                newClient.Close();                               
                            }

                            else // add client to players
                            {
                                string messageToSend = ".add";
                                Byte[] msgBuffer = Encoding.Default.GetBytes(messageToSend);
                                newClient.Send(msgBuffer);

                                var client = new Client
                                {
                                    Socket = newClient,
                                    Username = username,
                                };

                                clients.Add(client);
                                usernames.Add(client.Username);
                                logs.AppendText($"{username} has connected.\n");

                                Thread receiveThread = new Thread(() => Receive(client)); // updated
                                receiveThread.Start();
                            }
                        }
                    }
                }
                catch
                {
                    if (terminating)
                    {
                        listening = false;
                    }
                    else
                    {
                        logs.AppendText("The socket stopped working.\n");
                    }

                }
            }
        }

        private void Receive(Client thisClient) // updated
        {
            bool connected = true;

            while(connected && !terminating)
            {
                try
                {
                    Byte[] buffer = new Byte[64];
                    thisClient.Socket.Receive(buffer);

                    string incomingMessage = Encoding.Default.GetString(buffer);
                    incomingMessage = incomingMessage.Substring(0, incomingMessage.IndexOf("\0"));

                    logs.AppendText($"{thisClient.Username}: {incomingMessage} \n");
                    thisClient.Answered = true;
                    thisClient.Answer = double.Parse(incomingMessage);
                }
                catch
                {
                    if(!terminating)
                    {
                        logs.AppendText($"{thisClient.Username} has disconnected\n");
                        thisClient.Score = 0;
                    }
                    thisClient.Socket.Close();
                    clients.Remove(thisClient);
                    connected = false;
                }
            }

        }

        private void Form1_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            listening = false;
            terminating = true;
            Environment.Exit(0);
        }

        private void button_set_Click(object sender, EventArgs e)
        {
            
            textBox_qnum.Enabled = false;
            if (Int32.TryParse(textBox_qnum.Text, out questionNumber))
            {
                button_set.Enabled = false;
                questionNumsSet = true;
                Thread gameThread = new Thread(game);
                gameThread.Start();
            }

            else
                logs.AppendText("Enter a valid number of questions!");
        }

        private void textBox_qnum_TextChanged(object sender, EventArgs e)
        {

        }
    }

    public class Client
    {
        public Socket Socket;
        public string Username;
        public double Score = 0;
        public double Answer = 0;
        public bool Answered = false;
    }
}
