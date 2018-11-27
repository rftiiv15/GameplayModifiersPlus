﻿using IllusionPlugin;
using UnityEngine.SceneManagement;
using UnityEngine;
using System;
using System.Collections;
using System.Media;
using System.Linq;
using AsyncTwitch;
using IllusionInjector;
using TMPro;
namespace GamePlayModifiersPlus
{
    public class Plugin : IPlugin
    {
        public string Name => "GameplayModifiersPlus";
        public string Version => "0.0.1";

        public static bool gnomeOnMiss = false;
        SoundPlayer gnomeSound = new SoundPlayer(Properties.Resources.gnome);
        SoundPlayer beepSound = new SoundPlayer(Properties.Resources.Beep);
        bool soundIsPlaying = false;
        public static AudioTimeSyncController AudioTimeSync { get; private set; }
        private static AudioSource _songAudio;
        public static bool isValidScene = false;
        public static bool gnomeActive = false;
        public static bool twitchStuff = false;
        public static bool superHot = false;
        public static PlayerController player;
        public static Saber leftSaber;
        public static Saber rightSaber;
        public static bool playerInfo = false;
        public static float prevLeftPos;
        public static float prevRightPos;
        public static float prevHeadPos;
        public static float speedPitch = 1;
        public static bool calculating = false;
        public static bool startSuperHot;
        public static bool swapSabers;
        public static bool bulletTime = false;
        public static bool paused = false;
        private static Cooldowns _cooldowns;
        public static TMP_Text ppText;
        public static string rank;
        public static string pp;
        public static float currentpp;
        public static float oldpp = 0;
        public static int currentRank;
        public static int oldRank;
        public static float deltaPP;
        public static int deltaRank;
        public static bool chatDelta;
        public static bool firstLoad = true;
        VRController leftController;
        VRController rightController;



        private static bool _hasRegistered = false;

        public void OnApplicationStart()
        {
            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            gnomeOnMiss = ModPrefs.GetBool("GameplayModifiersPlus", "gnomeOnMiss", false, true);
            superHot = ModPrefs.GetBool("GameplayModifiersPlus", "superHot", false, true);
            bulletTime = ModPrefs.GetBool("GameplayModifiersPlus", "bulletTime", false, true);
            twitchStuff = ModPrefs.GetBool("GameplayModifiersPlus", "twitchStuff", false, true);
            swapSabers = ModPrefs.GetBool("GameplayModifiersPlus", "swapSabers", false, true);
            chatDelta = ModPrefs.GetBool("GameplayModifiersPlus", "chatDelta", false, true);
            _cooldowns = new Cooldowns();
        }

