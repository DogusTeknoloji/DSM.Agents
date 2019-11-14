using DSM.Core.Ops;
using Microsoft.Extensions.Configuration;
using System;
using System.ServiceProcess;
using System.Threading;

namespace DSM.Agents
{
    public abstract class DSMAgent : ServiceBase
    {
        private readonly string _serviceName;
        private readonly bool _bypassAuth = false;
        protected Timer _mainSvcTimer;

        protected static string authToken;
        protected static readonly string srvName = Environment.MachineName;
        protected static readonly IConfiguration appsettings = AppSettingsManager.GetConfiguration();

        public LogManager loggingSvc;

        protected DSMAgent(string agentName, bool bypassAuth = false)
        {
            ServiceName = agentName;
            _serviceName = agentName;
            _bypassAuth = bypassAuth;
            loggingSvc = LogManager.GetManager(agentName);
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
            loggingSvc.Write($"Service Started -> {_serviceName}");
            string status = "No";
            if (!_bypassAuth)
            {
                authToken = WebOperations.AuthenticateAgent(AgentName: _serviceName, ServerName: srvName); // Agent authentication
                status = authToken.Length > 0 ? "Yes" : "No";
                _mainSvcTimer = new Timer(callback: ActionMain, state: null, dueTime: TimeSpan.Zero, period: TimeSpan.FromMinutes(60));
            }
            else
            {
                _mainSvcTimer = new Timer(callback: ActionMain, state: null, dueTime: 0, period: 1);
                Thread.Sleep(1);
                _mainSvcTimer?.Change(dueTime: Timeout.Infinite, period: 0);
            }

            loggingSvc.Write($"Agent Authentication Required -> {status}");

        }
        protected override void OnStop()
        {
            _mainSvcTimer?.Change(dueTime: Timeout.Infinite, period: 0);
            loggingSvc.Write($"Service Stopped -> {_serviceName}");
            base.OnStop();
        }

        public abstract void ActionMain(object authObj);
    }
}
