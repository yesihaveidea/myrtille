using System;
using System.ServiceProcess;

namespace Myrtille.Admin.Services
{
    public class Program : ServiceBase
    {
        private static void Main(string[] args)
        {
            if (!Environment.UserInteractive)
            {
                Run(new Program());
            }
            else
            {
                MyrtilleApiHost.Start();
                Console.ReadLine();
                MyrtilleApiHost.Stop();
            }
        }

        protected override void OnStart(string[] args)
		{
            MyrtilleApiHost.Start();
        }
 
		protected override void OnStop()
		{
            MyrtilleApiHost.Stop();
        }
    }
}