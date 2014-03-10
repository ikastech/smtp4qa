using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using Rnwood.Smtp4dev;
using Message = Rnwood.SmtpServer.Message;
using System.IO;
using System.Configuration;
using Rnwood.SmtpServer;
using System.CodeDom.Compiler;


namespace Smtp4qaService
{
    public partial class Smtp4qaService : ServiceBase
    {
        private Server _server;

        public Smtp4qaService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            StartServer();
        }

        protected override void OnStop()
        {
            StopServer();
        }
        private void StartServer()
        {
            new Thread(ServerWork).Start();
        }

        private void ServerWork()
        {
            try
            {
               // Application.DoEvents();

                ServerBehaviour b = new ServerBehaviour();
                b.MessageReceived += OnMessageReceived;
                //b.SessionCompleted += OnSessionCompleted;

                _server = new Server(b);
                _server.Run();
            }
            catch (Exception exception)
            {
                using (StreamWriter newTask = new StreamWriter(ConfigurationManager.AppSettings["EmailFolderPath"]+"error.txt", false))
                {
                    newTask.WriteLine(exception.ToString());
                }
            }
        }



        private void OnMessageReceived(object sender, MessageEventArgs e)
        {
            MessageViewModel message = new MessageViewModel(e.Message);
            FileInfo msgFile1 = new FileInfo(ConfigurationManager.AppSettings["EmailFolderPath"] + message.To + "_" + System.Guid.NewGuid() + ".eml");
            message.SaveToFile(msgFile1);
        }

        private void StopServer()
        {
            if (_server.IsRunning)
            {
                _server.Stop();
            }
        }
    }
}