        private void TwitchConnection_OnMessageReceived(TwitchConnection arg1, TwitchMessage message)
        {
            Log("Message Recieved, AsyncTwitch currently working");
            //Status check message
            if (message.Content.ToLower().Contains("!gm status"))
            {
                beepSound.Play();
                TwitchConnection.Instance.SendChatMessage("Currently Not Borked");
            }
            if(message.Content.ToLower().Contains("!gm help"))
            {
                TwitchConnection.Instance.SendChatMessage("Include !gm followed by a command in your message while the streamer has twitch mode on to mess with their game." +
                    " Currently supported commands: status - check the plugin is still working, faster, slower. !gm pp to show the streamer's current global rank");
            }

            if (message.Content.ToLower().Contains("!gm pp"))
            {
                if(currentpp != 0)
                TwitchConnection.Instance.SendChatMessage("Streamer Rank: #" + currentRank + ". Streamer pp: " + currentpp + "pp");
                else
                    TwitchConnection.Instance.SendChatMessage("Currently do not have streamer info");
            }
            if (twitchStuff == true && isValidScene == true && message.Content.StartsWith("!gm"))
            {
                if (message.Author != null)

                    if (!message.Author.DisplayName.Contains("Nightbot"))
                    {
                        if (!_cooldowns.GetCooldown("Global"))
                        {
                            //Speed commands
                            if (!_cooldowns.GetCooldown("Speed"))
                            {
                                //Speed up
                                if (message.BitAmount >= 0 || message.Author.IsMod)
                                    if (message.Content.Contains("!gm faster"))
                                    {
                                        beepSound.Play();
                                        speedPitch += 0.1f;
                                        if (speedPitch >= 2f) speedPitch = 2f;
                                        ReflectionUtil.SetProperty(typeof(PracticePlugin.Plugin), "TimeScale", speedPitch);
                                        Time.timeScale = speedPitch;
                                        Log("Valid Message");
                                        SharedCoroutineStarter.instance.StartCoroutine(CoolDown(5f, "Global", "Speeding Up."));
                                    }

                                //Speed Down
                                if (message.BitAmount >= 0 || message.Author.IsMod)
                                    if (message.Content.ToLower().Contains("!gm slower"))
                                    {
                                        beepSound.Play();
                                        speedPitch -= 0.1f;
                                        if (speedPitch <= .25f) speedPitch = .1f;
                                        ReflectionUtil.SetProperty(typeof(PracticePlugin.Plugin), "TimeScale", speedPitch);
                                        Time.timeScale = speedPitch;
                                        Log("Valid Message");
                                        SharedCoroutineStarter.instance.StartCoroutine(CoolDown(5f, "Global", "Slowing Down."));
                                    }
                            }
                            else
                            {
                                TwitchConnection.Instance.SendChatMessage("Speed Command Cooldown Active, please wait.");
                            }
                            //Gnome message
                            if (message.BitAmount >= 1000)
                                if (message.Content.ToLower().Contains("!gm saberwave") || message.Content.ToLower().Contains("!gm s a b e r w a v e"))
                                {

                                    SharedCoroutineStarter.instance.StopAllCoroutines();
                                    SharedCoroutineStarter.instance.StartCoroutine(SpecialEvent());
                                    Log("Gnoming");
                                    SharedCoroutineStarter.instance.StartCoroutine(CoolDown(45f, "Global", "Gnoming."));
                                }
                            //Saber Swap message
                            if (message.BitAmount >= 10000 && message.Content.ToLower().Contains("!gm swap"))
                            {
                                SharedCoroutineStarter.instance.StartCoroutine(CoolDown(60f, "Global", "Swapping Sabers."));
                                SharedCoroutineStarter.instance.StartCoroutine(SwapSabers(leftSaber, rightSaber));
                                SharedCoroutineStarter.instance.StartCoroutine(Pause(5f));
                                speedPitch = 1;
                                Time.timeScale = 1;
                            }

                        }
                        else
                        {
                          
                            TwitchConnection.Instance.SendChatMessage("Global Cooldown Active, please wait.");
                        }
                        //Kyle Messages
                        if (message.Author.DisplayName.Contains("Kyle1413K"))
                    {
                        if (message.Content.Contains("gnome"))
                        {
                            Log("Kyle Message");
                            SharedCoroutineStarter.instance.StopAllCoroutines();
                            SharedCoroutineStarter.instance.StartCoroutine(SpecialEvent());
                            Log("Gnoming");
                        }
                        if (message.Content.ToLower().Contains("slow down"))
                        {
                            Log("Kyle Message");
                            ReflectionUtil.SetProperty(typeof(PracticePlugin.Plugin), "TimeScale", 0.4f);
                            Time.timeScale = 0.4f;
                                speedPitch = 1;
                            }
                        if (message.Content.ToLower().Contains("faster!"))
                        {
                            Log("Kyle Message");
                            ReflectionUtil.SetProperty(typeof(PracticePlugin.Plugin), "TimeScale", 1.5f);
                            Time.timeScale = 1.5f;
                                speedPitch = 1;
                        }
                        if(message.Content.ToLower().Contains("swap!"))
                            {
                                Log("Kyle Message");
                                SharedCoroutineStarter.instance.StartCoroutine(CoolDown(60f, "Global", ""));
                                SharedCoroutineStarter.instance.StartCoroutine(SwapSabers(leftSaber, rightSaber));
                                SharedCoroutineStarter.instance.StartCoroutine(Pause(5f));
                                speedPitch = 1;
                                Time.timeScale = 1;
                                
                            }
                        if(message.Content.ToLower().Contains("!resetcooldowns"))
                            {
                                TwitchConnection.Instance.SendChatMessage("Cooldowns Reset.");
                                _cooldowns.ResetCooldowns();
                            }
                      
                    }
                }
                else
                    Log("Bot message");

            }
        }

