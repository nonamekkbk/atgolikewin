using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Net.Http;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Collections;
using Newtonsoft.Json.Linq;
using System.Windows.Threading;

namespace MyAutoGolikeWin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            webBrowserStatic = webBrowser;
            //webBrowser
            logined = false;
            startjobs();
        }

      
        private static readonly HttpClient client = new HttpClient();
        private static BackgroundWorker startWorkingTask;
        private static BackgroundWorker distributeJobsTask;
        private static BackgroundWorker requestJobTask;
        private static BackgroundWorker doJobTask;
        private static BackgroundWorker checkJobSuccessTask;

        public static Boolean logined;
        public static string authToken;

        public static int currentUserDoingJobIndex;
        public static ArrayList userList = new ArrayList();
        public static ArrayList taskList = new ArrayList();
        public static int currentTaskIndex;
        public static WebBrowser webBrowserStatic;// = new WebBrowser();



        public class RecaptchaSolver
        {
            public String taskIdString;
            public String realResult;

            public Boolean solve()
            {
                Console.WriteLine("in solve()");
                if (!requestSolveCaptcha()) return false;
                if (!getResult()) return false;
                return true;
            }

            public Boolean requestSolveCaptcha()
            {
                Console.WriteLine("in requestSolveCaptcha()");
                while (true)
                {
                    String res = requestSolveCaptchaSingle();
                    CaptchaRequest request = JsonConvert.DeserializeObject<CaptchaRequest>(res);
                    String id = request.request;
                    if (id.ToLower().IndexOf("error") != -1)
                    {
                        return false;
                    }
                    else
                    {
                        taskIdString = id;
                        return true;
                    }


                }
            }

            public String requestSolveCaptchaSingle()
            {
                Console.WriteLine("in requestSolveCaptchaSingle()");
                var clientKey = "feb96160fe47600b9de840b46a3b3e1b";
                var method = "userrecaptcha";
                var siteKey = "6LfiTfQUAAAAANM8yUWHdhLQ1pwdkaGlaCHUW609";
                var webUrl = "https://dev.golike.net/api/login";
                var request = (HttpWebRequest)WebRequest.Create("https://api.captcha.guru/in.php?key=" + clientKey + "&method=" + method +"&googlekey="+ siteKey +"&pageurl=" + webUrl + "&json=1");
                request.ContentType = "application/json;charset=utf-8";

                var response = (HttpWebResponse)request.GetResponse();

                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Console.WriteLine(responseString);

                return responseString;
            }

            public Boolean getResult()
            {
                Console.WriteLine("in getResult()");
                int countError = 0, count = 0;
                while (true)
                {
                    String res = getResultSingle();
                    CaptchaRequest request = JsonConvert.DeserializeObject<CaptchaRequest>(res);
                    String result = request.request;
                    if (result.ToLower().IndexOf("error") != -1)
                    {
                        countError++;
                        if (countError >= 20)
                        {
                            //MyLogger.getInstance(mContext).writeLog("error: " + response);
                            return false;
                        }

                        Thread.Sleep(7000);
                        //return false;
                    }
                    else if (result.ToLower().IndexOf("captcha_not_ready") != -1 || result.ToLower().IndexOf("capcha_not_ready") != -1)
                    {
                        count++;
                        if (count >= 200)
                        {
                            //MyLogger.getInstance(mContext).writeLog("error: " + "cannot solve captcha after 1000s waiting");
                            return false;
                        }

                        Thread.Sleep(5000);
                    }
                    else
                    {
                        realResult = result;
                        return true;
                    }
                }
            }

            public String getResultSingle()
            {
                Console.WriteLine("in getResultSingle()");
                var clientKey = "feb96160fe47600b9de840b46a3b3e1b";
                var method = "userrecaptcha";
                var siteKey = "6LfiTfQUAAAAANM8yUWHdhLQ1pwdkaGlaCHUW609";
                var webUrl = "https://dev.golike.net/api/login";
                var request = (HttpWebRequest)WebRequest.Create("https://api.captcha.guru/res.php?" + "key=" + clientKey + "&action=get&id=" + taskIdString + "&json=1");
                request.ContentType = "application/json;charset=utf-8";

                var response = (HttpWebResponse)request.GetResponse();

                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Console.WriteLine(responseString);

                return responseString;
            }

            public class CaptchaRequest
            {
                public String status;
                public String request;
            }
        }

        

        public class StartWorkingTask : BackgroundWorker
        {
            private int loginCount;
            public StartWorkingTask()
            {
                this.DoWork += doInBackground;
                this.RunWorkerCompleted += onDone;
                loginCount = 0;
            }

            public Boolean requestLogin()
            {
                loginCount++;

                if (loginCount >= 5)
                {
                    return false;
                }

                string path = @"C:\token.txt";

                string token = null;//readAuthToken(path);

                if (token == null)
                {

                    try
                    {
                        RecaptchaSolver Solver = new RecaptchaSolver();
                        Solver.solve();
                        //token = Solver.realResult;
                        Console.WriteLine("real reslt= " + Solver.realResult);


                        var request = (HttpWebRequest)WebRequest.Create("https://dev.golike.net/api/login");

                        //var postData = "username=" + Uri.EscapeDataString("nonamekkbk");
                        //postData += "&password=" + Uri.EscapeDataString("cuocsong@@!11235");
                        //postData += "&re_captcha_token=" + Uri.EscapeDataString(Solver.realResult);

                        LoginRequest loginReq = new LoginRequest();
                        loginReq.username = "linhtran11235";
                        loginReq.password = "cuocsong@@!11235";
                        loginReq.re_captcha_token = Solver.realResult;

                        string datastr = JsonConvert.SerializeObject(loginReq);

                        Console.WriteLine("data = " + datastr);

                        Thread.Sleep(10000);

                        var data = Encoding.UTF8.GetBytes(datastr);

                        request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.125 Safari/537.36";
                        request.Method = "POST";
                        request.Headers.Add("t", "VFZSVk5VNTZWVEpOVkZVd1RVRTlQUT09");
                        request.ContentType = "application/json;charset=utf-8";
                        request.ContentLength = data.Length;


                        using (var stream = request.GetRequestStream())
                        {
                            stream.Write(data, 0, data.Length);
                        }

                        //if (response.)

                        var response = (HttpWebResponse)request.GetResponse();

                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            doInBackground(null, null);
                        }
                        else
                        {
                            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                            Console.WriteLine(responseString);

                            LoginResponse res = JsonConvert.DeserializeObject<LoginResponse>(responseString);

                            writeAuthToken(res.token, path);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        doInBackground(null, null);
                    }


                }

                return true;
            }

            public Boolean getFbAccounts()
            {
                Console.WriteLine("in getFbAccounts()");

                string path = @"C:\token.txt";

                string token = readAuthToken(path);

                if (token == null)
                {
                    return false;
                }

                var request = (HttpWebRequest)WebRequest.Create("http://dev.golike.net/api/fb-account?limit=100");

                request.Method = "GET";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.125 Safari/537.36";
                request.ContentType = "application/json;charset=UTF-8";
                //request.Accept = "application/json";
                request.Headers.Add("authorization", "Bearer " + token);
                request.Headers.Add("t", "VFZSVk5VNTZWVEpOVkZVd1RVRTlQUT09");
                //request.Proxy = null;
                //request.Credentials = CredentialCache.DefaultCredentials;  
                //ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(ValidateServerCertificate);
                //System.Net.ServicePointManager.CertificatePolicy = new MyPolicy(); 
                
                var response = (HttpWebResponse)request.GetResponse();

                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                Console.WriteLine("response = " + responseString);

                GetFbAccountResponse res = JsonConvert.DeserializeObject<GetFbAccountResponse>(responseString);
                int size = res.data["data"].Count;

                for (int i = 0; i < size; i++) 
                {
                    UserInfo info = new UserInfo();
                    info.done = false;
                    info.jobsDone = 0;
                    info.userinfoJson = (JObject)res.data["data"][i];
                    userList.Add(info);
                }
                

                return true;
            }

            public void initJobsinfo()
            {

            }


            public Boolean restoreDataFromLocalStorage()
            {
                return true;
            }

            public void onPrepare()
            {

            }

            private string readAuthToken(string path)
            {
                
                if (!File.Exists(path))
                {
                    return null;
                }
                string readText = File.ReadAllText(path, Encoding.UTF8);
                return readText;
            }

            private void writeAuthToken(string token, string path)
            {
                
                //if (!File.Exists(path))
                //{
                    File.WriteAllText(path, token, Encoding.UTF8);
                //}
            }

            public void doInBackground(object sender, DoWorkEventArgs e)
            {
                authToken = readAuthToken(@"C:\token.txt");// null; // readAuthToken(@"C:\token.txt");
                if (authToken == null)
                {
                    if (!logined)
                    {
                        logined = requestLogin();
                        if (!logined) return;
                    }


                }

                Boolean res = false;
                for (int i = 0; i < 3; i++) {
                    res = getFbAccounts();
                    if (res) {
                        logined = true;
                        break;
                    }
                    if (!logined) {
                        
                        logined = requestLogin();

                        if (!logined)
                        {
                            //currentExecutingJobs = false;
                            return;
                        }

                    }
                }

                distributeJobs();
               
               
            }

            public void onDone(object sender, RunWorkerCompletedEventArgs e)
            {

            }
        
        }
        public class DistributeJobsTask : BackgroundWorker
        {
            public DistributeJobsTask()
            {
                this.DoWork += doInBackground;
                this.RunWorkerCompleted += onDone;
            }

            public void doInBackground(object sender, DoWorkEventArgs e)
            {
                
            }

            public void onDone(object sender, RunWorkerCompletedEventArgs e)
            {
                requestJobs();
            }
        }

        public class RequestJobsTask : BackgroundWorker
        {
            public RequestJobsTask()
            {
                this.DoWork += doInBackground;
                this.RunWorkerCompleted += onDone;

                taskList.Clear();
                currentTaskIndex = 0;
            }

            private String requestJobSingle(Boolean needReload)
            {
                Console.WriteLine("in requestJobSingle");
                JObject userinfo = ((UserInfo) userList[currentUserDoingJobIndex]).userinfoJson;

                if (userinfo["load_job_like_clone"] != null)
                {
                    userinfo.Property("load_job_like_clone").Remove();
                }

                userinfo.Add("load_job_like_clone", true);
                //obj.load_job_like_clone = true;

                //var resStr = JsonConvert.SerializeObject(userinfo);
                RequestJobRequest req = new RequestJobRequest();
                req.user = userinfo;
                var sendStr = JsonConvert.SerializeObject(req);

                Console.WriteLine("request = " + sendStr);
                
                string portNo = "8443";
                var url = "https://api.golike.net:" + portNo + "/api/job";
                if(needReload) url = "https://api.golike.net:" + portNo + "/api/job/reload";
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.125 Safari/537.36";
                request.Method = "POST";
                //request.Headers.Add("t", "VFZSVk5VNTZWVEpOVkZVd1RVRTlQUT09");
                request.ContentType = "application/json;charset=utf-8";
                //request.ContentLength = data.Length;

                Console.WriteLine("data = " + sendStr);

                var data = Encoding.UTF8.GetBytes(sendStr);

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                //if (response.)

                var response = (HttpWebResponse)request.GetResponse();

                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                Console.WriteLine(responseString);

                return responseString;
            }

            public void doInBackground(object sender, DoWorkEventArgs e)
            {
                Console.WriteLine("in doInBackground()");
                while (true)
                {
                    string res = requestJobSingle(false);
                    if (res.IndexOf("data") != -1)
                    {
                        RequestJobResponse response = JsonConvert.DeserializeObject<RequestJobResponse>(res);
                        ArrayList list = response.data;
                        if (list == null || list.Count == 0) {
                            continue;
                        }

                        for (int i = 0; i < list.Count; i++)
                        {
                            TaskInfo taskInfo = new TaskInfo();
                            taskInfo.done = false;
                            taskInfo.jobInfo = (JObject) list[i];
                            taskInfo.userInfo = (JObject) ((UserInfo) userList[currentUserDoingJobIndex]).userinfoJson;
                            taskList.Add(taskInfo);
                        }
                        
                        return;
                    }

                    Thread.Sleep(7000);

                }
            }

            public void onDone(object sender, RunWorkerCompletedEventArgs e)
            {
                doJobs();
            }
        }

        public class DoJobsTask: BackgroundWorker
        {
            public DoJobsTask()
            {
                this.DoWork += doInBackground;
                this.RunWorkerCompleted += onDone;
            }

            public void doInBackground(object sender, DoWorkEventArgs e)
            {
                
            }

            public void onDone(object sender, RunWorkerCompletedEventArgs e)
            {
                JObject jobInfo = ((TaskInfo)taskList[currentTaskIndex]).jobInfo;

                string url = (string)jobInfo["link"];
                string newurl = "";

                if (url.IndexOf("facebook.com") == -1)
                {
                    newurl = "https://mbasic.facebook.com/" + url;
                }
                else
                {
                    if (url.IndexOf("http") == -1 || url.IndexOf("https") == -1)
                        url = "https://" + url;
                    if (url.IndexOf("www.facebook.com") != -1)
                    {
                        newurl = url.Replace("www.facebook.com", "mbasic.facebook.com");
                    }
                    else if (url.IndexOf("facebook.com") != -1)
                    {
                        newurl = url.Replace("facebook.com", "mbasic.facebook.com");
                    }
                    else
                        newurl = url;
                }

                webBrowserStatic.Dispatcher.Invoke(
                    new Action(() => {
                        webBrowserStatic.Navigate(newurl);
                    }
                ));
               

            }
        }

        public class CheckJobSuccessTask: BackgroundWorker
        {
            public CheckJobSuccessTask()
            {
                this.DoWork += doInBackground;
                this.RunWorkerCompleted += onDone;
            }

            public void doInBackground(object sender, DoWorkEventArgs e)
            {

            }

            public void onDone(object sender, RunWorkerCompletedEventArgs e)
            {

            }
        }

        // functions 

        public static void startjobs()
        {
            startWorkingTask = new StartWorkingTask();
            startWorkingTask.RunWorkerAsync();
        }

        public static void distributeJobs()
        {
            distributeJobsTask = new DistributeJobsTask();
            distributeJobsTask.RunWorkerAsync();
        }

        public static void requestJobs()
        {
            requestJobTask = new RequestJobsTask();
            requestJobTask.RunWorkerAsync();
        }

        public static void doJobs()
        {
            doJobTask = new DoJobsTask();
            doJobTask.RunWorkerAsync();
        }

        public static void checkJobSuccess()
        {
            checkJobSuccessTask = new CheckJobSuccessTask();
            checkJobSuccessTask.RunWorkerAsync();
        }

     

        public class LoginResponse
        {
            public string token;
            
        }

        public class LoginRequest
        {
            public string username;
            public string password;
            public string re_captcha_token;
        }

        public class GetFbAccountResponse
        {
            public Dictionary<string, ArrayList> data;
        }

        public class RequestJobRequest
        {
            public JObject user;
        }

        public class RequestJobResponse
        {
            public ArrayList data;
        }

        public class UserInfo
        {
            public Boolean done;
            public JObject userinfoJson;
            public int jobsDone;
        }

        public class TaskInfo
        {
            public JObject jobInfo;
            public JObject userInfo;
            public Boolean done;
        }

        public class JobInfo
        {
            public string link;
        }

    }

  
    
}
