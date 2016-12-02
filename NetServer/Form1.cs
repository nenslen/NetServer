using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetServer {
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
        }

        private IPAddress ip;
        private int port;
        private List<Message> messages = new List<Message>();
        private Thread listenThread;

        public struct Message {
            public int sequenceNumber;
            public string type;
            public string source;
            public string destination;
            public string payload;
        };


        private void btnStart_Click(object sender, EventArgs e) {
            
            // Set server info
            ip = IPAddress.Parse(txtIP.Text);
            port = 1986;

            listenThread = new Thread(listen);
            listenThread.Start();
            //Console.WriteLine("lskdf");
            //listen();
            timer1.Start();
        }


        // The server will listen for messages
        private void listen() {
            string received_data;
            byte[] receive_byte_array;
            UdpClient listener = new UdpClient(port);
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, port);
            

            while(true) {

                // Server waits for message
                //lstConsole.Items.Add("Waiting");
                Console.WriteLine("Waiting for messages.");
                receive_byte_array = listener.Receive(ref groupEP);

                // Server has receieved message
                Console.WriteLine("Received a broadcast from {0}", groupEP.ToString());
                received_data = Encoding.ASCII.GetString(receive_byte_array, 0, receive_byte_array.Length);
                
                Message m = decodeMessage(receive_byte_array);
                

                switch(m.type) {
                    case "SEND":
                        saveMessage(m);
                        break;
                    case "GET":
                        sendMessages(m.source, groupEP.Address);
                        break;
                    case "ACK":
                        deleteMessage(m.source, m.sequenceNumber);
                        break;
                }

                Console.WriteLine("data follows \n{0}\n\n", received_data);
            }
        }


        // Gets the decoded message
        private Message decodeMessage(byte[] msg) {
            Message m = new Message();
            string str = Encoding.Default.GetString(msg);
            string[] contents = str.Split('^');

            m.sequenceNumber = Convert.ToInt32(contents[0]);
            m.type = contents[1];
            m.source = contents[2];
            m.destination = contents[3];
            m.payload = contents[4];
            
            return m;
        }


        // Saves a message
        private void saveMessage(Message m) {
            messages.Add(m);
        }


        // Sends messages to a user
        private void sendMessages(string user, IPAddress ipAddr) {

            Socket sending_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint sending_end_point = new IPEndPoint(ipAddr, port);

            
            // Get and send each message for a specific user
            foreach(Message msg in getMessages(user)) {
                byte[] packet = Encoding.ASCII.GetBytes(msg.sequenceNumber + "^" + msg.type + "^" + msg.source + "^" + msg.destination + "^" + msg.payload);

                try {
                    sending_socket.SendTo(packet, sending_end_point);
                }
                catch (Exception send_exception) {
                    Console.WriteLine(" Exception {0}", send_exception.Message);
                }
            }
        }


        // Get a list of all messages for a user
        private List<Message> getMessages(string user) {
            List<Message> msgs = new List<Message>();

            foreach (Message m in messages) {
                if(m.destination == user) {
                    msgs.Add(m);
                }
            }

            return msgs;
        }


        // Deletes a message
        private void deleteMessage(string user, int seqNumber) {
            for(int i = 0; i < messages.Count; i++) {
                if(messages[i].destination == user && messages[i].sequenceNumber == seqNumber) {
                    messages.RemoveAt(i);
                }
            }
        }


        private void timer1_Tick(object sender, EventArgs e) {
            lstConsole.Items.Clear();

            foreach(Message m in messages) {
                string msg = "[" + m.sequenceNumber +
                             "] Type=" + m.type +
                             ", Src=" + m.source +
                             ", Dest=" + m.destination +
                             ", Payload=" + m.payload;
                lstConsole.Items.Add(msg);
            }
        }
    }
}