        private void SceneManagerOnActiveSceneChanged(Scene arg0, Scene scene)
        {
            _cooldowns.ResetCooldowns();
            Time.timeScale = 1;
            speedPitch = 1;
            if (soundIsPlaying == true)
                gnomeSound.Stop();
            soundIsPlaying = false;
            SharedCoroutineStarter.instance.StopAllCoroutines();
            isValidScene = false;
            playerInfo = false;
            if (scene.name == "Menu")
            {
                if(_hasRegistered == false)
                {
                TwitchConnection.Instance.StartConnection();
                TwitchConnection.Instance.RegisterOnMessageReceived(TwitchConnection_OnMessageReceived);
                _hasRegistered = true;
                }

                var controllers = Resources.FindObjectsOfTypeAll<VRController>();
                foreach (VRController controller in controllers)
                {
                    //        Log(controller.ToString());
                    if (controller.ToString() == "ControllerLeft (VRController)")
                        leftController = controller;
                    if (controller.ToString() == "ControllerRight (VRController)")
                        rightController = controller;
                }
                Log("Left:" + leftController.ToString());
                Log("Right: " + rightController.ToString());

                var gnomeOption = GameOptionsUI.CreateToggleOption("Gnome on miss");
                gnomeOption.GetValue = ModPrefs.GetBool("GameplayModifiersPlus", "gnomeOnMiss", false, true);
                gnomeOption.OnToggle += (gnomeOnMiss) => { ModPrefs.SetBool("GameplayModifiersPlus", "gnomeOnMiss", gnomeOnMiss); Log("Changed Modprefs value"); };

                var superHotOption = GameOptionsUI.CreateToggleOption("SuperHot");
                superHotOption.GetValue = ModPrefs.GetBool("GameplayModifiersPlus", "superHot", false, true);
                superHotOption.OnToggle += (superHot) => { ModPrefs.SetBool("GameplayModifiersPlus", "superHot", superHot); Log("Changed Modprefs value"); };

                var bulletTimeOption = GameOptionsUI.CreateToggleOption("Bullet Time");
                bulletTimeOption.GetValue = ModPrefs.GetBool("GameplayModifiersPlus", "bulletTime", false, true);
                bulletTimeOption.OnToggle += (bulletTime) => { ModPrefs.SetBool("GameplayModifiersPlus", "bulletTime", bulletTime); Log("Changed Modprefs value"); };

                var twitchStuffOption = GameOptionsUI.CreateToggleOption("Twitch Chat");
                twitchStuffOption.GetValue = ModPrefs.GetBool("GameplayModifiersPlus", "twitchStuff", false, true);
                twitchStuffOption.OnToggle += (twitchStuff) => { ModPrefs.SetBool("GameplayModifiersPlus", "twitchStuff", twitchStuff); Log("Changed Modprefs value"); };

                var swapSabersOption = GameOptionsUI.CreateToggleOption("Swap Sabers");
                swapSabersOption.GetValue = ModPrefs.GetBool("GameplayModifiersPlus", "swapSabers", false, true);
                swapSabersOption.OnToggle += (swapSabers) => { ModPrefs.SetBool("GameplayModifiersPlus", "swapSabers", swapSabers) ; Log("Changed Modprefs value"); };

                var chatDeltaOption = GameOptionsUI.CreateToggleOption("Chat Delta");
                chatDeltaOption.GetValue = ModPrefs.GetBool("GameplayModifiersPlus", "chatDelta", false, true);
                chatDeltaOption.OnToggle += (chatDelta) => { ModPrefs.SetBool("GameplayModifiersPlus", "chatDelta", chatDelta); Log("Changed Modprefs value"); };
            }
        }

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode arg1)
        {
            gnomeOnMiss = ModPrefs.GetBool("GameplayModifiersPlus", "gnomeOnMiss", false, true);
            superHot = ModPrefs.GetBool("GameplayModifiersPlus", "superHot", false, true);
            bulletTime = ModPrefs.GetBool("GameplayModifiersPlus", "bulletTime", false, true);
            twitchStuff = ModPrefs.GetBool("GameplayModifiersPlus", "twitchStuff", false, true);
            swapSabers = ModPrefs.GetBool("GameplayModifiersPlus", "swapSabers", false, true);
            chatDelta = ModPrefs.GetBool("GameplayModifiersPlus", "chatDelta", false, true);
            if (scene.name == "Menu")
            {


                var texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
                foreach (TMP_Text text in texts)
                {
                    if (text.ToString() == "PP (TMPro.TextMeshPro)")
                    {
                        ppText = text;
                        SharedCoroutineStarter.instance.StartCoroutine(GrabPP());
                        break;

                    }

                }


            }
            if (bulletTime == true)
                superHot = false;
            if (twitchStuff == true)
            {
                superHot = false;
                bulletTime = false;
                gnomeOnMiss = false;
            }


            if (scene.name == "GameCore")
            {
                ReflectionUtil.SetProperty(typeof(PracticePlugin.Plugin), "TimeScale", 1f);
                isValidScene = true;
                AudioTimeSync = Resources.FindObjectsOfTypeAll<AudioTimeSyncController>().FirstOrDefault();
                if (AudioTimeSync != null)
                {
                    _songAudio = AudioTimeSync.GetField<AudioSource>("_audioSource");
                    if (_songAudio != null)
                        Log("Audio not null");
                    Log("Object Found");
                }
                //Get Sabers
                player = Resources.FindObjectsOfTypeAll<PlayerController>().FirstOrDefault();
                if (player != null)
                {
                    leftSaber = player.leftSaber;
                    rightSaber = player.rightSaber;
                    playerInfo = true;
                }
                else
                {
                    playerInfo = false;
                    Log("Player is null");
                }
                Log(leftSaber.saberBladeBottomPos.ToString());
                Log(leftSaber.saberBladeTopPos.ToString());
                if(swapSabers)
                SharedCoroutineStarter.instance.StartCoroutine(SwapSabers(leftSaber, rightSaber));

                if (gnomeOnMiss == true)
                {

                    BeatmapObjectSpawnController[] beatmapObjectSpawnControllers = Resources.FindObjectsOfTypeAll<BeatmapObjectSpawnController>();
                    BeatmapObjectSpawnController beatmapObjectSpawnController = beatmapObjectSpawnControllers.Length > 0 ? beatmapObjectSpawnControllers?[0] : null;
                    if (beatmapObjectSpawnController != null)
                    {
                        beatmapObjectSpawnController.noteWasMissedEvent += delegate (BeatmapObjectSpawnController beatmapObjectSpawnController2, NoteController noteController)
                        {
                            if (noteController.noteData.noteType != NoteType.Bomb)
                            {
                                try
                                {
                                    SharedCoroutineStarter.instance.StopAllCoroutines();
                                    SharedCoroutineStarter.instance.StartCoroutine(SpecialEvent());
                                    Log("Gnoming");
                                }
                                catch (Exception ex)
                                {
                                    Log(ex.ToString());
                                }
                            }
                        };

                        beatmapObjectSpawnController.noteWasCutEvent += delegate (BeatmapObjectSpawnController beatmapObjectSpawnController2, NoteController noteController, NoteCutInfo noteCutInfo)
                        {
                            if (!noteCutInfo.allIsOK)
                            {
                                SharedCoroutineStarter.instance.StopAllCoroutines();
                                SharedCoroutineStarter.instance.StartCoroutine(SpecialEvent());
                                Log("Gnoming");
                            }

                        };

                    }
                }
                if(superHot == true)
                {
                    startSuperHot = false;
                    SharedCoroutineStarter.instance.StartCoroutine(Wait(1f));

                }






            }
        }

