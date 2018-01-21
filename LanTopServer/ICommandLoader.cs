using LanTopServer.Models;
using SuperSocket.SocketBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LanTopServer
{
    class ICommandLoader
    {
        private IBootstrap bootstrap;
        public string User = "sys";

        public ICommandLoader(IBootstrap bootstrap)
        {
            this.bootstrap = bootstrap;
        }

        public void start()
        {
            var result = bootstrap.Start();

            Console.WriteLine("Start result: {0}!", result);

            if (result == StartResult.Failed)
            {
                Console.WriteLine("Failed to start!");
            }
        }

        public void stop()
        {
            Console.WriteLine("Stop Server!");
            bootstrap.Stop();
        }

        public void restart()
        {
            this.stop();
            this.start();
        }

        public void clearMem()
        {
            System.GC.Collect(0, GCCollectionMode.Forced);
        }

        public void reload()
        {
            stop();
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
            Program.WriteLine("Config Load Success!");
        }

        public void License()
        {
            Console.WriteLine(Library.Bootstrap.GetLicenseCertificate());
        }

        public void Backup()
        {
            Program.BackupDB();
        }

        public void help()
        {
            Type op = this.GetType();
            MethodInfo[] Command = op.GetMethods();
            for (int i = 0; i < Command.Length; i++)
            {
                if (!Command[i].IsVirtual)
                    Console.WriteLine("    " + Command[i].Name);
            }
        }
    }
}
