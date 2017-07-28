using Nancy.Hosting.Self;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace Space
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x =>                                 //1
            {
                x.RunAsLocalSystem();                            //6
                x.StartAutomatically();
                x.SetDescription("Space自动生成发布服务");        //7
                x.SetDisplayName("Space自动生成发布服务");                       //8
                x.SetServiceName("Space自动生成发布服务");                       //9

                x.Service<StartService>(s =>                        //2
                {
                    s.ConstructUsing(name => new StartService());     //3
                    s.WhenStarted(tc => tc.Start());              //4
                    s.WhenStopped(tc => tc.Stop());               //5
                });
            });
        }
    }

    public class StartService
    {
        private NancyHost _host;
        public void Start()
        {
            _host = new NancyHost(new Uri("http://localhost:1234"));
            _host.Start();
            //Console.WriteLine("Running on http://localhost:1234");
            //Console.ReadLine();
        }
        public void Stop()
        {
            if (_host != null)
            {
                _host.Stop();
                _host.Dispose();
            }
        }
    }
}
