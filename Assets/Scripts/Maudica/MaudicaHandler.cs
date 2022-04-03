using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using NotReaper.MapBrowser;
using AudicaTools;
using NotReaper.Notifications;

namespace NotReaper.Maudica
{
    public class MaudicaHandler : MonoBehaviour
    {
        private const string MAUDICA_URL = @"https://maudica.com/api/";
        private const string ACCOUNT_ENDPOINT = @"account/me/";
        private const string MAPS_ENDPOINT = @"maps/";
        private const string APPROVE_ENDPOINT = @"approve/";
        private const string UNAPPROVE_ENDPOINT = @"unapprove/";
        private const string VOTE_UP_ENDPOINT = @"vote-up/";
        private const string VOTE_DOWN_ENDPOINT = @"vote-down/";

        public static bool IsCurator { get; private set; }
        public static bool HasToken => NRSettings.config.maudicaToken.Length > 3;
        private Account userAccount;
        private static Audica audica;
        private void Start()
        {
            NRSettings.OnLoad(() => 
            {
                StartCoroutine(CheckPermissions());
            });
        }

        private IEnumerator CheckPermissions()
        {
            if (!HasToken) yield break;
            Debug.Log("Checking permissions");
            string requestUrl = MAUDICA_URL + ACCOUNT_ENDPOINT + @"?api_token=" + NRSettings.config.maudicaToken;
            using UnityWebRequest www = UnityWebRequest.Get(requestUrl);
            www.timeout = 60;
            yield return www.SendWebRequest();
            if(www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                userAccount = JsonConvert.DeserializeObject<Account>(www.downloadHandler.text);
                if (userAccount.roles != null && userAccount.roles.Any(role => role == "curator"))
                {
                    IsCurator = true;
                }
            }
        }


        public static IEnumerator GetMap(string filepath, Action<Song> response)
        {
            if (!HasToken) yield break;

            string requestUrl = MAUDICA_URL + MAPS_ENDPOINT;
            audica = new Audica(filepath);
            using UnityWebRequest www = UnityWebRequest.Get(requestUrl);
            yield return www.SendWebRequest();
            if(www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                var songList = JsonConvert.DeserializeObject<APISongList>(www.downloadHandler.text);
                bool exists = songList.count > 0;
                Song song = exists ? songList.maps[0] : new Song();
                if (!exists) audica = null;
                response?.Invoke(song);
            }
                      
        }

        public static IEnumerator ApproveMap(Action onComplete = null)
        {
            if (!IsCurator) yield break;

            string requestUrl = MAUDICA_URL + MAPS_ENDPOINT + APPROVE_ENDPOINT;
            requestUrl += "?audica_id=" + audica.GetHashedSongID();
            requestUrl += "&api_token=" + NRSettings.config.maudicaToken;
            using UnityWebRequest www = UnityWebRequest.Get(requestUrl);
            www.method = "PATCH";
            www.timeout = 60;
            yield return www.SendWebRequest();
            if(www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                NotificationCenter.SendNotification("Map approved!", NotificationType.Success);
                onComplete?.Invoke();
            }
        }

        public static IEnumerator UnapproveMap(Action onComplete = null)
        {
            if (!IsCurator) yield break;
            string requestUrl = MAUDICA_URL + MAPS_ENDPOINT + UNAPPROVE_ENDPOINT;
            requestUrl += "?audica_id=" + audica.GetHashedSongID();
            requestUrl += "&api_token=" + NRSettings.config.maudicaToken;

            using UnityWebRequest www = UnityWebRequest.Get(requestUrl);
            www.method = "PATCH";
            www.timeout = 60;
            yield return www.SendWebRequest();
            if(www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                NotificationCenter.SendNotification("Map unapproved!", NotificationType.Success);
                onComplete?.Invoke();
            }
        }

        public static IEnumerator VoteMapUp(Action onComplete = null)
        {
            if (!HasToken) yield break;
            string requestUrl = MAUDICA_URL + MAPS_ENDPOINT + VOTE_UP_ENDPOINT;
            requestUrl += "?audica_id=" + audica.GetHashedSongID();
            requestUrl += "&api_token=" + NRSettings.config.maudicaToken;

            using UnityWebRequest www = UnityWebRequest.Get(requestUrl);
            www.method = "PATCH";
            www.timeout = 60;
            yield return www.SendWebRequest();
            if(www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                NotificationCenter.SendNotification("Map upvoted!", NotificationType.Success);
                onComplete?.Invoke();
            }
        }

        public static IEnumerator VoteMapDown(Action onComplete = null)
        {
            if (!HasToken) yield break;
            string requestUrl = MAUDICA_URL + MAPS_ENDPOINT + VOTE_DOWN_ENDPOINT;
            requestUrl += "?audica_id=" + audica.GetHashedSongID();
            requestUrl += "&api_token=" + NRSettings.config.maudicaToken;

            using UnityWebRequest www = UnityWebRequest.Get(requestUrl);
            www.method = "PATCH";
            www.timeout = 60;
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                NotificationCenter.SendNotification("Map downvoated!", NotificationType.Success);
                onComplete?.Invoke();
            }
        }

        private class Account
        {
            public string discord_username = "";
            public string[] roles;

            public Account(string discord_username, string[] roles)
            {
                this.discord_username = discord_username;
                this.roles = roles;
            }
        }
    }
}

