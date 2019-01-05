using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebApplication1.Models;
using Newtonsoft.Json;
using RestSharp;
using Newtonsoft.Json.Linq;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        static string[] Scopes = { DriveService.Scope.DriveReadonly };

        //Application ID
        static string client_id = "Enter your Application ID";
        //Application Secret
        static string client_secret = "Enter you Application Secret";
        //Redirect URL
        static string redirect_url = "http://localhost:10626/Home/GenerateToken";
        //Scopes
        static string scope = "onedrive.readonly onedrive.readwrite onedrive.appfolder openid profile offline_access user.readwrite mail.readwrite mail.send";

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult CreateToken()
        {
            SampleModel sm = new SampleModel();

            sm.access_token = "";

            string fileName = Server.MapPath(Url.Content("~/Content/token.file"));

            if (System.IO.File.Exists(fileName))
            {
                StreamReader sr = new StreamReader(fileName);

                string tem = sr.ReadToEnd();

                sr.Close();

                CustomJsonClass _json = JsonConvert.DeserializeObject<CustomJsonClass>(tem);


                sm.access_token = _json.access_token;

                DateTime JsonExpiryTime = DateTime.Parse(_json.time);

                double SecondsDifference = DateTime.Now.Subtract(JsonExpiryTime).TotalSeconds;

                if (SecondsDifference > 3000)
                {
                    return Redirect(String.Format("https://login.live.com/oauth20_authorize.srf?client_id={0}&scope={1}&response_type=code&redirect_uri={2}", client_id, scope, redirect_url));
                }

            }
            else
            {
                return Redirect(String.Format("https://login.live.com/oauth20_authorize.srf?client_id={0}&scope={1}&response_type=code&redirect_uri={2}", client_id, scope, redirect_url));
            }

            ViewBag.MyString = "Token already Created";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult drive()
        {
            return View(new List<string>());
        }

        public JsonResult GetFolders()
        {
            SampleModel sm = new SampleModel();

            sm.access_token = "";

            string fileName = Server.MapPath(Url.Content("~/Content/token.file"));

            if (System.IO.File.Exists(fileName))
            {
                StreamReader sr = new StreamReader(fileName);

                string tem = sr.ReadToEnd();

                sr.Close();

                CustomJsonClass _json = JsonConvert.DeserializeObject<CustomJsonClass>(tem);


                sm.access_token = _json.access_token;

                DateTime JsonExpiryTime = DateTime.Parse(_json.time);

                double SecondsDifference = DateTime.Now.Subtract(JsonExpiryTime).TotalSeconds;

                if (SecondsDifference > 3000)
                {

                    var client = new RestClient("https://login.live.com/oauth20_token.srf");
                    var request = new RestRequest(Method.POST);
                    request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                    request.AddParameter("undefined", String.Format("client_id={1}&grant_type=refresh_token&redirect_uri={2}&client_secret={3}&refresh_token={0}&undefined=", _json.refresh_token, client_id, HttpUtility.UrlEncode(redirect_url), HttpUtility.UrlEncode(client_secret)), ParameterType.RequestBody);
                    IRestResponse response = client.Execute(request);

                    string tt = response.Content;

                    StreamWriter sw = new StreamWriter(fileName);

                    sw.WriteLine(response.Content.Replace("}", String.Format(",\"time\":\"{0} {1}\"", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString())) + "}");
                    sw.Flush();
                    sw.Close();
                }


                List<FolderListClass> _folders = new List<FolderListClass>();
                List<FolderTempClass> _foldersObject = new List<FolderTempClass>();

                _folders.Add(new FolderListClass { id = "root", text = "root" });

                foreach (var Res in ResFromFolder(_foldersObject, "root", sm.access_token).ToList())
                    getHierarchy(Res, _folders[0].children, _foldersObject, sm.access_token);

                return this.Json(_folders, JsonRequestBehavior.AllowGet);
            }

            return this.Json("", JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetAllFiles(string id)
        {

            SampleModel sm = new SampleModel();

            sm.access_token = "";

            string fileName = Server.MapPath(Url.Content("~/Content/token.file"));

            if (System.IO.File.Exists(fileName))
            {
                StreamReader sr = new StreamReader(fileName);

                CustomJsonClass _json = JsonConvert.DeserializeObject<CustomJsonClass>(sr.ReadToEnd());

                sr.Close();

                sm.access_token = _json.access_token;

                DateTime JsonExpiryTime = DateTime.Parse(_json.time);

                double SecondsDifference = DateTime.Now.Subtract(JsonExpiryTime).TotalSeconds;

                if (SecondsDifference > 3000)
                {
                    var client = new RestClient("https://login.live.com/oauth20_token.srf");
                    var request = new RestRequest(Method.POST);
                    request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                    request.AddParameter("undefined", String.Format("client_id={1}&grant_type=refresh_token&redirect_uri={2}&client_secret={3}&refresh_token={0}&undefined=", _json.refresh_token, client_id, HttpUtility.UrlEncode(redirect_url), HttpUtility.UrlEncode(client_secret)), ParameterType.RequestBody);
                    IRestResponse response = client.Execute(request);

                    string tt = response.Content;

                    StreamWriter sw = new StreamWriter(fileName);

                    sw.WriteLine(response.Content.Replace("}", String.Format(",\"time\":\"{0} {1}\"", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString())) + "}");
                    sw.Flush();
                    sw.Close();
                }


                List<string> _files = new List<string>();

                foreach (var Res in ResFromFiles(id, sm.access_token).ToList())
                    getHierarchyFiles(Res, _files, sm.access_token);

                var jj = JsonConvert.SerializeObject(_files);

                return View(_files);
            }
            return View("");

        }

        #region Old Logic

        //public List<Google.Apis.Drive.v3.Data.File> ResFromFolder(DriveService service, string folderId)
        //{
        //    var request = service.Files.List();
        //    request.PageSize = 1000;
        //    request.Fields = "nextPageToken, files(id, name, parents, shared, mimeType)";
        //    request.Q = String.Format("'{0}' in parents", folderId);

        //    List<Google.Apis.Drive.v3.Data.File> TList = new List<Google.Apis.Drive.v3.Data.File>();
        //    do
        //    {
        //        var children = request.Execute();
        //        foreach (var child in children.Files)
        //        {
        //            TList.Add(service.Files.Get(child.Id).Execute());
        //        }
        //        request.PageToken = children.NextPageToken;
        //    } while (!String.IsNullOrEmpty(request.PageToken));

        //    return TList;
        //}

        //private void getHierarchy(Google.Apis.Drive.v3.Data.File Res, DriveService driveService, StringBuilder sb)
        //{
        //    if (Res.MimeType == "application/vnd.google-apps.folder")
        //    {
        //        sb.Append(intend + Res.Name + " :" + Environment.NewLine);
        //        intend += "     ";

        //        foreach (var res in ResFromFolder(driveService, Res.Id).ToList())
        //            getHierarchy(res, driveService, sb);

        //        intend = intend.Remove(intend.Length - 5);
        //    }
        //    else
        //    {
        //        sb.Append(intend + Res.Name + Environment.NewLine);
        //    }
        //}


        #endregion

        #region New Logic

        public List<FolderTempClass> ResFromFolder(List<FolderTempClass> _folderObject, string folderId, string token)
        {
            if (folderId == "root")
            {
                List<FolderTempClass> TList = new List<FolderTempClass>();

                var client = new RestClient("https://api.onedrive.com/v1.0/drive/root/children?filter=folder%20ne%20null&select=id,name,folder,parentReference");
                var request = new RestRequest(Method.GET);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddHeader("Authorization", String.Format("Bearer {0}", token));
                IRestResponse response = client.Execute(request);

                JObject temp = JsonConvert.DeserializeObject<JObject>(response.Content.Replace("@odata.context", "context"));

                foreach (var item in temp["value"])
                {
                    TList.Add(new FolderTempClass
                    {
                        Childcount = int.Parse(item["folder"]["childCount"].ToString()),
                        FolderID = item["id"].ToString(),
                        FolderName = item["name"].ToString(),
                        ParentID = item["parentReference"]["path"].ToString()
                    });
                }

                return TList;
            }
            else
            {
                List<FolderTempClass> TList = new List<FolderTempClass>();

                var client = new RestClient(String.Format("https://api.onedrive.com/v1.0{0}?filter=folder%20ne%20null&select=id,name,folder,parentReference", folderId));
                var request = new RestRequest(Method.GET);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddHeader("Authorization", String.Format("Bearer {0}", token));
                IRestResponse response = client.Execute(request);

                JObject temp = JsonConvert.DeserializeObject<JObject>(response.Content.Replace("@odata.context", "context"));

                foreach (var item in temp["value"])
                {
                    TList.Add(new FolderTempClass
                    {
                        Childcount = int.Parse(item["folder"]["childCount"].ToString()),
                        FolderID = item["id"].ToString(),
                        FolderName = item["name"].ToString(),
                        ParentID = item["parentReference"]["path"].ToString()
                    });
                }

                return TList;
            }
        }

        private void getHierarchy(FolderTempClass Res, List<FolderListClass> _folders, List<FolderTempClass> _folderObject, string token)
        {
            _folders.Add(new FolderListClass { id = Res.ParentID + "/" + Res.FolderName + ":/children", text = Res.FolderName });

            if (Res.Childcount > 1)
            {
                foreach (var res in ResFromFolder(_folderObject, Res.ParentID + "/" + Res.FolderName + ":/children", token))
                {
                    List<FolderListClass> _tempList = (from k in _folders where k.id == Res.ParentID + "/" + Res.FolderName + ":/children" select k.children).Single();
                    getHierarchy(res, _tempList, _folderObject, token);
                }
            }




        }

        public static List<FolderTempClass> ResFromFiles(string folderId, string token)
        {
            if (folderId == "root")
            {
                List<FolderTempClass> TList = new List<FolderTempClass>();

                var client = new RestClient("https://api.onedrive.com/v1.0/drive/root/children?filter=folder%20ne%20null&select=id,name");
                var request = new RestRequest(Method.GET);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddHeader("Authorization", String.Format("Bearer {0}", token));
                IRestResponse response = client.Execute(request);

                JObject temp = JsonConvert.DeserializeObject<JObject>(response.Content.Replace("@odata.context", "context"));

                foreach (var item in temp["value"])
                {
                    TList.Add(new FolderTempClass
                    {
                        //Childcount = int.Parse(item["folder"]["childCount"].ToString()),
                        FolderID = item["id"].ToString(),
                        FolderName = item["name"].ToString() + " - [Folder]",
                        //ParentID = item["parentReference"]["path"].ToString()
                    });
                }

                client = new RestClient("https://api.onedrive.com/v1.0/drive/root/children?filter=file%20ne%20null&select=id,name");
                request = new RestRequest(Method.GET);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddHeader("Authorization", String.Format("Bearer {0}", token));
                response = client.Execute(request);

                temp = JsonConvert.DeserializeObject<JObject>(response.Content.Replace("@odata.context", "context"));

                foreach (var item in temp["value"])
                {
                    TList.Add(new FolderTempClass
                    {
                        //Childcount = int.Parse(item["folder"]["childCount"].ToString()),
                        FolderID = item["id"].ToString(),
                        FolderName = item["name"].ToString(),
                        //ParentID = item["parentReference"]["path"].ToString()
                    });
                }

                return TList;
            }
            else
            {
                List<FolderTempClass> TList = new List<FolderTempClass>();

                var client = new RestClient(String.Format("https://api.onedrive.com/v1.0{0}?filter=folder%20ne%20null&select=id,name", folderId));
                var request = new RestRequest(Method.GET);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddHeader("Authorization", String.Format("Bearer {0}", token));
                IRestResponse response = client.Execute(request);

                JObject temp = JsonConvert.DeserializeObject<JObject>(response.Content.Replace("@odata.context", "context"));

                foreach (var item in temp["value"])
                {
                    TList.Add(new FolderTempClass
                    {
                        //Childcount = int.Parse(item["folder"]["childCount"].ToString()),
                        FolderID = item["id"].ToString(),
                        FolderName = item["name"].ToString() + " - [Folder]",
                        //ParentID = item["parentReference"]["path"].ToString()
                    });
                }


                client = new RestClient(String.Format("https://api.onedrive.com/v1.0{0}?filter=file%20ne%20null&select=id,name", folderId));
                request = new RestRequest(Method.GET);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddHeader("Authorization", String.Format("Bearer {0}", token));
                response = client.Execute(request);

                temp = JsonConvert.DeserializeObject<JObject>(response.Content.Replace("@odata.context", "context"));

                foreach (var item in temp["value"])
                {
                    TList.Add(new FolderTempClass
                    {
                        //Childcount = int.Parse(item["folder"]["childCount"].ToString()),
                        FolderID = item["id"].ToString(),
                        FolderName = item["name"].ToString(),
                        //ParentID = item["parentReference"]["path"].ToString()
                    });
                }

                return TList;
            }
        }



        private static void getHierarchyFiles(FolderTempClass Res, List<string> _files, string token)
        {
            _files.Add(Res.FolderName);
        }

        #endregion

        public ActionResult GenerateToken(string code)
        {
            string fileName = Server.MapPath(Url.Content("~/Content/token.file"));

            //https://api.onedrive.com/v1.0/drive

            var client = new RestClient("https://login.live.com/oauth20_token.srf");
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("undefined", String.Format("code={0}&redirect_uri={1}&grant_type=authorization_code&client_id={2}&client_secret={3}&undefined=", code, HttpUtility.UrlEncode(redirect_url), client_id, HttpUtility.UrlEncode(client_secret)), ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);

            string tt = response.Content;

            StreamWriter sw = new StreamWriter(fileName);

            sw.WriteLine(response.Content.Replace("}", String.Format(",\"time\":\"{0} {1}\"", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString())) + "}");
            sw.Flush();
            sw.Close();

            return Redirect("drive");
        }

    }

    public class FolderListClass
    {
        public string id { get; set; }
        public string text { get; set; }
        public List<FolderListClass> children { get; set; }

        public FolderListClass()
        {
            this.children = new List<FolderListClass>();
        }
    }

    public class CustomJsonFolderModel
    {
        public string id { get; set; }
        public string text { get; set; }
    }

    public class FolderTempClass
    {
        public string FolderID { get; set; }
        public string FolderName { get; set; }
        public int Childcount { get; set; }
        public string ParentID { get; set; }
        public string MimeType { get; set; }
    }

    public class SampleModel
    {
        public string access_token { get; set; }
    }

    public class CustomFolderClass
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Childcount { get; set; }
        public string ParentPath { get; set; }
    }

    public class CustomJsonClass
    {
        public string token_type { get; set; }
        public string expires_in { get; set; }
        public string scope { get; set; }
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public string id_token { get; set; }
        public string time { get; set; }
    }
}