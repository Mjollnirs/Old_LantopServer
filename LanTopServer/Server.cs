using LanTopServer.Models;
using Library.Json;
using Library.Net.Message.LanTop;
using Library.Net.Message.ShortMessage;
using Library.Net.Message.Tencent.SmartQQ;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace LanTopServer
{
    class Server : AppServer<Session>
    {

        /// <summary>
        /// 发送线程
        /// </summary>
        protected Thread ASyncSendMessage;

        /// <summary>
        /// 发送队列
        /// </summary>
        protected Queue<Message> MessageQueue;
        /// <summary>
        /// 获取队列线程
        /// </summary>
        protected Thread ASyncGetQueue;
        /// <summary>
        /// 自动审核线程
        /// </summary>
        protected Thread AutoVerify;

        /// <summary>
        /// 备份线程
        /// </summary>
        protected Thread BackupWorking;

        protected void BackupWorkingVoid()
        {
            bool t = false;
            while (true)
            {
                if (DateTime.Now.Hour == 0 && !t)
                {
                    Program.WriteLineC("Start Auto BackUp...");
                    Program.BackupDB();
                    t = true;
                    Thread.Sleep(7200000);
                }

                if (DateTime.Now.Hour != 0)
                {
                    t = false;
                    Thread.Sleep(900000);
                }
            }
        }

        /// <summary>
        /// XX客户端
        /// </summary>
        protected LanTopClient<Message, Receive, Blacklist> LanTop;

        /// <summary>
        /// QQ客户端
        /// </summary>
        protected Client QQClient;

        protected JsonHelper JsonHelper;
        public static Dictionary<String, String> Configs = new Dictionary<string, string>();
        public Dictionary<string, SuffrageVerify> QQVerify = new Dictionary<string, SuffrageVerify>();

        protected override bool Setup(IRootConfig rootConfig, IServerConfig config)
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 99999999;
            return base.Setup(rootConfig, config);
        }

        public override bool Start()
        {
            Program.WriteLineC("Initialize Library...");
            try
            {
                Library.Bootstrap.Initialize(AppDomain.CurrentDomain.BaseDirectory + "license.cer");
            }
            catch (Library.Exception.LicenseException ex)
            {
                Program.WriteLineC($"许可证无效！{ex.Message}");
                throw ex;
            }

            Program.WriteLineC("Load Config...");
            Server.Configs = new Dictionary<string, string>();
            using (LanTopEntities db = new LanTopEntities())
            {
                var datas = (from u in db.Configs
                             select u).ToArray();
                foreach (var item in datas)
                {
                    Server.Configs.Add(item.Key, item.Value);
                }
            }
            Program.WriteLineC("Config Load Success!");

            this.MessageQueue = new Queue<Message>();
            this.JsonHelper = new JsonHelper();

            this.LanTop = new LanTopClient<Message, Receive, Blacklist>(Configs["LanTopUserName"], Configs["LanTopUserPassword"]);
            this.LanTop.SumbitSuccess += LanTop_SumbitSuccess;
            this.LanTop.SendSuccess += LanTop_SendSuccess;
            this.LanTop.Receive += LanTop_Receive;
            if (this.LanTop.IsLogin)
            {
                Program.WriteLine("{0} {1}", DateTime.Now, "LanTop Login Success!");
            }
            else
            {
                Program.WriteLine("{0} {1}", DateTime.Now, "LanTop Login Faild!");
                throw new Exception("LanTop Login Faild!");
            }

            QQClient = new Client();
            //QQ客户端启动
            QQClient.LoginQRCodeChange += QQClient_LoginQRCodeChange;
            QQClient.LoginSuccess += QQClient_LoginSuccess;
            QQClient.ReceiveMessage += QQClient_ReceiveMessage;
            QQClient.Disconnected += QQClient_Disconnected;
            QQClient.QRCodeStatusChange += QQClient_QRCodeStatusChange;
            QQClient.SendMessageFailed += QQClient_SendMessageFailed; 
            QQClient.Login();//开始登录

            return base.Start();
        }

        /// <summary>
        /// 消息发送失败
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ex"></param>
        private void QQClient_SendMessageFailed(Client sender, Exception ex)
        {
            Program.WriteLine($"{DateTime.Now} {ex.Message} {ex.StackTrace}");
        }

        /// <summary>
        /// 登录二维码变化时间
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="status"></param>
        private void QQClient_QRCodeStatusChange(Client sender, QRCodeStatus status)
        {
            Program.WriteLine("{0}:{1}", DateTime.Now, $"{status.ToString()}");
        }

        /// <summary>
        /// QQ断开连接处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void QQClient_Disconnected(Client sender, string message)
        {
            Program.WriteLine("{0} QQClient_Disconnected:{1}", DateTime.Now, message);
        }

        /// <summary>
        /// QQ收到消息处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void QQClient_ReceiveMessage(Client sender, ReceiveMessageObject message)
        {
            if (message.Content.Trim().ToLower().Equals("#status"))
            {
                sender.Send(message.Type, message.FromId, $"Online:{this.StartedTime}");
            }
            string[] _list = Configs["QQClientQun"].Split(',');
            if (message.Type == MessageType.Group)
            {
                GroupInfo info = (GroupInfo)message.FromObject;
                GroupInfo.MemberInfo minfo = (GroupInfo.MemberInfo)message.SendObject;
                if (
                    _list.Contains($"{info.name}:{info.createtime}")
                    && message.Content.Trim().ToUpper().StartsWith("V")
                    && (
                    message.Content.Trim().EndsWith("+1") ||
                    message.Content.Trim().EndsWith("-1") ||
                    message.Content.Trim().EndsWith("+2") ||
                    message.Content.Trim().EndsWith("-2")
                    ))
                {
                    string value = message.Content.Trim().ToUpper();
                    string id = value.Replace("V", "").Split('+', '-')[0];
                    long long_id = 0;
                    if (!long.TryParse(id, out long_id))
                    {
                        return;
                    }

                    if (!QQVerify.Keys.Contains(id))
                    {
                        QQVerify[id] = new SuffrageVerify();
                    }
                    SuffrageVerify verify = QQVerify[id];

                    using (LanTopEntities db = new LanTopEntities())
                    {
                        int count = (from u in db.Messages
                                     where u.IsVerify == false && u.IsSubmit == false && u.IsSend == false && u.IsQueue == false
                                     && u.Id == long_id
                                     select u).Count();

                        if (count == 0)
                        {
                            sender.Send(MessageType.Group, message.FromId, $"未找到ID为ME{long_id}的待审核短信！");
                            QQVerify.Remove(id);
                            return;
                        }
                    }

                    int isVerify = 0;

                    if (value.EndsWith("+1") ||
                    value.EndsWith("-1"))
                    {
                        if (value.EndsWith("+1"))
                        {
                            if (verify.AddPassed(message.SendRealQQ))
                            {
                                Program.WriteLine("{0} {1}", DateTime.Now, $"QQ:{message.SendRealQQ} ME{long_id} Verify+1");
                            }
                        }
                        else if (value.EndsWith("-1"))
                        {
                            if (verify.AddDenied(message.SendRealQQ))
                            {
                                Program.WriteLine("{0} {1}", DateTime.Now, $"QQ:{message.SendRealQQ} ME{long_id} Verify-1");
                            }
                        }

                        if (verify.PassedCount >= 3)
                        {
                            isVerify = 1;
                        }
                        else if (verify.DeniedCount >= 3)
                        {
                            isVerify = -1;
                        }
                    }
                    else if ((value.EndsWith("+2") ||
                    value.EndsWith("-2")) && (
                    minfo.isManager || message.SendId.Equals(info.owner) //管理员或群主
                    ))
                    {
                        if (value.EndsWith("+2"))
                        {
                            verify.AddPassed(message.SendRealQQ);
                            isVerify = 1;
                            Program.WriteLine("{0} {1}", DateTime.Now, $"QQ:{message.SendRealQQ} ME{long_id} Verify+2");
                        }
                        else if (value.EndsWith("-2"))
                        {
                            verify.AddDenied(message.SendRealQQ);
                            isVerify = -1;
                            Program.WriteLine("{0} {1}", DateTime.Now, $"QQ:{message.SendRealQQ} ME{long_id} Verify-2");
                        }
                    }

                    if (isVerify == 0)
                    {
                        sender.Send(MessageType.Group,
                            message.FromId,
                            $"短信ME{long_id}\r\n已通过人数为[{QQVerify[id].PassedCount}],\r\n不通过人数为[{QQVerify[id].DeniedCount}],\r\n为[待通过]状态！");
                    }
                    else if (isVerify == 1)
                    {
                        sender.Send(MessageType.Group,
                            message.FromId,
                            $"短信ME{long_id}\r\n已通过人数为[{QQVerify[id].PassedCount}],\r\n不通过人数为[{QQVerify[id].DeniedCount}],\r\n为[已通过]状态！");
                        using (LanTopEntities db = new LanTopEntities())
                        {
                            var me = (from u in db.Messages
                                      where u.Id == long_id
                                      select u).First();
                            me.IsVerify = true;
                            me.VerifyTime = DateTime.Now;
                            me.Auditor = "suffrage-verify";

                            db.SaveChanges();
                        }
                        Program.WriteLine("{0} {1}", DateTime.Now, $"ME{long_id} suffrage-verify allow");
                    }
                    else if (isVerify == -1)
                    {
                        sender.Send(MessageType.Group,
                            message.FromId,
                            $"短信ME{long_id}\r\n已通过人数为[{QQVerify[id].PassedCount}],\r\n不通过人数为[{QQVerify[id].DeniedCount}],\r\n为[不通过]状态！");

                        using (LanTopEntities db = new LanTopEntities())
                        {
                            var me = (from u in db.Messages
                                      where u.Id == long_id
                                      select u).First();
                            me.IsVerify = false;
                            me.IsSubmit = true;
                            me.VerifyTime = DateTime.Now;
                            me.Auditor = "suffrage-verify";

                            db.SaveChanges();
                        }

                        Program.WriteLine("{0} {1}", DateTime.Now, $"ME{long_id} suffrage-verify deny");
                    }
                }
            }
        }

        /// <summary>
        /// QQ登录成功处理
        /// </summary>
        /// <param name="sender"></param>
        private void QQClient_LoginSuccess(Client sender)
        {
            Program.WriteLineC("{0} {1}", DateTime.Now, "QQClient Login Success!");
            if (!this.AutoVerify.IsAlive)
            {
                this.AutoVerify.Start();
                Program.WriteLineC("{0} {1}", DateTime.Now, "AutoVerify Start!");
            }

            foreach (var item in sender.GroupList)
            {
                Program.WriteLine("{0}:{1}", item.Value.name, item.Value.createtime);
            }
        }

        /// <summary>
        /// QQ登录二维码处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="qrCode"></param>
        private void QQClient_LoginQRCodeChange(Client sender, System.Drawing.Image qrCode)
        {
            qrCode.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QQ.png"));
        }

        protected override void OnStarted()
        {
            base.OnStarted();
            this.ASyncSendMessage = new Thread(AsyncSend);
            this.ASyncGetQueue = new Thread(ASyncQueue);
            this.ASyncGetQueue.Start();
            Thread.Sleep(10000);
            this.ASyncSendMessage.Start();

            this.AutoVerify = new Thread(AsyncAutoVerify);
            if (!Configs["NeedQQClient"].Equals("true"))
            {
                this.AutoVerify.Start();
                Program.WriteLineC("{0} {1}", DateTime.Now, "AutoVerify Start!");
            }

            this.BackupWorking = new Thread(new ThreadStart(BackupWorkingVoid));
            this.BackupWorking.Start();
            Program.BackupDB();
        }

        /// <summary>
        /// 自动审核处理
        /// </summary>
        private void AsyncAutoVerify()
        {
            while (true)
            {
                DateTime Now = DateTime.Now;
                DateTime StartTime;
                DateTime EndTime;
                try
                {
                    StartTime = DateTime.Parse(Server.Configs["AutoVerifyStartTime"]);
                    EndTime = DateTime.Parse(Server.Configs["AutoVerifyEndTime"]);

                    if (!IsInTimeInterval(Now, StartTime, EndTime))
                    {
                        Program.WriteLine("AutoVerify Stoped!");
                        Thread.Sleep(60000);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLog(typeof(Server), ex);
                }

                using (LanTopEntities db = new LanTopEntities())
                {
                    var list = (from u in db.Messages
                                where u.IsVerify == false && u.IsSubmit == false && u.IsSend == false && u.IsQueue == false
                                select u).ToArray();
                    foreach (var item in list)
                    {
                        try
                        {
                            Permission p = (from u in db.Permissions
                                            where u.username == item.User
                                            select u).First();
                            var keyword = (from u in db.Keywords
                                           select u).ToArray();

                            //关键字校验
                            Boolean noKeyWord = true;
                            foreach (var key in keyword)
                            {
                                if (item.Content.Contains(key.Content))
                                {
                                    noKeyWord = false;
                                }
                            }

                            //权限校验
                            if (p.permissions != 0 && noKeyWord)
                            {
                                Program.WriteLine("{0} ID:{1}|User:{2} AutoVerify allow", DateTime.Now, item.Id, item.User);
                                item.IsVerify = true;
                                item.VerifyTime = DateTime.Now;
                            }
                            else if (Server.Configs.ContainsKey("VerifyUserList"))
                            {
                                String m = "";

                                String _key = String.Format("用户{0}在{1}提交的短信ME{2}", item.User, item.SubTime, item.Id);
                                bool needSend = false;
                                var d = (from u in db.Messages
                                         where u.IsVerify == true && u.User.Equals("System") && u.Content.Contains(_key)
                                         orderby u.SubTime descending
                                         select u);

                                if (d.Count() == 0)
                                {
                                    needSend = true;
                                    m = String.Format("[系统提示]用户{0}在{1}提交的短信ME{2}需要人工审核/学生会短信平台", item.User, item.SubTime, item.Id);

                                    Program.WriteLine("{0} ID:{1}|User:{2} AutoVerify deny", DateTime.Now, item.Id, item.User);
                                }
                                else
                                {
                                    var _d = d.ToList();
                                    Message first = _d.First();
                                    //计算前一条提醒短信距离当前的时间差
                                    TimeSpan ts = DateTime.Now - first.SubTime;
                                    if (ts.Hours >= 1)
                                    {
                                        needSend = true;

                                        int Hours = 1;
                                        //如果不是第二次提醒
                                        if (d.Count() != 1)
                                        {
                                            Message last = _d.Last();
                                            TimeSpan ds = DateTime.Now - last.SubTime;
                                            Hours = ds.Hours;
                                        }
                                        m = String.Format("[系统提示]用户{0}在{1}提交的短信ME{2}已超过{3}小时未审核，请及时审核/学生会短信平台", item.User, item.SubTime, item.Id, Hours);

                                        Program.WriteLine("{0} ID:{1}|User:{2} AutoVerify deny, {3} Hours Warning", DateTime.Now, item.Id, item.User, Hours);
                                    }
                                }

                                if (needSend)
                                {
                                    Message SystemMessage = new Message()
                                    {
                                        User = "System",
                                        Auditor = "System",
                                        IsVerify = true,
                                        SubTime = DateTime.Now,
                                        VerifyTime = DateTime.Now,
                                        Content = m,
                                        Recipients = Server.Configs["VerifyUserList"]
                                    };
                                    if (Server.Configs["VerifyUserList"].Equals(""))//如果为空
                                    {
                                        SystemMessage.IsSend = true;
                                    }
                                    db.Messages.Add(SystemMessage);

                                    if (QQClient.IsOnline)//QQ在线处理
                                    {
                                        string[] _list = Configs["QQClientQun"].Split(',');

                                        foreach (var _item in QQClient.GroupList)
                                        {
                                            if (_list.Contains($"{_item.Value.name}:{_item.Value.createtime}"))
                                            {
                                                QQClient.Send(
                                                    MessageType.Group,
                                                    _item.Key,
                                                    $"{m}\r\n短信内容：{item.Content}\r\n请回复[V{item.Id}+1]通过，[V{item.Id}-1]不通过\r\n管理员回复[V{item.Id}+2]直接通过，[V{item.Id}-2]直接不通过");
                                            }
                                        }
                                    }
                                }
                            }
                            db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            LogHelper.WriteLog(typeof(Server), ex);
                        }
                    }
                }
                Thread.Sleep(5000);
            }
        }

        /// <summary>
        /// 发送成功处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arg"></param>
        private void LanTop_SendSuccess(object sender, object[] arg)
        {
            long id = (long)arg[0];
            using (LanTopEntities db = new LanTopEntities())
            {
                Message message = (from u in db.Messages
                                   where u.Id == id
                                   select u).First();
                Program.WriteLine("SendSuccess {0} ID:{1} User:{2}", DateTime.Now, message.Id, message.User);
                message.IsSend = true;
                db.SaveChanges();
            }
        }

        /// <summary>
        /// 提交成功处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arg"></param>
        private void LanTop_SumbitSuccess(object sender, object[] arg)
        {
            long id = (long)arg[0];
            using (LanTopEntities db = new LanTopEntities())
            {
                Message message = (from u in db.Messages
                                   where u.Id == id
                                   select u).First();
                Program.WriteLine("SumbitSuccess {0} ID:{1} User:{2}", DateTime.Now, message.Id, message.User);
                message.IsSubmit = true;
                db.SaveChanges();
            }
        }

        /// <summary>
        /// 收到回复处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arg"></param>
        private void LanTop_Receive(object sender, IReceive arg)
        {
            using (LanTopEntities db = new LanTopEntities())
            {
                int t = (from u in db.Receives
                         where u.Id == arg.Id
                         select u).Count();
                if (t == 0)
                {
                    db.Receives.Add((Receive)arg);
                    Program.WriteLine("Catch Receives: {0} {1} {2} {3}", arg.Id, arg.Phone, arg.Content, arg.Time);
                    db.SaveChanges();
                }
            }
        }

        /// <summary>
        /// 获取队列线程
        /// </summary>
        /// <param name="obj"></param>
        private void ASyncQueue(object obj)
        {
            while (true)
            {
                lock (this.MessageQueue)
                {
                    using (LanTopEntities db = new LanTopEntities())
                    {
                        var datas = (from u in db.Messages
                                     where u.IsVerify == true && u.IsSend == false && u.IsQueue == false
                                     select u).ToArray();
                        for (int i = 0; i < datas.Length; i++)
                        {
                            this.MessageQueue.Enqueue(datas[i]);
                            datas[i].IsQueue = true;
                            db.SaveChanges();
                        }
                    }
                }
                Thread.Sleep(5000);
            }
        }

        /// <summary>
        /// 发送短信线程
        /// </summary>
        /// <param name="obj"></param>
        private void AsyncSend()
        {
            while (true)
            {
                Thread.Sleep(6000);
                lock (this.MessageQueue)
                {
                    using (LanTopEntities db = new LanTopEntities())
                    {
                        for (int i = 0; i < this.MessageQueue.Count; i++)
                        {
                            Thread.Sleep(1000);
                            Message tmp = this.MessageQueue.Dequeue();
                            Message message = (from u in db.Messages
                                               where u.Id == tmp.Id
                                               select u).First();
                            message.SendTime = DateTime.Now;
                            Program.WriteLine("Try Send {0} {1} {2} {3}", new object[] { message.User, message.Content, message.SubTime, message.SendTime });

                            //BlackList
                            var blacklist = (from u in db.Blacklists
                                             select u).ToArray();
                            List<string> rec = message.Recipients.Split(',').ToList();
                            List<string> irec = message.Recipients.Split(',').ToList();
                            foreach (var item in blacklist)
                            {
                                foreach (var t in irec)
                                {
                                    if (item.Phone.Equals(t))
                                    {
                                        Program.WriteLine("BlackList Attack {0}|{1}", item.Name, item.Phone);
                                        rec.Remove(t);
                                    }
                                }
                            }
                            String[] mulsel = rec.ToArray();

                            //Send
                            LanTop.Send(message.Content, mulsel);
                            LanTop.Check(message, blacklist.ToList<IBlacklist>());
                            db.SaveChanges();
                        }
                    }
                }
            }
        }

        protected override void OnStopped()
        {
            base.OnStopped();
            this.AutoVerify.Abort();
            this.ASyncGetQueue.Abort();
            this.ASyncSendMessage.Abort();
            this.BackupWorking.Abort();
            this.LanTop.Dispose();
            using (LanTopEntities db = new LanTopEntities())
            {
                while (this.MessageQueue.Count != 0)
                {
                    lock (this.MessageQueue)
                    {
                        Message item = this.MessageQueue.Dequeue();
                        Message q = (from u in db.Messages
                                     where u.Id == item.Id
                                     select u).First();
                        q.IsQueue = false;
                        db.SaveChanges();
                    }
                }
            }
        }

        protected override void ExecuteCommand(Session session, SuperSocket.SocketBase.Protocol.StringRequestInfo requestInfo)
        {
            //base.ExecuteCommand(session, requestInfo);
            CommandLoader Loader = new CommandLoader(session, requestInfo);
            Type lo = Loader.GetType();
            MethodInfo Command = lo.GetMethod(requestInfo.Key.Trim());
            if (Command != null)
            {
                Command.Invoke(Loader, new object[0]);
            }
            else
            {
                throw new Exception("Unknow request");
            }
        }

        /// <summary>
        /// 判断是否在某个时间段内
        /// </summary>
        /// <param name="time"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public static bool IsInTimeInterval(DateTime time, DateTime startTime, DateTime endTime)
        {
            //判断时间段开始时间是否小于时间段结束时间，如果不是就交换
            if (startTime > endTime)
            {
                DateTime tempTime = startTime;
                startTime = endTime;
                endTime = tempTime;
            }

            //获取以公元元年元旦日时间为基础的新判断时间
            DateTime newTime = new DateTime();
            newTime = newTime.AddHours(time.Hour);
            newTime = newTime.AddMinutes(time.Minute);
            newTime = newTime.AddSeconds(time.Second);

            //获取以公元元年元旦日时间为基础的区间开始时间
            DateTime newStartTime = new DateTime();
            newStartTime = newStartTime.AddHours(startTime.Hour);
            newStartTime = newStartTime.AddMinutes(startTime.Minute);
            newStartTime = newStartTime.AddSeconds(startTime.Second);

            //获取以公元元年元旦日时间为基础的区间结束时间
            DateTime newEndTime = new DateTime();
            if (startTime.Hour > endTime.Hour)
            {
                newEndTime = newEndTime.AddDays(1);
            }
            newEndTime = newEndTime.AddHours(endTime.Hour);
            newEndTime = newEndTime.AddMinutes(endTime.Minute);
            newEndTime = newEndTime.AddSeconds(endTime.Second);

            if (newTime > newStartTime && newTime < newEndTime)
            {
                return true;
            }
            return false;
        }
    }
}
