using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper.Managers;
using NotReaper.Models;
using UnityEngine;



namespace NotReaper {


    public class NRDiscordPresence : Singleton<NRDiscordPresence> {

        public DifficultyManager difficultyManager;

        public bool showTime;
        //public bool showTargetAmount;

        public DiscordPresence presence;

        private void Start()
            => NRSettings.onSettingsSaved += (config) => gameObject.SetActive(config.useDiscordRichPresence);
        

        public void InitPresence() {

            NRSettings.OnLoad(() => {
                
                if (!NRSettings.config.useDiscordRichPresence) {
                    gameObject.SetActive(false);
                    return;
                }
                
                showTime = NRSettings.config.showTimeElapsed;

                presence.details = "In Menus";
                presence.state = "";

                presence.startTime =
                    showTime ? new DiscordTimestamp(Time.realtimeSinceStartup) : DiscordTimestamp.Invalid;

                float duration = 0;
                presence.endTime = duration > 0
                    ? new DiscordTimestamp(Time.realtimeSinceStartup + duration)
                    : DiscordTimestamp.Invalid;



                //Register to a presence change
                DiscordManager.current.events.OnPresenceUpdate.AddListener((message) => {
                    Debug.Log("Received a new presence! Current App: " + message.ApplicationID + ", " + message.Name);
                });

                DiscordManager.current.SetPresence(presence);
            });
        }




        public void UpdatePresenceDifficulty(Difficulty difficulty) {

            switch (difficulty) {
                case Difficulty.Expert:
                    presence.smallAsset = new DiscordAsset() {
                        image = "diffexpert",
                        tooltip = "Expert"
                    };
                    break;

                case Difficulty.Advanced:
                    presence.smallAsset = new DiscordAsset() {
                        image = "diffadvanced",
                        tooltip = "Advanced"
                    };
                    break;

                case Difficulty.Standard:
                    presence.smallAsset = new DiscordAsset() {
                        image = "diffstandard",
                        tooltip = "Standard"
                    };
                    break;

                case Difficulty.Beginner:
                    presence.smallAsset = new DiscordAsset() {
                        image = "diffeasy",
                        tooltip = "Beginner"
                    };
                    break;
            }
            
            if (!NRSettings.config.useDiscordRichPresence) return;

            DiscordManager.current.SetPresence(presence);
        }

        public void UpdatePresenceSongName(string name) {
            presence.details = "Mapping:";
            presence.state = name;
            
            if (!NRSettings.config.useDiscordRichPresence) return;
            DiscordManager.current.SetPresence(presence);
        }




        public void UpdatePresence() {
            presence.state = "NotReaper";
            presence.details = ("Mapping: ");
            presence.startTime = showTime ? new DiscordTimestamp(Time.realtimeSinceStartup) : DiscordTimestamp.Invalid;

            presence.largeAsset = new DiscordAsset() {
                image = "logosquare",
                tooltip = "NotReaper v0.3"
            };
            presence.smallAsset = new DiscordAsset() {
                image = "diffstandard",
                tooltip = "Standard"
            };

            float duration = 0;
            presence.endTime = duration > 0 ? new DiscordTimestamp(Time.realtimeSinceStartup + duration) : DiscordTimestamp.Invalid;
        }
    }


}