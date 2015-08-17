﻿using FSO.Server.Database.DA;
using FSO.Server.Debug;
using FSO.Server.Servers;
using FSO.Server.Servers.Api;
using FSO.Server.Servers.City;
using Ninject;
using Ninject.Parameters;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.Server
{
    public class ToolRunServer : ITool
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        private ServerConfiguration Config;
        private IKernel Kernel;

        private bool Running;
        private List<AbstractServer> Servers;
        private RunServerOptions Options;

        public ToolRunServer(RunServerOptions options, ServerConfiguration config, IKernel kernel)
        {
            this.Options = options;
            this.Config = config;
            this.Kernel = kernel;
        }

        public void Run()
        {
            LOG.Info("Starting server");

            if(Config.Services == null)
            {
                LOG.Warn("No services found in the configuration file, exiting");
                return;
            }

            Servers = new List<AbstractServer>();

            if(Config.Services.Api != null &&
                Config.Services.Api.Enabled)
            {
                Servers.Add(
                    Kernel.Get<ApiServer>(new ConstructorArgument("config", Config.Services.Api))
                );
            }

            foreach(var cityServer in Config.Services.Cities){
                Servers.Add(
                    Kernel.Get<CityServer>(new ConstructorArgument("config", cityServer))
                );
            }

            Running = true;

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            NetworkDebugger debugInterface = null;

            if (Options.Debug)
            {
                debugInterface = new NetworkDebugger();
                foreach (AbstractServer server in Servers)
                {
                    server.AttachDebugger(debugInterface);
                }
            }

            foreach (AbstractServer server in Servers)
            {
                server.Start();
            }

            if (debugInterface != null)
            {
                Application.EnableVisualStyles();
                Application.Run(debugInterface);
            }
            else
            {
                while (Running)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            foreach (AbstractServer server in Servers)
            {
                server.Shutdown();
            }

            Running = false;
        }
    }
}