        public void OnApplicationQuit()
        {
            SceneManager.activeSceneChanged -= SceneManagerOnActiveSceneChanged;
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
        }

        public void OnLevelWasLoaded(int level)
        {

        }

        public void OnLevelWasInitialized(int level)
        {
            //test
        }
        
        public void OnUpdate()
        {
       
            if (soundIsPlaying == true && _songAudio != null && isValidScene == true)
            {
                ReflectionUtil.SetProperty(typeof(PracticePlugin.Plugin), "TimeScale", 0f);
                Time.timeScale = 0f;
                return;
            }

            if (bulletTime == true && isValidScene == true && soundIsPlaying == false)
            {
                speedPitch = 1 - (leftController.triggerValue + rightController.triggerValue) / 2;
                ReflectionUtil.SetProperty(typeof(PracticePlugin.Plugin), "TimeScale", speedPitch);
                Time.timeScale = speedPitch;
                return;
            }
        

            if (superHot == true && playerInfo == true && soundIsPlaying == false && isValidScene == true && startSuperHot == true)
            {
                speedPitch = (leftSaber.bladeSpeed / 15 + rightSaber.bladeSpeed / 15) / 1.5f;
                if (speedPitch > 1)
                    speedPitch = 1;
                ReflectionUtil.SetProperty(typeof(PracticePlugin.Plugin), "TimeScale", speedPitch);
                Time.timeScale = speedPitch;
       /*     if (calculating == false)
                {
                prevLeftPos = leftSaber.handlePos.magnitude;
                prevRightPos = rightSaber.handlePos.magnitude;
                prevHeadPos = player.headPos.magnitude;
                prevRotHead = player.GetField<Transform>("_headTransform").rotation.eulerAngles.magnitude;
                prevSpeedL = leftSaber.bladeSpeed;
                prevSpeedR = rightSaber.bladeSpeed;
                SharedCoroutineStarter.instance.StartCoroutine(Delta());
                    ReflectionUtil.SetProperty(typeof(PracticePlugin.Plugin), "TimeScale", speedPitch);                }
*/      
            }
            else
            {
                Time.timeScale = 1f;
            }
            if (playerInfo == true)
                if(player.disableSabers == true)
                    Time.timeScale = 1;
        }
    
