using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AssigmentForm
{
    public partial class MainForm : Form
    {
        private Thread chatServerThread = null;
        private Thread chatClientThread = null;
        private Thread transferServerThread = null;
        private Thread transferClientThread = null;
        private const int BUFFER_SIZE = 1024;

        private string IPConnect = null;
        private int portConnect = 0;
        private TcpListener chatListener = null;
        private TcpListener transferListener = null;
        private TcpClient client = null;
        private NetworkStream netStream = null;

        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            chatServerThread = new Thread(startChatServer);
            chatServerThread.IsBackground = true;
            chatServerThread.Start();

            transferServerThread = new Thread(startTransferServer);
            transferServerThread.IsBackground = true;
            transferServerThread.Start();
            /*Set T is Back Ground to Terminal Program when Press X button*/
        }

        public void startTransferServer()
        {
            while (true)
            {
                try
                {
                    transferListener = new TcpListener(IPAddress.Any, transferPort);
                    transferListener.Start();
                    string ipAddress = Dns.GetHostByName(Dns.GetHostName()).AddressList[0].ToString();
                    addTextView("Server started, IP Server = " +ipAddress);
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    transferPort += 1;
                }
            }
        }
/*Method for Thread Call*/
        public void startChatServer()
        {
            while (true)
            {
                try
                {
                    chatListener = new TcpListener(IPAddress.Any, serverPort);
                    chatListener.Start();
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    serverPort += 1;
                }
            }

            while (true)
            {
                try
                {
                    if (chatListener.Pending())
                    {
                        setStatus("Incoming Connection...");
                        string message = "Accept Incoming Connection?";
                        string caption = "New Connection";
                        MessageBoxButtons boxConfirm = MessageBoxButtons.YesNo;
                        DialogResult result = MessageBox.Show(message, caption, boxConfirm);
                        try
                        {
                            if (result == System.Windows.Forms.DialogResult.Yes)
                            {
                                client = chatListener.AcceptTcpClient();

                                setStatus("Connected");

                                netStream = client.GetStream();
                                while (true)
                                {
                                    receivingData();
                                }
                            }
                            else
                            {
                                chatListener.AcceptTcpClient();
                                setStatus("Waiting...");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            announceDisconnect();
                        }

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    break;
                }
            }  
        }


/*Method handle receiving data from Partner and write it to Form*/
        private void receivingData()
        {
            byte[] receiveData = new byte[BUFFER_SIZE];
            int receiveBytes = netStream.Read(receiveData, 0, BUFFER_SIZE);

            if (receiveBytes > 0)
            {
                string receiveMessage = Encoding.ASCII.GetString(receiveData);
                if (receiveMessage.Substring(0,receiveBytes) == "DISCONNECTPLEASE12345")
                {
                    client.Close();
                    return;
                }
                
                receiveMessage = "Partner: " + receiveMessage;
                addTextView(receiveMessage);
            }
        }

/*Method handle sending data to Partner and write it to form*/
        private void sendingDaTa()
        {
            string writeMessage = tbMessage.Text;
            try
            {
                if (writeMessage.Length > 0)
                {
                    byte[] writeData = Encoding.ASCII.GetBytes(writeMessage);
                    netStream.Write(writeData, 0, writeMessage.Length);
                    writeMessage = "You: " + writeMessage;
                    addTextView(writeMessage);
                    tbMessage.Clear();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
        }

        private delegate void SetTextCallback(string text);

/*Method edit status of connection*/
        private void setStatus(string text)
        {
            if (this.lbStatus.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(setStatus);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.lbStatus.Text = text;
            }
        }

/*Method add Text to main view*/
        private void addTextView(string text)
        {
            try
            {
                if (this.tbView.InvokeRequired)
                {
                    SetTextCallback d = new SetTextCallback(addTextView);
                    this.Invoke(d, new object[] { text });
                }
                else
                {
                    this.tbView.AppendText(text);
                    this.tbView.AppendText(Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            } 
        }

/*Method handle connecting to Server to chat and send File*/
        public void sendTCP()
        {
            try
            {
                client = new TcpClient(IPConnect, portConnect);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                addTextView("Cannot connect to " + IPConnect + " at " + portConnect.ToString());
                return;
            }

            try
            {
                setStatus("Connected");
                netStream = client.GetStream();
                while (true)
                {
                    receivingData();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                announceDisconnect();
                return;
            }
        }

/*Method excute when press New Connection*/
        private void newConnectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InputIPForm form = new InputIPForm();
            if (form.ShowDialog() == DialogResult.OK)
            {
                IPConnect = form.IP;
                portConnect = form.port;
                form.Dispose();
            }
            else
            {
                form.Dispose();
                return;
            }

            chatClientThread = new Thread(sendTCP);
            chatClientThread.Start();
        }

/*Method excute when press View Port*/
        private void viewPortToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string message = "Port Server: " + serverPort.ToString() + '\n' + "Port Transfer: " + transferPort.ToString();
            MessageBox.Show(message, "Current Port",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }

/*Method excute when press Send, send data to partner*/
        private void button1_Click(object sender, EventArgs e)
        {
            sendingDaTa();
        }

/*Check if Enter is pressing*/
        private void tbMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                sendingDaTa();
                e.SuppressKeyPress = true;       
             }
        }

/*Method excute when press Disconnect*/
        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] writeData = Encoding.ASCII.GetBytes("DISCONNECTPLEASE12345");
                netStream.Write(writeData, 0, writeData.Length);
                client.Close();
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }
        }

/*Method show string disconnect on View Textbox*/
        public void announceDisconnect()
        {
            string message = "Your partner has disconnectd...";
            addTextView(message);
            setStatus("Waiting...");
        }

/*Change port of server, Close old port and start Thread for new one*/

/*Method excute when press Exit*/
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

/*Method handle transfer file without encryption*/
        private void nornalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void chatPortToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangePortForm changePort = new ChangePortForm();
            changePort.setOldPort(serverPort);

            if (changePort.ShowDialog() == DialogResult.OK)
            {
                chatListener.Stop();
                serverPort = changePort.newPort;
                string message = "Server Port has been changed to " + serverPort.ToString();
                addTextView(message);

                chatServerThread.Abort();
                chatServerThread = new Thread(startChatServer);
                chatServerThread.IsBackground = true;
                chatServerThread.Start();
            }

            changePort.Dispose();
        }

        private void transferPortToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangePortForm changePort = new ChangePortForm();
            changePort.setOldPort(transferPort);

            if (changePort.ShowDialog() == DialogResult.OK)
            {
                transferListener.Stop();
                transferPort = changePort.newPort;
                string message = "Transfer Port has been changed to " + transferPort.ToString();
                addTextView(message);

                transferServerThread.Abort();
                transferServerThread = new Thread(startChatServer);
                transferServerThread.IsBackground = true;
                transferServerThread.Start();
            }

            changePort.Dispose();
        }

    }
}
