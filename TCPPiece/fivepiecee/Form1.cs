using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.VisualBasic.PowerPacks; //VB向量繪圖功能

namespace fivepiecee
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        Socket T; //通訊物件
        Thread Th; //監聽執行緒
        string User; //使用者
        ShapeContainer CVS; //畫布物件(棋盤)
        byte[,] S; //對應棋盤狀態的陣列 : 0為空格，1為黑子，2為白子
        //繪製棋盤與加入畫布

        private void Form1_Load(object sender, EventArgs e)
        {
            Bitmap bg = new Bitmap(570, 570); //棋盤影像物件
            Graphics g = Graphics.FromImage(bg); //棋盤影像繪圖物件
            g.Clear(Color.BurlyWood); //設定背景色
            for (int i = 15; i <= 555; i += 30)
            {
                g.DrawLine(Pens.Black, i, 15, i, 555);
            }//畫19條垂直線
            for (int j = 15; j <= 555; j += 30)
            {
                g.DrawLine(Pens.Black, 15, j, 555, j);
            }//畫19條水平線
            panel1.BackgroundImage = bg; //貼上棋盤影像為panel1的背景
            CVS = new ShapeContainer();
            panel1.Controls.Add(CVS);
            S = new byte[19, 19];
        }
        //登入
        private void button1_Click(object sender, EventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;

            User = textBox3.Text;
            string IP = textBox1.Text;
            int Port = int.Parse(textBox2.Text);
            try
            {
                IPEndPoint EP = new IPEndPoint(IPAddress.Parse(IP), Port);

                T = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                T.Connect(EP);
                Th = new Thread(Listen);
                Th.IsBackground = true;
                Th.Start();
                textBox4.Text = "已連線" + "\r\n"; Send("0" + User);
                button1.Enabled = false;
            }
            catch
            {
                textBox4.Text = "無法連線" + "\r\n";
            }
        }
        //清除重玩鍵
        private void button2_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex >= 0)
            {
                Send("5" + "C" + "|" + listBox1.SelectedItem);
            }
            CVS.Shapes.Clear();
            S = new byte[19, 19];
            panel1.Enabled = true;
        }

        //傳送訊息給伺服器
        private void Send(string Str)
        {
            byte[] B = Encoding.Default.GetBytes(Str);
            T.Send(B, 0, B.Length, SocketFlags.None);
        }

        //監聽
        private void Listen()
        {
            EndPoint ServerEP = (EndPoint)T.RemoteEndPoint;

            byte[] B = new byte[1023];
            int inLen = 0;
            string Msg;
            string St;
            string Str;
            while (true)
            {
                try
                {
                    inLen = T.ReceiveFrom(B, ref ServerEP);
                }
                catch (Exception)
                {
                    T.Close();
                    listBox1.Items.Clear();
                    MessageBox.Show("伺服器中斷");
                    button1.Enabled = true;
                    Th.Abort();
                }
                Msg = Encoding.Default.GetString(B, 0, inLen);

                St = Msg.Substring(0, 1);
                Str = Msg.Substring(1);
                switch (St)
                {
                    case "L":
                        listBox1.Items.Clear();
                        string[] M = Str.Split(',');
                        for (int i = 0; i < M.Length; i++) listBox1.Items.Add(M[i]);
                        break;
                    case "5":
                        CVS.Shapes.Clear();
                        S = new byte[19, 19];
                        panel1.Enabled = true;
                        break;
                    case "6":
                        string[] D = Str.Split(',');
                        int x = int.Parse(D[0]);
                        int y = int.Parse(D[1]);
                        Chess(x, y, Color.White);
                        S[x, y] = 2;
                        panel1.Enabled = true; //輪到你下棋
                        if (chk5(x, y, 2))
                        {
                            MessageBox.Show("你輸了"); //CHECK對手
                        }
                        break;
                }
            }
        }
        //畫棋子副程序
        private void Chess(int i, int j, Color BW)
        {
            OvalShape C = new OvalShape();
            C.Width = 26;
            C.Height = 26;
            C.Left = i * 30 + 2;
            C.Top = j * 30 + 2;
            C.FillStyle = FillStyle.Solid;
            C.FillColor = BW;
            C.Parent = CVS;
        }
        //下棋動作
        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            int i = e.X / 30;
            int j = e.Y / 30;
            if (S[i, j] == 0)
            {
                Chess(i, j, Color.Black);
                S[i, j] = 1;
                if (listBox1.SelectedIndex >= 0)
                {
                    Send("6" + i.ToString() + "," + j.ToString() + "|" + listBox1.SelectedItem);
                    panel1.Enabled = false;
                }
                if (chk5(i, j, 1)) MessageBox.Show("你贏了");
            }
        }
        //檢查是否五連線的程式
        private bool chk5(int i, int j, byte tg)
        {
            int n = 0;
            int ii, jj;
            for (int k = -4; k <= 4; k++)
            {
                ii = i + k;
                if (ii >= 0 && ii < 19)
                {
                    if (S[ii, j] == tg)
                    {
                        n += 1;
                        if (n == 5) return true;
                    }
                    else
                    {
                        n = 0;
                    }
                }
            }
            //垂直
            n = 0;
            for (int k = -4; k <= 4; k++)
            {
                jj = j + k;
                if (jj >= 0 && jj < 19)
                {
                    if (S[i, jj] == tg)
                    {
                        n += 1;
                        if (n == 5) return true;
                    }
                    else
                    {
                        n = 0;
                    }
                }
            }
            //左上至右下
            n = 0;
            for (int k = -4; k <= 4; k++)
            {
                ii = i + k; jj = j + k;
                if (ii >= 0 && ii < 19 && jj >= 0 && jj < 19)
                {
                    if (S[ii, jj] == tg)
                    {
                        n += 1;
                        if (n == 5) return true;
                    }
                    else
                    {
                        n = 0;
                    }
                }
            }
            //右上至左下
            n = 0;
            for (int k = -4; k <= 4; k++)
            {
                ii = i - k; jj = j + k;
                if (ii >= 0 && ii < 19 && jj >= 0 && jj < 19)
                {
                    if (S[ii, jj] == tg)
                    {
                        n += 1;
                        if (n == 5) return true;
                    }
                    else
                    {
                        n = 0;
                    }
                }
            
            }
            return false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (button1.Enabled == false)
            {
                Send("9" + User);
                T.Close();
            }
        }
    }
}