        public void OnFixedUpdate()
        {
        }


        private IEnumerator SpecialEvent()
        {
            gnomeActive = true;
            yield return new WaitForSecondsRealtime(0.1f);
            ReflectionUtil.SetProperty(typeof(PracticePlugin.Plugin), "TimeScale", 0f);
            Time.timeScale = 0f;
            gnomeSound.Load();
            gnomeSound.Play();
            soundIsPlaying = true;
            Log("Waiting");
            yield return new WaitForSecondsRealtime(16f);
            if(isValidScene == true)
            {
            soundIsPlaying = false;
                ReflectionUtil.SetProperty(typeof(PracticePlugin.Plugin), "TimeScale", 1f);
                Time.timeScale = 1f;
            Log("Unpaused");
            gnomeActive = false;
            }        
        }
      
        private static IEnumerator Wait(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            startSuperHot = true;
        }

        private static IEnumerator CoolDown(float waitTime, string cooldown, string message)
        {
            _cooldowns.SetCooldown(true, cooldown);
            TwitchConnection.Instance.SendChatMessage(message + " " + cooldown + " Cooldown Active for " + waitTime.ToString() + "seconds");
            yield return new WaitForSeconds(waitTime);
            _cooldowns.SetCooldown(false, cooldown);
      //      TwitchConnection.Instance.SendChatMessage(cooldown + " Cooldown Deactivated, have fun!");
        }

        private IEnumerator Pause(float waitTime)
        {
            paused = true;
            ReflectionUtil.SetProperty(typeof(PracticePlugin.Plugin), "TimeScale", 0f);
            Time.timeScale = 0f;
            Log("Pausing");
            yield return new WaitForSecondsRealtime(waitTime);
            if (isValidScene == true)
            {
                ReflectionUtil.SetProperty(typeof(PracticePlugin.Plugin), "TimeScale", 1f);
                Time.timeScale = 1f;
                Log("Unpaused");
                paused = false;
            }
        }

        public static bool IsModInstalled(string modName)
        {
            foreach (IPlugin p in PluginManager.Plugins)
            {
                if (p.Name == modName)
                {
                    return true;
                }
            }
            return false;
        }

