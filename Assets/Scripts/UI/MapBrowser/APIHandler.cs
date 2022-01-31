using System.Net;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace NotReaper.MapBrowser
{
    public class APIHandler
    {
        private const string ApiUrl = "https://maudica.com/api/maps?per_page=20";
        private const string Issuer = "O=Let's Encrypt, C=US";
        private const string Subject = "CN=*.maudica.com";
        private const string DownloadUrlFormat = "https://maudica.com/maps/{0}/download";
        private const string PreviewUrlFormat = "https://maudica.com/maps/{0}/preview";
        private static string DownloadFolder = Path.Combine(Application.dataPath, @"../", "downloads");
        private static string SavesFolder = Path.Combine(Application.dataPath, @"../", "saves");


        public APIHandler()
        {
            if (!Directory.Exists(DownloadFolder)) Directory.CreateDirectory(DownloadFolder);
        }

        internal string GetRequestUrl(string searchText, CurationState curationState, int page, bool[] difficulties)
        {
            string webSearch;

            webSearch = searchText == null || searchText == "" ? "" : "&search=" + WebUtility.UrlEncode(searchText);
            string webPage = page == 1 ? "" : "&page=" + page.ToString();
            string webDifficulty = "";
            if (difficulties[0]) webDifficulty += "&difficulties%5B%5D=beginner";
            if (difficulties[1]) webDifficulty += "&difficulties%5B%5D=moderate";
            if (difficulties[2]) webDifficulty += "&difficulties%5B%5D=advanced";
            if (difficulties[3]) webDifficulty += "&difficulties%5B%5D=expert";
            string curated = curationState == CurationState.None ? "" : "&curated=true";
            return ApiUrl + webSearch + webPage + webDifficulty + curated;
        }

        internal APISongList APISearch(string requestUrl)
        {            
            return GetSongList(requestUrl);          
        }

        internal delegate void OnSearchDone(APISongList songlist);
        internal IEnumerator APISearch(string requestUrl, OnSearchDone callback)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(requestUrl))
            {
                www.timeout = 20;
                yield return www.SendWebRequest();
                callback.Invoke(JsonConvert.DeserializeObject<APISongList>(www.downloadHandler.text));
            }          
        }

        private APISongList GetSongList(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            UpdateValidationCB(request);

            string htmlResponse = string.Empty;
            using (WebResponse response = request.GetResponse())
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    htmlResponse = reader.ReadToEnd();
                    reader.Close();
                }
                response.Close();
            }
            return JsonConvert.DeserializeObject<APISongList>(htmlResponse);
        }

        private void UpdateValidationCB(HttpWebRequest request)
        {
            // unity is not happy with our chain of certificates for whatever reason,
            // so we directly compare public keys instead to identify maudica.com
            request.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) =>
            {
                return certificate.Issuer.Contains(Issuer) && certificate.Subject.Equals(Subject);
            };
        }

        internal Task<MapData> DownloadSelectedMap(MapData map)
        {
            
            string downloadsPath = Path.Combine(DownloadFolder, map.Filename);
            return Task.Run(() => DownloadSongAsync(map, string.Format(DownloadUrlFormat, map.ID), downloadsPath));
            
        }

        public delegate void OnTaskCompleteDelegate(MapData data, bool success);
        public IEnumerator DownloadSong(MapData map, OnTaskCompleteDelegate callback)
        {
            bool success;
            string outputFile = Path.Combine(DownloadFolder, map.Filename);
            if (File.Exists(outputFile))
            {
                callback.Invoke(map, true);
                yield break;
            }
            string downloadUrl = string.Format(DownloadUrlFormat, map.ID);
            DownloadHandlerFile handler = new DownloadHandlerFile(outputFile);
            handler.removeFileOnAbort = true;
            UnityWebRequest www = new UnityWebRequest(downloadUrl);
            www.timeout = 30;
            www.downloadHandler = handler;
            yield return www.SendWebRequest();
           
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
                success = false;
            }
            else
            {
                success = true;
            }
            handler.Dispose();
            www.Dispose();           
            callback.Invoke(map, success);
        }

        private async Task<MapData> DownloadSongAsync(MapData map, string downloadUrl, string downloadsPath)
        {
            


            byte[] results = null;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(downloadUrl);
            request.ReadWriteTimeout = 20000; //20s timeout
            UpdateValidationCB(request);
            try
            {
                using (WebResponse resposne = await request.GetResponseAsync())
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        resposne.GetResponseStream().CopyTo(stream);
                        results = stream.ToArray();
                        stream.Close();
                    }
                    resposne.Close();
                }
                if (results != null)
                {
                    File.WriteAllBytes(downloadsPath, results);
                    return map;
                }
                return null;
            }
            catch
            {
                return null;
            }
          
        }


    }
    [Serializable]
    internal class APISongList
    {
        public Song[] maps = null;
        public bool has_more = false;
        public int count = 0;
    }

    [Serializable]
    internal class Song
    {
        public int id = 0;
        public string created_at = null;
        public string updated_at = null;
        public string title = null;
        public string artist = null;
        public string author = null;
        public string[] difficulties = null;
        public string description = null;
        public string embed_url = null;
        public string filename = null;
        public bool curated = false;
    }

}

