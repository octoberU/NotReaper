using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Michsky.UI.ModernUIPack;
using NotReaper.UI.UpdaterWindow;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Version = System.Version;


namespace NotReaper.Managers {
	public class AutoUpdater : MonoBehaviour {
		public static AutoUpdater I;
		
		
		
		AutoUpdaterJSON updateData = new AutoUpdaterJSON();
		public TextMeshProUGUI downloadText;

		private bool isBeta = true;

		private void Start() {
			I = this;

            if (isBeta)
            {
				Debug.Log("Auto updates are disabled for beta.");
            }
            else
            {
				if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor) {
					StartCoroutine(Init());
				}
		 		UpdaterWindow.I.downloadSlider.currentPercent = 0;
				//downloadSlider.gameObject.SetActive(false);
            }
			



		}







		public IEnumerator DoUpdate(AutoUpdaterJSON data) {

			UpdaterWindow.I.downloadSlider.gameObject.SetActive(true);
			
			if (Application.isEditor) {
				downloadText.SetText("Editor build detected, skipping install process.");
				yield return new WaitForSeconds(2f);
				Application.Quit();
				yield break;
			}
			
			downloadText.SetText("Downloading...");
			
			
			UnityWebRequest www = UnityWebRequest.Get(data.downloadLink);

			string savePath = Path.Combine(Application.dataPath, @"..\", "update.zip");
			www.downloadHandler = new DownloadHandlerFile(savePath);
			
			var operation = www.SendWebRequest();


			while (!operation.isDone) {
				UpdaterWindow.I.downloadSlider.currentPercent = operation.progress * 100f;
				yield return null;
			}


			if(www.result != UnityWebRequest.Result.Success) {
				Debug.Log(www.error);
			}
			else {
				Debug.Log("File successfully downloaded and saved to " + savePath);
				UpdaterWindow.I.downloadSlider.currentPercent = 100;
				downloadText.SetText("Download complete, installing...");

			}



			
			//Launch unzipper
			
			www.downloadHandler.Dispose();
			www.Dispose();
			
			yield return new WaitForSeconds(2f);

			Process.Start(Path.Combine(Application.streamingAssetsPath, "Installer", "NotReaperInstaller.exe"));
			
			Application.Quit();


		}
		

		

		private IEnumerator Init() {
			string url = "https://raw.githubusercontent.com/octoberU/NotReaper/master/updates.json";
			
			UnityWebRequest www = UnityWebRequest.Get(url);
			yield return www.SendWebRequest();
 
			if(www.result != UnityWebRequest.Result.Success) {
				Debug.Log(www.error);
			}
			else {
				updateData = JsonUtility.FromJson<AutoUpdaterJSON>(www.downloadHandler.text);
				HandleUpdate(updateData);
			}
			
			
		}


		private void HandleUpdate(AutoUpdaterJSON data) {


			if (!IsNewUpdate(data))
            {
				return;
            }

			if (IsIgnoringUpdate(data)) return;
			
			
			UpdaterWindow.I.gameObject.SetActive(true);
			UpdaterWindow.I.Activate(data);
			

		}


		private bool IsIgnoringUpdate(AutoUpdaterJSON data) {

			if (data.latestVersion == PlayerPrefs.GetString("ignoredVersion")) return true;

			else return false;
		}
		
		
		private bool IsNewUpdate(AutoUpdaterJSON data) {
			Version newVer = Version.Parse(data.latestVersion);
			
			Version currentVer = Version.Parse(Application.version);
			

			if (newVer.Build > currentVer.Build && newVer.Minor >= currentVer.Minor && newVer.Major >= currentVer.Major) {
				return true;
			}
			
			else if (newVer.Minor > currentVer.Minor && newVer.Major >= currentVer.Major) return true;
			
			else if (newVer.Major > currentVer.Major) return true;


			
			return false;

		}
		
		
		
		
	}


	[Serializable]
	public class AutoUpdaterJSON {
		public string latestVersion;
		public string downloadLink;
		public string changelog;
		public bool forced;
	}
	
}