        public static void Log(string message)
        {
            Console.WriteLine("[{0}] {1}", "GameplayModifiersPlus", message);
        }
        public IEnumerator SwapSabers(Saber saber1, Saber saber2)
        {
            yield return new WaitForSecondsRealtime(0f);
            beepSound.Play();
            Transform transform1 = saber1.transform.parent.transform;

            Transform transform2 = saber2.transform.parent.transform;

            saber2.transform.parent = transform1;
            saber1.transform.parent = transform2;
            saber2.transform.SetPositionAndRotation(transform1.transform.position, player.rightSaber.transform.parent.rotation);
            saber1.transform.SetPositionAndRotation(transform2.transform.position, player.leftSaber.transform.parent.rotation);
        }
        public IEnumerator GrabPP()
        {
            if(firstLoad == true)
            {
                yield return new WaitForSecondsRealtime(20);
            }
            else
                yield return new WaitForSecondsRealtime(10);
            if (!ppText.text.Contains("html"))
            Log(ppText.text);
            if (!(ppText.text.Contains("Refresh") || ppText.text.Contains("html")))
            {
                rank = ppText.text.Split('#', '<')[1];
                pp = ppText.text.Split('(', 'p')[1];
                currentpp = float.Parse(pp, System.Globalization.CultureInfo.InvariantCulture);
                currentRank = int.Parse(rank, System.Globalization.CultureInfo.InvariantCulture);
                Log("Rank: " + currentRank);
                Log("PP: " + currentpp);
                if (firstLoad == true)
                    if (chatDelta)
                        TwitchConnection.Instance.SendChatMessage("Loaded. PP: " + currentpp.ToString("F3") + " pp. Rank: " + currentRank);

                if (oldpp != 0)
                {
                    deltaPP = 0;
                    deltaRank = 0;
                    deltaPP = currentpp - oldpp;
                    deltaRank = currentRank - oldRank;
                    
                    if (deltaPP != 0 || deltaRank != 0)
                    {
                        ppText.enableWordWrapping = false;
                        if (deltaRank < 0)
                        {
                            if (deltaRank == -1)
                            {
                                if (chatDelta)
                                    TwitchConnection.Instance.SendChatMessage("Gained " + deltaPP.ToString("F3") + " pp. Gained 1 Rank.");
                                ppText.text += " Change: Gained " + deltaPP.ToString("F3") + " pp. " + "Gained 1 Rank";
                            }

                            else
                            {
                                if (chatDelta)
                                    TwitchConnection.Instance.SendChatMessage("Gained " + deltaPP.ToString("F3") + " pp. Gained " + Math.Abs(deltaRank) + " Ranks.");
                                ppText.text += " Change: Gained " + deltaPP.ToString("F3") + " pp. " + "Gained " + Math.Abs(deltaRank) + " Ranks";
                            }

                        }
                        else if (deltaRank == 0)
                        {
                            if (chatDelta)
                                TwitchConnection.Instance.SendChatMessage("Gained " + deltaPP.ToString("F3") + " pp. No change in Rank.");
                            ppText.text += " Change: Gained " + deltaPP.ToString("F3") + " pp. " + "No change in Rank";
                        }

                        else if (deltaRank > 0)
                        {
                            if (deltaRank == 1)
                            {
                                if (chatDelta)
                                    TwitchConnection.Instance.SendChatMessage("Gained " + deltaPP.ToString("F3") + " pp. Lost 1 Rank.");
                                ppText.text += " Change: Gained " + deltaPP.ToString("F3") + " pp. " + "Lost 1 Rank";
                            }

                            else
                            {
                                if (chatDelta)
                                    TwitchConnection.Instance.SendChatMessage("Gained " + deltaPP.ToString("F3") + " pp. Lost " + Math.Abs(deltaRank) + " Ranks.");
                                ppText.text += " Change: Gained " + deltaPP.ToString("F3") + " pp. " + "Lost " + Math.Abs(deltaRank) + " Ranks";
                            }

                        }

                        oldRank = currentRank;
                        oldpp = currentpp;
                    }
                }
                else
                {
                    oldRank = currentRank;
                    oldpp = currentpp;
                    deltaPP = 0;
                    deltaRank = 0;
                }

            }
            firstLoad = false;



        }
    }
}
