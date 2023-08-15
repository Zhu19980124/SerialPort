using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using HslCommunication.Profinet.Keyence;
using HslCommunication;

namespace WindowsFormsApp1
{
    class KeyEnceMc
    {
        #region 变量定义
        string Connectmessage;
        string Receivemessage;
        string ip;
        int port;
        public bool first = true;
        Form1 form = new Form1();
        //接收到的消息
        public string ConnectMessage { get { return Connectmessage; } set {; } }        //连接
        public string ReceiveMessage { get { return Receivemessage; } set {; } }        //读取
        public KeyEnceMc(string _ip, int _port)
        {
            ip = _ip;
            port = _port;
        }
        private KeyenceMcNet keyence_net;
        #endregion

        #region Connect
        public void Connect()
        {
            try
            {
                if (first)
                {
                    keyence_net = new KeyenceMcNet(ip, port);
                }
                OperateResult connect = keyence_net.ConnectServer();
                if (connect.IsSuccess)
                {
                    Connectmessage = "连接成功";
                    first = false;
                }
                else 
                {
                    Connectmessage = "IP/端口输入有误，开启失败！";
                }
                //form.ToolStripStatusByListen(Connectmessage);
            }
            catch (Exception ex)
            {
                Connectmessage = "IP/端口输入有误，开启失败！";
                //form.ToolStripStatusByListen(Connectmessage);
                if (keyence_net!=null)
                    keyence_net.Dispose();
            }
        }
        public void ConnectThread()
        {
            try
            {
                while (true)
                {
                    Connect();
                    Thread.Sleep(20);
                }
            }
            catch (Exception)
            {
                Connectmessage = "已断开与服务器的连接";
                //form.ToolStripStatusByListen(Connectmessage);
                keyence_net.Dispose();
            }
        }
        public void CloseEstablish()
        {
            try
            {
                keyence_net.Write("DM64", float.Parse("0"));
            }
            catch (Exception ex)
            {

            }
        }
        public void DisPose() 
        {
            keyence_net.Dispose();
        }
        #endregion

        #region Read
        /// <summary>
        /// 接收服务端返回的消息
        /// </summary>
        public void Received()
        {
            try
            {
                int[] result = keyence_net.ReadInt32("DM244", 1).Content;
                Receivemessage = result[0].ToString();
                Thread.Sleep(20);
            }
            catch (Exception ex)
            {
                //断连
                Connectmessage = "已断开与服务器的连接";
                //form.ToolStripStatusByListen(Connectmessage);
                keyence_net.Dispose();
            }
        }

        public void Establish()
        {
            try
            {
                int[] result = keyence_net.ReadInt32("DM62", 1).Content;
                if (result[0] == 0) 
                {
                    keyence_net.Write("DM62", float.Parse("1"));
                }
            }
            catch (Exception ex)
            {
                //断连
                Connectmessage = "已断开与服务器的连接";
                //form.ToolStripStatusByListen(Connectmessage);
                keyence_net.Dispose();
            }
        }
        #endregion

        #region Write
        /// 向服务器写入
        /// </summary>
        /// <param name="msg">消息</param>
        public bool Write(string type,string msg)
        {
            try
            {
                if (ConnectMessage == "连接成功")
                {
                    OperateResult op = keyence_net.Write(type, float.Parse(msg));
                    return op.IsSuccess;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        #endregion
    }
}
