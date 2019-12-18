using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Ad2AventriSync
{
    class AventriRest
    {
        public readonly string AventriHost;
        public readonly string AventriAccountID;
        private readonly string _aventriApiKey;
        private readonly HttpClient _client;
        private object _cachedTokenLock = new Object();
        private Int64 _cachedTokenValidUntil;
        private string _cachedToken;

        public AventriRest(string aventriHost, string aventriAccountId, string aventriApiKey) : this(aventriHost, aventriAccountId, aventriApiKey, null as Uri) { }

        public AventriRest(string aventriHost, string aventriAccountId, string aventriApiKey, string proxyUri)
        {
            AventriHost = aventriHost;
            AventriAccountID = aventriAccountId;
            _aventriApiKey = aventriApiKey;
            _client = new HttpClient(
                new HttpClientHandler()
                {
                    Proxy = CreateProxyFromUrl(proxyUri),
                    UseProxy = true,
                }
            );
        }

        public AventriRest(string aventriHost, string aventriAccountId, string aventriApiKey, Uri proxyUri)
        {
            AventriHost = aventriHost;
            AventriAccountID = aventriAccountId;
            _aventriApiKey = aventriApiKey;
            _client = new HttpClient(
                new HttpClientHandler()
                {
                    Proxy = CreateProxyFromUrl(proxyUri),
                    UseProxy = true,
                }
            );
        }

        protected async Task<string> ExecuteGetRequestAsync(Uri uri, HttpStatusCode[] statusCodes)
        {
            HttpResponseMessage response = await _client.GetAsync(uri);
            ThrowOnInvalidStatusCode(response.StatusCode, statusCodes);
            if (response != null && response.Content != null)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return null;
        }

        protected string ExecuteGetRequest(Uri uri)
        {
            return ExecuteGetRequest(uri, new HttpStatusCode[] { });
        }


        protected string ExecuteGetRequest(Uri uri, HttpStatusCode[] statusCodes)
        {
            Task<string> task = ExecuteGetRequestAsync(uri, statusCodes);
            return task.GetAwaiter().GetResult();
        }



        protected async Task<string> ExcecuteJsonPostRequestAsync(Uri uri, string body, HttpStatusCode[] statusCodes)
        {

            HttpResponseMessage response = await _client.PostAsync(uri, new StringContent(body, Encoding.UTF8, "application/json"));
            ThrowOnInvalidStatusCode(response.StatusCode, statusCodes);
            if (response != null && response.Content != null)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return null;
        }

        protected string ExcecuteJsonPostRequest(Uri uri, string body)
        {
            return ExcecuteJsonPostRequest(uri, body, new HttpStatusCode[] { });
        }

        protected string ExcecuteJsonPostRequest(Uri uri, string body, HttpStatusCode[] statusCodes)
        {
            Task<string> task = ExcecuteJsonPostRequestAsync(uri, body, statusCodes);
            return task.GetAwaiter().GetResult();
        }



        protected async Task<string> ExcecuteJsonPutRequestAsync(Uri uri, string body, HttpStatusCode[] statusCodes)
        {

            HttpResponseMessage response = await _client.PutAsync(uri, new StringContent(body, Encoding.UTF8, "application/json"));
            ThrowOnInvalidStatusCode(response.StatusCode, statusCodes);
            if (response != null && response.Content != null)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return null;
        }

        protected string ExcecuteJsonPutRequest(Uri uri, string body)
        {
            return ExcecuteJsonPutRequest(uri, body, new HttpStatusCode[] { });
        }

        protected string ExcecuteJsonPutRequest(Uri uri, string body, HttpStatusCode[] statusCodes)
        {
            Task<string> task = ExcecuteJsonPutRequestAsync(uri, body, statusCodes);
            return task.GetAwaiter().GetResult();
        }

        private void ThrowOnInvalidStatusCode(HttpStatusCode httpStatusCode, HttpStatusCode[] allowedStatusCodes)
        {
            if (allowedStatusCodes.Length == 0) return;
            for (int i = 0; i < allowedStatusCodes.Length; i++)
            {
                if (allowedStatusCodes[i] == httpStatusCode) return;
            }
            throw new Exception("Invalid status code!");
        }


        protected string GetToken()
        {
            lock (_cachedTokenLock)
            {
                if (_cachedTokenValidUntil >= DateTime.Now.Ticks && _cachedToken != null)
                {
                    return _cachedToken;
                }
                JObject j = JObject.Parse(ExecuteGetRequest(new Uri("https://" + AventriHost + "/api/v2/global/authorize.json?accountid=" + AventriAccountID + "&key=" + _aventriApiKey)));
                if (j["accesstoken"] != null)
                {
                    _cachedToken = j["accesstoken"].ToString();
                    _cachedTokenValidUntil = DateTime.Now.Ticks + 3600000000; // valid for 1 hour
                    return _cachedToken;
                }
            }
            throw new Exception("Failed to aquire access token using url: https://" + AventriHost + "/api/v2/global/authorize.json");
        }

        protected IWebProxy CreateProxyFromUrl(string url)
        {
            if (url == null || url == "") return null;

            return CreateProxyFromUrl(new Uri(url));
        }

        protected IWebProxy CreateProxyFromUrl(Uri uri)
        {
            if (uri == null) return null;
            if (uri.Scheme == null || (uri.Scheme.ToLower() != "http" && uri.Scheme.ToLower() != "https")) return null;
            if (uri.Host == null) return null;

            string proxyUrl = "";
            proxyUrl = uri.Scheme.ToLower() + "://" + uri.Host.ToLower() + ":" + uri.Port;
            if (uri.PathAndQuery != null) proxyUrl += uri.PathAndQuery;
            Uri proxyUri = new Uri(proxyUrl);
            WebProxy p = new WebProxy(proxyUri);

            if (uri.UserInfo.Trim() == ":")
            {
                p.UseDefaultCredentials = true;
            }
            else
            {
                p.UseDefaultCredentials = false;
                if (uri.UserInfo.Trim() != "")
                {
                    NetworkCredential nc = new NetworkCredential();
                    string[] userpw = uri.UserInfo.Split(new char[] { ':' }, 2);
                    string[] user = userpw[0].Split(new char[] { '\\' }, 2);
                    if (user.Length == 1)
                    {
                        nc.UserName = user[0];
                    }
                    else
                    {
                        nc.Domain = user[0];
                        nc.UserName = user[1];
                    }
                    if (userpw.Length == 2)
                    {
                        nc.Password = userpw[1];
                    }
                    p.Credentials = nc;
                }
            }
            return p;
        }


        public List<Dictionary<string, string>> GetAllSubscribersAsList(string listId)
        {
            List<Dictionary<string, string>> subscribers = new List<Dictionary<string, string>>();

            string _token = GetToken();

            int limit = 600;
            int offset = 0;
            while (offset < 600000)
            {
                try
                {
                    JArray j = JArray.Parse(ExecuteGetRequest(new Uri("https://" + AventriHost + "/api/v2/emarketing/listSubscribers.json?accesstoken=" + _token + "&listid=" + listId + "&limit=" + limit + "&offset=" + offset + "&fields=subscriberid,email,fname,lname,city,country,other_id")));
                    foreach (var el in j)
                    {
                        Dictionary<string, string> subscriber = new Dictionary<string, string>();
                        foreach (var usr in (JObject)el)
                        {
                            subscriber[usr.Key] = usr.Value.ToString();
                        }
                        subscribers.Add(subscriber);
                    }
                }
                catch (JsonReaderException)
                {
                    JObject j = JObject.Parse(ExecuteGetRequest(new Uri("https://" + AventriHost + "/api/v2/emarketing/listSubscribers.json?accesstoken=" + _token + "&listid=" + listId + "&limit=" + limit + "&offset=" + offset + "&fields=subscriberid,email,fname,lname,city,country,other_id")));
                    if (j["error"] != null)
                    {
                        if (j["error"]["data"] != null && j["error"]["data"].ToString().ToLower().Contains("no subscribers found"))
                        {
                            break;
                        }
                        throw new Exception(j["error"].ToString());
                    }
                }
                offset += limit;
            }

            return subscribers;
        }


        public System.Data.DataTable GetAllSubscribersAsDataTable(string listId)
        {
            System.Data.DataTable table = new System.Data.DataTable();

            string[] propertiesToLoad = new string[] { "subscriberid", "email", "fname", "lname", "city", "country" };

            foreach (string key in propertiesToLoad)
            {
                table.Columns.Add(key);
            }

            foreach (Dictionary<string, string> subscriber in GetAllSubscribersAsDict(listId).Values)
            {
                System.Data.DataRow row = table.NewRow();
                int i = 0;
                foreach (string key in propertiesToLoad)
                {
                    if (subscriber.ContainsKey(key))
                    {
                        row[i] = subscriber[key];
                    }
                    else
                    {
                        row[i] = "";
                    }
                    i++;
                }
                table.Rows.Add(row);
            }

            return table;
        }

        public Dictionary<string, Dictionary<string, string>> GetAllSubscribersAsDict(string listId)
        {
            Dictionary<string, Dictionary<string, string>> subscribers = new Dictionary<string, Dictionary<string, string>>();

            string _token = GetToken();

            int limit = 600;
            int offset = 0;
            while (offset < 600000)
            {
                try
                {
                    JArray j = JArray.Parse(ExecuteGetRequest(new Uri("https://" + AventriHost + "/api/v2/emarketing/listSubscribers.json?accesstoken=" + _token + "&listid=" + listId + "&limit=" + limit + "&offset=" + offset + "&fields=subscriberid,email,fname,lname,city,country,other_id")));
                    foreach (var el in j)
                    {
                        Dictionary<string, string> subscriber = new Dictionary<string, string>();
                        foreach (var usr in (JObject)el)
                        {
                            subscriber[usr.Key] = usr.Value.ToString();
                        }
                        if (subscriber.ContainsKey("email"))
                        {
                            subscribers[subscriber["email"].ToLower()] = subscriber;
                        }
                    }
                }
                catch (JsonReaderException)
                {
                    JObject j = JObject.Parse(ExecuteGetRequest(new Uri("https://" + AventriHost + "/api/v2/emarketing/listSubscribers.json?accesstoken=" + _token + "&listid=" + listId + "&limit=" + limit + "&offset=" + offset + "&fields=subscriberid,email,fname,lname,city,country,other_id")));
                    if (j["error"] != null)
                    {
                        if (j["error"]["data"] != null && j["error"]["data"].ToString().ToLower().Contains("no subscribers found"))
                        {
                            break;
                        }
                        throw new Exception(j["error"].ToString());
                    }
                }
                offset += limit;
            }

            return subscribers;
        }

        public string DeleteSubscriber(string subscriberId)
        {
            string _token = GetToken();

            JObject j = JObject.Parse(ExcecuteJsonPutRequest(
                new Uri("https://" + AventriHost + "/api/v2/emarketing/updateSubscriber.json?accesstoken=" + _token + "&subscriberid=" + subscriberId + "&deleted=1"),
                JsonConvert.SerializeObject(new Dictionary<string, string> {
                    { "accesstoken", _token },
                    { "subscriberid" , subscriberId },
                    { "deleted" , "1" },
                })
            ));

            if (j["error"] != null)
            {
                throw new Exception(j["error"].ToString());
            }

            if (j["subscriberid"] == null)
            {
                throw new Exception(j.ToString());
            }

            return j["subscriberid"].ToString();
        }


        public string AddSubscriber(
            string listId,
            string mail,
            string fname,
            string lname,
            string city,
            string country,
            string username
        )
        {
            string _token = GetToken();

            JObject j = JObject.Parse(ExcecuteJsonPostRequest(
                new Uri("https://" + AventriHost + "/api/v2/emarketing/createSubscriber.json?accesstoken=" + _token),
                JsonConvert.SerializeObject(new Dictionary<string, string> {
                    { "accesstoken", _token },
                    { "listid" , listId },
                    { "email" , mail },
                    { "fname" , fname },
                    { "lname" , lname },
                    { "city" , city },
                    { "country" , country },
                    { "other_id" , username },
                })
            ));

            if (j["error"] != null)
            {
                throw new Exception(j["error"].ToString());
            }

            if (j["subscriberid"] == null)
            {
                throw new Exception(j.ToString());
            }

            return j["subscriberid"].ToString();
        }
    }
}
