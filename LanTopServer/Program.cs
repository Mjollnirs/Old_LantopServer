using LanTopServer.Models;
using Library.DataBase;
using SuperSocket.SocketEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Configuration;

namespace LanTopServer
{
    class Program
    {
        public static ICommandLoader cmdloader;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            try
            {
                var bootstrap = BootstrapFactory.CreateBootstrap();
                if (!bootstrap.Initialize())
                {
                    Program.WriteLine("Failed to initialize!");
                }
                cmdloader = new ICommandLoader(bootstrap);
            }
            catch (Exception ex)
            {
                Program.WriteLine(ex.Message);
                Console.ReadLine();
            }
            while (true)
            {
                try
                {
                    Console.Write("{0} > ", cmdloader.User);
                    string cmd = Console.ReadLine();
                    string[] tmp = cmd.Split(' ');
                    ArrayList tmpA = new ArrayList();

                    for (int i = 0, j = 0; i < tmp.Length; i++)
                    {
                        if (i == 0 && !tmp[i].Trim().Equals(""))
                        {
                            cmd = tmp[0];
                            continue;
                        }
                        if (!tmp[i].Trim().Equals(""))
                        {
                            tmpA.Add(tmp[i]);
                            j++;
                        }
                    }
                    Type op = cmdloader.GetType();
                    MethodInfo Command = op.GetMethod(cmd);
                    if (Command != null)
                    {
                        ParameterInfo[] parameter = Command.GetParameters();
                        if (parameter.Length == tmpA.Count)
                        {
                            object[] Args = new object[parameter.Length];
                            for (int i = 0; i < parameter.Length; i++)
                            {
                                //ParameterInfo t = parameter[i];
                                Args[i] = tmpA[i];
                            }
                            try
                            {
                                Command.Invoke(cmdloader, Args);
                            }
                            catch (Exception ex)
                            {
                                Program.WriteLine(ex.Message);
                            }
                        }
                        else
                        {
                            Console.Write("Args Error : {0} ", cmd);
                            for (int i = 0; i < parameter.Length; i++)
                            {
                                Console.Write("{0} ", parameter[i].Name);
                            }
                            Console.Write('\n');
                        }
                    }
                    else
                    {
                        Program.WriteLine("Unknow request\nUseing help\n");
                        Command = op.GetMethod("help");
                        Command.Invoke(cmdloader, new object[0]);
                    }
                }
                catch (Exception ex)
                {
                    Program.WriteLine(ex.Message, true);
                    Console.ReadLine();
                }
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(((Exception)e.ExceptionObject).Message);
        }

        public static void WriteLine(string format, params object[] arg)
        {
            Console.Write('\n');
            Console.WriteLine(format, arg);
            LogHelper.WriteLog(typeof(Program), String.Format(format, arg));
            if (cmdloader != null)
            {
                Console.Write("{0} > ", cmdloader.User);
            }
        }

        public static void WriteLineC(string format, params object[] arg)
        {
            Console.WriteLine(format, arg);
            LogHelper.WriteLog(typeof(Program), String.Format(format, arg));
        }

        internal static void BackupDB()
        {
            using (DbBackuper<LanTopEntities> helper = new DbBackuper<LanTopEntities>(
                new LanTopEntities(),
                new List<string> {}, ConfigurationManager.AppSettings["BackupDir"].Trim()))
            {
                helper.Backup();
            }
        }
    }
}
