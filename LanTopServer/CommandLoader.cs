using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace LanTopServer
{
    class CommandLoader
    {
        private Session session;
        private StringRequestInfo info;

        public CommandLoader(Session session, StringRequestInfo requestInfo)
        {
            this.session = session;
            this.info = requestInfo;
        }

        public void Login()
        {
            Library.Security.Hash hash = new Library.Security.Hash();
            if (ConfigurationManager.AppSettings["Password"].Trim().Equals(hash.SHA256(info.Body.Trim())))
            {
                session.isAdmin = true;
                this.session.Send("Login Success!");
            } else
            {
                this.session.Send("Login Faild!");
            }
        }

        public void exec()
        {
            if (session.isAdmin)
            {
                try
                {
                    session.CmdProcess.BeginOutputReadLine();
                }
                catch (InvalidOperationException)
                {
                }
                finally
                {
                    session.CmdProcess.StandardInput.WriteLine(info.Body);
                }
            }
        }

        public void TT()
        {
            this.session.Send(this.info.Body);
        }
    }
}
