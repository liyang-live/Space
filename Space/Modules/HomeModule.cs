using Nancy;
using SharpSvn;
using SharpSvn.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;
using SharpSvn.Remote;
using System.Collections.ObjectModel;
using System.IO.Compression;
using Space.Models;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Space.Modules
{
    public class HomeModule : NancyModule
    {

        private string SVNAdress = ConfigurationManager.AppSettings["SVNAdress"];
        private string SVNUserName = ConfigurationManager.AppSettings["SVNUserName"];
        private string SVNUserPwd = ConfigurationManager.AppSettings["SVNUserPwd"];
        private string SVNPath = ConfigurationManager.AppSettings["SVNPath"];
        private string devenv_com = ConfigurationManager.AppSettings["devenv_com"];
        private static Allowcopyfiles _AllowCopyFile = null;


        public HomeModule()
        {
            //主页
            Get["/"] = r =>
            {
                return Response.AsRedirect("/Home/Index");
            };

            Get["/Home/Index"] = s =>
            {
                return View["index", "DevOps站点"];
            };
            Get["/Home/DownLoad/{name}"] = s =>
            {
                string fileName = s.name;
                var relatePath = AppDomain.CurrentDomain.BaseDirectory + @"发布文件\" + fileName;
                return Response.AsFile(relatePath);
            };

            //启动服务
            Get["/Service/Start"] = r =>
            {
                //return View["index", "测试站点"];
                StartService();
                return Response.AsJson(new ResultMessage { Result = "ok" });
            };

            // 关闭服务
            Get["/Service/Stop"] = r =>
            {
                StopService();
                return Response.AsJson(new ResultMessage { Result = "ok" });
            };

            // 更新项目
            Get["/Programe/Update"] = r =>
            {
                var list = CheckOutPrograme();
                return Response.AsJson(new ResultMessage { Result = "ok", Message = list });
            };

            // 生成并编译项目项目
            Get["/Programe/Build"] = r =>
            {
                var result = BuildPrograme();
                return Response.AsJson(new ResultMessage { Result = "ok", Message = result });
            };

            // 发布版本
            Get["/Programe/Publish"] = r =>
            {
                var str = PublishPrograme();

                return Response.AsJson(new ResultMessage { Result = "ok", Message = new List<string> { str } });
            };


            /////桌面
            //Get["/DestTop"] = r =>
            //{
            //    return View["DestTop"];
            //};
        }

        /// <summary>
        /// 启动服务成功!
        /// </summary>
        public void StartService()
        {

            List<string> list = new List<string>()
            {
                @"Lenovo.HIS.ConsoleHost.exe",
                @"Lenovo.HIS.ConsoleHost.Patients.exe",
                @"Lenovo.HIS.ConsoleHost.DoctorAdvices.exe",
                @"Lenovo.HIS.ConsoleHost.DoctorRequest.exe",
                @"Lenovo.HIS.ConsoleHost.Nurse.exe",
            };
            foreach (var item in list)
            {
                StartService(SVNPath + @"\LenovoService\" + item);
            }

            Console.WriteLine("启动服务成功!");
        }

        //启动外部程序
        //Process proc;
        private void StartService(string AppName)
        {
            try
            {
                //启动外部程序
                Process proc = Process.Start(AppName);
                if (proc != null)
                {
                    //监视进程退出
                    proc.EnableRaisingEvents = true;
                    //指定退出事件方法
                    proc.Exited += new EventHandler(proc_Exited);
                }
            }
            catch (Exception ex) { }
        }

        /// <summary>
        ///启动外部程序退出事件
        /// </summary>
        void proc_Exited(object sender, EventArgs e)
        {
            //MessageBox.Show(String.Format("外部程序 {0} 已经退出！", this.appName), this.Text,
            //MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 关闭服务成功!
        /// </summary>
        public void StopService()
        {
            List<string> list = new List<string>()
            {
                @"Lenovo.HIS.ConsoleHost",
                @"Lenovo.HIS.ConsoleHost.Patients",
                @"Lenovo.HIS.ConsoleHost.DoctorAdvices",
                @"Lenovo.HIS.ConsoleHost.DoctorRequest",
                @"Lenovo.HIS.ConsoleHost.Nurse",
            };
            Process[] myProcesses = Process.GetProcesses();
            foreach (Process myProcess in myProcesses)
            {
                if (list.Contains(myProcess.ProcessName))
                    myProcess.Kill();//强制关闭该程序
            }
            Console.WriteLine("关闭服务成功!");
        }

        /// <summary>
        /// 从SVN检出并自动更新项目
        /// </summary>
        public List<string> CheckOutPrograme()
        {
            using (SvnClient client = new SvnClient())
            {
                SvnInfoEventArgs serverInfo;
                SvnInfoEventArgs clientInfo;
                SvnUriTarget repos = new SvnUriTarget(SVNAdress);
                //SvnPathTarget local = new SvnPathTarget(@"C:\demo");

                client.Authentication.UserNamePasswordHandlers +=
                    new EventHandler<SvnUserNamePasswordEventArgs>(
                        delegate (object s, SvnUserNamePasswordEventArgs e)
                        {
                            e.UserName = SVNUserName;
                            e.Password = SVNUserPwd;
                        });
                client.Authentication.SslServerTrustHandlers += new EventHandler<SharpSvn.Security.SvnSslServerTrustEventArgs>(
                delegate (Object ssender, SharpSvn.Security.SvnSslServerTrustEventArgs se)
                {
                    // Look at the rest of the arguments of E whether you wish to accept

                    // If accept:
                    se.AcceptedFailures = se.Failures;
                    se.Save = true; // Save acceptance to authentication store
                });

                //string path = @"C:\Demo";
                //client.CleanUp(path);
                //client.Revert(path);

                if (!Directory.Exists(SVNPath))
                {
                    Directory.CreateDirectory(SVNPath);
                }

                client.CheckOut(repos, SVNPath);
                client.Update(SVNPath);

                client.GetInfo(repos, out serverInfo);
                client.GetInfo(SVNPath, out clientInfo);

                List<string> listMsg = new List<string>();
                SvnLogArgs logArgs = new SvnLogArgs();

                logArgs.Range = new SvnRevisionRange(clientInfo.LastChangeRevision, serverInfo.LastChangeRevision);
                logArgs.RetrieveAllProperties = true;
                EventHandler<SvnLogEventArgs> logHandler = new EventHandler<SvnLogEventArgs>(delegate (object lo, SvnLogEventArgs le)
                {
                    foreach (SvnChangeItem changeItem in le.ChangedPaths)
                    {
                        string str = string.Format(
                                                     "[操作]:{0}   &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[路径]:{1}   &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[作者]:{2}   &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[日志]:{3}   &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[版本]:{4} ",
                                                     changeItem.Action,
                                                     changeItem.Path,
                                                     //changeItem.CopyFromRevision,
                                                     //changeItem.CopyFromPath,
                                                     le.Author,
                                                     le.LogMessage,
                                                     le.Revision);
                        listMsg.Add(str);

                    }
                });

                client.Log(new Uri(SVNAdress), logArgs, logHandler);
                return listMsg;
                //  this.txtLog.Text += DateTime.Now.ToLongTimeString() + "\r\n";

                //Console.WriteLine(string.Format("serverInfo revision of {0} is {1}", repos, serverInfo.Revision));
                //Console.WriteLine(string.Format("clientInfo revision of {0} is {1}", local, clientInfo.Revision));
            }

            //using (SvnClient client = new SvnClient())
            //{
            //    client.
            //    client.Authentication.UserNamePasswordHandlers +=
            //        new EventHandler<SvnUserNamePasswordEventArgs>(
            //            delegate (object s, SvnUserNamePasswordEventArgs e)
            //            {
            //                e.UserName = "test";
            //                e.Password = "password";
            //            });
            //}
            //using (SvnClient client = new SvnClient())
            //{
            //    SharpSvn.UI.SvnUIBindArgs uiBindArgs = new SharpSvn.UI.SvnUIBindArgs();
            //    SharpSvn.UI.SvnUI.Bind(client, uiBindArgs);
            //}
        }

        /// <summary>
        /// 生成并编译项目项目!
        /// </summary>
        public List<string> BuildPrograme()
        {
            string VScmd = " /Rebuild Debug";
            //string ProgramePath = SVNPath + @"\Lenovo.HIS\Lenovo.HIS.sln";
            List<string> list = new List<string>()
            {
                @"\Lenovo.HIS\Lenovo.HIS.sln",
                @"\Lenovo.HIS.ElectronicHealthRecord\Lenovo.HIS.ElectronicHealthRecord.sln",
                @"\Lenovo.HIS.DoctorAdvices\Lenovo.HIS.DoctorAdvices.sln",
                @"\Lenovo.HIS.DoctorRequest\Lenovo.HIS.DoctorRequest.sln",
                @"\Lenovo.HIS.NurseWorkstation\Lenovo.HIS.NurseWorkstation.sln",
            };
            List<string> BuildMsg = new List<string>();
            foreach (var item in list)
            {
                string cmd = string.Format("\"{0}\" {1}{2}  {3}", devenv_com, SVNPath, item, VScmd);// @"""C:\Install\Microsoft Visual Studio2017\Common7\IDE\devenv.com""  C:\公司项目\新建文件夹\Lenovo.HIS\Lenovo.HIS.sln /Rebuild Debug";
                string result = ExecuteCMD(cmd);
                BuildMsg.Add(result);
            }
            Console.WriteLine("生成并编译项目项目!");
            return BuildMsg;
        }

        #region 发布版本
        /// <summary>
        /// 发布版本!
        /// </summary>
        public string PublishPrograme()
        {
            return MoveFile();
        }


        private string MoveFile()
        {
            GetConfig();
            string path = Path.Combine(SVNPath, "LenovoClient");
            string newFileName = @"LenovoHIS" + DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss");
            string newpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"发布文件\" + newFileName + @"\LenovoClient");
            if (Directory.Exists(newpath))
            {
                Directory.Delete(newpath, true);
            }
            if (!Directory.Exists(newpath))
            {
                Directory.CreateDirectory(newpath);
            }
            ClientDirectoryCopy(path, newpath);

            //LenovoService
            string LenovoService = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"发布文件\" + newFileName + @"\LenovoService");
            if (Directory.Exists(LenovoService))
            {
                Directory.Delete(LenovoService, true);
            }
            if (!Directory.Exists(LenovoService))
            {
                Directory.CreateDirectory(LenovoService);
            }
            ServerDirectoryCopy(path, LenovoService);

            string zipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"发布文件\" + newFileName);

            ZipFile.CreateFromDirectory(zipPath, zipPath + ".zip");

            return newFileName + ".zip";
        }

        private void ClientDirectoryCopy(string sourceDirectory, string targetDirectory)
        {

            DirectoryInfo sourceInfo = new DirectoryInfo(sourceDirectory);
            FileInfo[] fileInfo = sourceInfo.GetFiles();
            foreach (FileInfo fiTemp in fileInfo)
            {
                try
                {
                    if (_AllowCopyFile != null && _AllowCopyFile.ClientAllowCopyFiles != null && _AllowCopyFile.ClientAllowCopyFiles.Where(s => s.FileName.ToLower() == fiTemp.Name.ToLower()).Count() > 0)
                    {
                        File.Copy(sourceDirectory + "\\" + fiTemp.Name, targetDirectory + "\\" + fiTemp.Name, true);
                    }
                    else if (_AllowCopyFile == null || (_AllowCopyFile.ClientAllowCopyFiles == null && _AllowCopyFile.ClientAllowCopyFolders == null))
                    {
                        File.Copy(sourceDirectory + "\\" + fiTemp.Name, targetDirectory + "\\" + fiTemp.Name, true);
                    }
                }
                catch (Exception ex) { }
            }
            DirectoryInfo[] diInfo = sourceInfo.GetDirectories();
            foreach (DirectoryInfo diTemp in diInfo)
            {
                string sourcePath = diTemp.FullName;
                string targetPath = diTemp.FullName.Replace(sourceDirectory, targetDirectory);
                if (_AllowCopyFile != null && _AllowCopyFile.ClientAllowCopyFolders != null && _AllowCopyFile.ClientAllowCopyFolders.Where(s => s.FolderName.ToLower() == diTemp.Name.ToLower()).Count() > 0)
                {
                    Directory.CreateDirectory(targetPath);
                }
                ClientDirectoryCopy(sourcePath, targetPath);
            }
        }

        private void ServerDirectoryCopy(string sourceDirectory, string targetDirectory)
        {

            DirectoryInfo sourceInfo = new DirectoryInfo(sourceDirectory);
            FileInfo[] fileInfo = sourceInfo.GetFiles();
            foreach (FileInfo fiTemp in fileInfo)
            {
                try
                {
                    if (_AllowCopyFile != null && _AllowCopyFile.ClientAllowCopyFiles != null && _AllowCopyFile.ClientAllowCopyFiles.Where(s => s.FileName.ToLower() == fiTemp.Name.ToLower()).Count() > 0)
                    {
                        File.Copy(sourceDirectory + "\\" + fiTemp.Name, targetDirectory + "\\" + fiTemp.Name, true);
                    }
                    else if (_AllowCopyFile == null || (_AllowCopyFile.ClientAllowCopyFiles == null && _AllowCopyFile.ClientAllowCopyFolders == null))
                    {
                        File.Copy(sourceDirectory + "\\" + fiTemp.Name, targetDirectory + "\\" + fiTemp.Name, true);
                    }
                }
                catch (Exception ex) { }
            }
            DirectoryInfo[] diInfo = sourceInfo.GetDirectories();
            foreach (DirectoryInfo diTemp in diInfo)
            {
                string sourcePath = diTemp.FullName;
                string targetPath = diTemp.FullName.Replace(sourceDirectory, targetDirectory);
                if (_AllowCopyFile != null && _AllowCopyFile.ClientAllowCopyFolders != null && _AllowCopyFile.ClientAllowCopyFolders.Where(s => s.FolderName.ToLower() == diTemp.Name.ToLower()).Count() > 0)
                {
                    Directory.CreateDirectory(targetPath);
                }
                ServerDirectoryCopy(sourcePath, targetPath);
            }
        }

        private void GetConfig()
        {
            if (_AllowCopyFile == null)
            {
                if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "AllowCopyFiles.json"))
                {
                    return;
                }
                var str = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "AllowCopyFiles.json");
                _AllowCopyFile = JsonConvert.DeserializeObject<Allowcopyfiles>(str);
            }
        }
        #endregion

        /// <summary>
        /// 执行命令行
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public string ExecuteCMD(string cmd)
        {
            //string cmd = @"""C:\Install\Microsoft Visual Studio2017\Common7\IDE\devenv.com""  C:\公司项目\新建文件夹\Lenovo.HIS\Lenovo.HIS.sln /Rebuild Debug";
            //string  cmd= @"""C:\Install\Microsoft Visual Studio2017\Common7\IDE\devenv.exe\""  C:\公司项目\新建文件夹\Lenovo.HIS\Lenovo.HIS.sln  /Rebuild Debug ";
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;    //是否使用操作系统shell启动
            p.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
            p.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
            p.StartInfo.RedirectStandardError = true;//重定向标准错误输出
            p.StartInfo.CreateNoWindow = true;//不显示程序窗口
            p.Start();//启动程序

            //向cmd窗口发送输入信息
            p.StandardInput.WriteLine(cmd + " &exit");
            //p.StandardInput.WriteLine(cmd);

            p.StandardInput.AutoFlush = true;
            //p.StandardInput.WriteLine("exit");
            //向标准输入写入要执行的命令。这里使用&是批处理命令的符号，表示前面一个命令不管是否执行成功都执行后面(exit)命令，如果不执行exit命令，后面调用ReadToEnd()方法会假死
            //同类的符号还有&&和||前者表示必须前一个命令执行成功才会执行后面的命令，后者表示必须前一个命令执行失败才会执行后面的命令

            //获取cmd窗口的输出信息
            string output = p.StandardOutput.ReadToEnd();

            //StreamReader reader = p.StandardOutput;
            //string line=reader.ReadLine();
            //while (!reader.EndOfStream)
            //{
            //    str += line + "  ";
            //    line = reader.ReadLine();
            //}

            p.WaitForExit();//等待程序执行完退出进程
            p.Close();

            return output;
            //Console.WriteLine(output);
            //Console.WriteLine("执行完毕！");
            //Console.ReadLine();
            //Console.ReadKey();
        }
    }

    public class ResultMessage
    {
        public string Result { get; set; }

        public List<string> Message { get; set; }

        public PublishFileInfo PublishFileInfo { get; set; }
    }

    public class PublishFileInfo
    {
        public string FileName { get; set; }

        public string Url { get; set; }
    }

}
