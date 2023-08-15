using System;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Threading;
using HslCommunication.Profinet.Keyence;
using HslCommunication;
using System.Drawing;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        KeyEnceMc PLC_Client;//与基恩士plc连接Client
        Thread receiveTodo_thread;//监听到值的线程
        public Form1()
        {
            InitializeComponent();
            InItPort();
        }
        
        private void Form1_Load(object sender, EventArgs e)
        {
           // InItPort();
        }

        private void InItPort()
        {
            String[] portnames = SerialPort.GetPortNames();
            if (portnames.Length >0) 
            {
                foreach (var item in portnames)
                {
                    comboBox1.Items.Add(item);
                }
                //默认选择COM4
                if (portnames.Length >= 4)
                    comboBox1.SelectedIndex = 3;
                else
                    comboBox1.SelectedIndex = 0;
            }
        }

        #region 事件处理
        /// <summary>
        /// 串口接收数据触发
        /// </summary>
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string receive = "";//数据接收
            try
            {
                Thread.Sleep(50);  //（毫秒）等待一定时间，确保数据的完整性 int len        
                int len = serialPort1.BytesToRead;
                if (len != 0)
                {
                    byte[] buff = new byte[len];
                    serialPort1.Read(buff, 0, len);
                    receive = Encoding.Default.GetString(buff);//数据接收内容
                    string[] arr = receive.Split(',');
                    string[] arr2 = arr[0].Split('E');
                    string[] arr3 = arr[1].Split('E');
                    PLC_Client.Write("DM1200", arr2[0]);
                    PLC_Client.Write("DM1202", arr3[0]);
                    PLC_Client.Write("DM244", "0");

                    LogHelper("内阻电压数据接收成功：" + receive);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                //日志记录
                LogHelper("接收数据出错：" + ex.Message);
                return;
            }
        }
        /// <summary>
        /// 开启通信/关闭通信
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (button1.Text == "开启通信")
                {
                    PLC_Client = new KeyEnceMc(txtIP.Text, Convert.ToInt32(txtPort.Text));
                    PLC_Client.Connect();
                    if (PLC_Client.ConnectMessage == "连接成功")
                    {
                        ToolStripStatusByListen("连接成功");
                        //线程开启
                        CloseOrOpenThread(true);
                        button1.Text = "停止通信";
                        button1.BackColor = Color.Red;
                        serialPort1.PortName = comboBox1.Text;
                        serialPort1.Open();//打开串口
                        SerialPortWrite(":TRIG:SOUR IMM\r\n");
                        SerialPortWrite(":INIT:CONT ON\r\n");
                        MessageBox.Show(PLC_Client.ConnectMessage);
                    }
                    else
                    {
                        MessageBox.Show(PLC_Client.ConnectMessage);
                        LogHelper("连接失败");
                    }
                }
                else
                {
                    button1.Text = "开启通信";
                    button1.BackColor = Color.Green;
                    ToolStripStatusByListen("通信异常");
                    CloseOrOpenThread(false);
                }
            }
            catch (Exception ex) 
            {
                ToolStripStatusByListen("通信异常");
                MessageBox.Show(PLC_Client.ConnectMessage);
                LogHelper(ex.Message);
            }
            

        }
        /// <summary>
        /// 开启/关闭线程
        /// </summary>
        /// <param name="status">开启true/关闭false</param>
        private void CloseOrOpenThread(bool status)
        {
            if (status)
            {
                receiveTodo_thread = new Thread(ReceiveMessageToDo);
                receiveTodo_thread.IsBackground = true;
                receiveTodo_thread.Start();
            }
            else
            {
                if (receiveTodo_thread!=null) 
                {
                    receiveTodo_thread.Abort();
                }
                serialPort1.Dispose();
                PLC_Client.DisPose();
            }
        }
        /// <summary>
        /// 状态控件
        /// </summary>
        public void ToolStripStatusByListen(string msg)
        {
            this.Invoke(new Action(()=> 
            {
                if (msg == "连接成功")
                {
                    toolStripStatusLabel1.Text = "通信正常";
                    toolStripStatusLabel1.BackColor = Color.Green;
                }
                else
                {
                    toolStripStatusLabel1.Text = msg;
                    toolStripStatusLabel1.BackColor = Color.Red;
                }
            }));
            
        }
        /// <summary>
        /// 写入串口
        /// </summary>
        /// <param name="message">消息</param>
        public void SerialPortWrite(object message)
        {
            try
            {
                serialPort1.Write(message.ToString());
                LogHelper("已发送字符串：" + message.ToString());
            }
            catch (Exception ex)
            {
                LogHelper("错误提示：" + ex.Message);
            }
        }
        /// <summary>
        /// 监听到值执行的线程
        /// </summary>
        private void ReceiveMessageToDo()
        {
            try
            {
                while (true)
                {
                    //1.心跳
                    //PLC_Client.Establish();
                    //2.接收指令
                    PLC_Client.Received();
                    //3.收到指令
                    //读取指令(1为开始命令)
                    if (!string.IsNullOrEmpty(PLC_Client.ReceiveMessage) && PLC_Client.ReceiveMessage == "1")
                    {
                        SerialPortWrite("FETCH?\r\n");
                    }
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                CloseOrOpenThread(false);
                LogHelper("执行线程时失败："+ ex.Message);
            }
        }
        #endregion

        #region Log
        /// <summary>
        /// 日志文件记录
        /// </summary>
        /// <param name="msg">写入信息</param>
        public void LogHelper(string content)
        {
            try
            {
                string filename = DateTime.Now.ToString("yyyyMMdd") + ".txt";
                string filePath = AppDomain.CurrentDomain.BaseDirectory + filename;
                FileInfo file = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + filename);
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now.ToString());
                sb.Append(" ");
                sb.Append(content);
                FileMode fm = new FileMode();
                if (!file.Exists)
                {
                    fm = FileMode.Create;
                }
                else
                {
                    fm = FileMode.Append;
                }

                using (FileStream fs = new FileStream(filePath, fm, FileAccess.Write, FileShare.Write))
                {
                    using (StreamWriter sw = new StreamWriter(fs, Encoding.Default))
                    {
                        sw.WriteLine(sb.ToString());
                        sw.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                //return;
            }
        }


        #endregion

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (receiveTodo_thread!=null) 
            {
                CloseOrOpenThread(false);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
