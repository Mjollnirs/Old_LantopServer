using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace LanTopServer
{
    class Session : AppSession<Session>
    {
        public Boolean isAdmin { get; set; }
        public Process CmdProcess = new Process();
        protected override void OnSessionStarted()
        {
            //设定程序名 
            CmdProcess.StartInfo.FileName = "cmd.exe";
            //关闭Shell的使用 
            CmdProcess.StartInfo.UseShellExecute = false;
            //重定向标准输入 
            CmdProcess.StartInfo.RedirectStandardInput = true;
            //重定向标准输出 
            CmdProcess.StartInfo.RedirectStandardOutput = true;
            //重定向错误输出 
            CmdProcess.StartInfo.RedirectStandardError = true;
            //设置不显示窗口 
            //CmdProcess.StartInfo.CreateNoWindow = true;
            CmdProcess.Start();
            CmdProcess.OutputDataReceived += new DataReceivedEventHandler(CmdProcess_OutputDataReceived);
            this.Send("LanTopServer:");
        }

        private void CmdProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                this.Send(e.Data);
            }
        }

        protected override void HandleException(Exception e)
        {
            this.Send("Application error: {0}", e.Message);
        }

        protected override void OnSessionClosed(CloseReason reason)
        {
            CmdProcess.Close();
            //add you logics which will be executed after the session is closed
            base.OnSessionClosed(reason);
        }
    }
}
