using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

using TMPro;
using Photon.Pun;
using NaughtyAttributes;

namespace Settings
{
    [Serializable] public class StringGameObjectDictionary : SerializableDictionary<string, GameObject> { }
    [Serializable] public class StringFloatDictionary : SerializableDictionary<string,float> { }
    [Serializable] public class StringDropdownDictionary : SerializableDictionary<string,TMP_Dropdown> { }

    [Serializable] public class StringAudioMixerGroupDictionary : SerializableDictionary<string, AudioMixerGroup> { }

    [Serializable] public class StringSliderDictionary : SerializableDictionary<string, Slider> { }
    
    public class SettingsManager : MonoBehaviourPunCallbacks
    {
        #region Variables
        
        [SerializeField] string lobbyScene;
        
        
        [BoxGroup("Settings")] [SerializeField] string settingsSavePath;
        [BoxGroup("Settings")] [SerializeField] Settings defaultSettings = new Settings();
        [BoxGroup("Settings")] [SerializeField] Settings currentSettings = new Settings(){anisotropicFiltering = AnisotropicFiltering.ForceEnable};
        [BoxGroup("Settings")] [SerializeField] GameObject pauseScene;

        [BoxGroup("Dictionaries")] [SerializeField] StringGameObjectDictionary mainDic;
        [BoxGroup("Dictionaries")] [SerializeField] StringGameObjectDictionary secondaryDico;
        [BoxGroup("Dictionaries")] [SerializeField] StringDropdownDictionary dicoDropDown;

        [BoxGroup("Dictionaries")] [SerializeField] StringGameObjectDictionary onOffDic;
        [BoxGroup("Dictionaries")] [SerializeField] StringAudioMixerGroupDictionary audioMixerDic;
        [BoxGroup("Dictionaries")] [SerializeField] StringSliderDictionary sliderDic;
        
        Resolution[] resolutions;
    
        #endregion
    
        #region Initialization

        void Awake()
        {
        }

        void Start() 
            => LoadSettings();

        void LoadSettings()
         {
             

             Debug.Log(currentSettings.anisotropicFiltering == AnisotropicFiltering.ForceEnable
                 ? "loaded default settings"
                 : "loaded saved setting");

             //Anisotropic Filtering is never on ForceEnable except if no saved settings have been found
             //We can then load default settings if that's the case
             currentSettings = currentSettings.anisotropicFiltering == AnisotropicFiltering.ForceEnable 
                 ? new Settings(defaultSettings)
                 : currentSettings;
             
             #region Display
         
             //Fullscreen
             Screen.fullScreen = currentSettings.fullscreen;
             OnOffSwitchTexture(onOffDic["Fullscreen"], Screen.fullScreen);

             //Vertical Sync
             QualitySettings.vSyncCount = currentSettings.verticalSync;
             OnOffSwitchTexture(onOffDic["Vertical Sync"], QualitySettings.vSyncCount != 0);
         
             //Resolution
             resolutions = Screen.resolutions;

             List<string> dropDownOption = new List<string>();
             string previousRes = "";

             //Get the index of the current resolution
             
             foreach (var res in resolutions)
             {
                 string currentRes = res.width + "x" + res.height;
                 if (previousRes != "" && currentRes != previousRes)
                     dropDownOption.Add(previousRes);
                 
                 previousRes = currentRes;
             }
             dropDownOption.Add(previousRes);
            
             TMP_Dropdown resolution = dicoDropDown["Resolution"];
             resolution.ClearOptions();
             resolution.AddOptions(dropDownOption);

             if (currentSettings.resolution[0] == 0 || currentSettings.resolution[1] == 0) //if defaultSettings are used
             {
                 resolution.value = dropDownOption.Count;
                 var res = dicoDropDown["Resolution"].captionText.text.Split('x');
                 var (width, height) = (int.Parse(res[0]), int.Parse(res[1]));
                 Screen.SetResolution(width, height, Screen.fullScreen);

                 currentSettings.resolution[0] = width;
                 currentSettings.resolution[1] = height;
             }
             else //if defaultSettings are not used
             {
                 for (int i = 0; i < resolution.options.Count; i++)
                 {
                     if (resolution.options[i].text ==
                         currentSettings.resolution[0] + "x" + currentSettings.resolution[1])
                     {
                         resolution.value = i;
                         break;
                     }
                 }
                 Screen.SetResolution(currentSettings.resolution[0], currentSettings.resolution[1], Screen.fullScreen);
             }
             
             #endregion

             #region Graphics

             QualitySettings.antiAliasing = currentSettings.antiAliasing;
             dicoDropDown["Anti Aliasing"].value = currentSettings.antiAliasing switch
             {
                 0 => 0,
                 2 => 1,
                 4 => 2,
                 8 => 3
             };

             QualitySettings.SetQualityLevel(currentSettings.textureQuality,true);
             dicoDropDown["Texture Quality"].value = currentSettings.textureQuality;

             QualitySettings.anisotropicFiltering = currentSettings.anisotropicFiltering;
             OnOffSwitchTexture(onOffDic["Anisotropic Filtering"],
                 QualitySettings.anisotropicFiltering == AnisotropicFiltering.Enable);

             #endregion

             #region Audio
         
             sliderDic["SFX Volume"].value = currentSettings.sfxVolume;
             if (currentSettings.sfxVolume == 0 || currentSettings.masterVolume == 0)
                 audioMixerDic["SFX Volume"].audioMixer.SetFloat("SFX Volume", -80);
             else
                audioMixerDic["SFX Volume"].audioMixer.SetFloat("SFX Volume", 
                 Mathf.Log10(currentSettings.sfxVolume * currentSettings.masterVolume) * 20);

             sliderDic["Music Volume"].value = currentSettings.musicVolume;
             if (currentSettings.musicVolume == 0 || currentSettings.masterVolume == 0)
                audioMixerDic["Music Volume"].audioMixer.SetFloat("Music Volume", -80);
             else
                 audioMixerDic["Music Volume"].audioMixer.SetFloat("Music Volume", 
                 Mathf.Log10(currentSettings.musicVolume * currentSettings.masterVolume) * 20);
         
             //Master Volume must be put after SFX and Music volumes, or else they won't load properly
             sliderDic["Master Volume"].value = currentSettings.masterVolume;
             if (currentSettings.masterVolume == 0)
                 audioMixerDic["Master Volume"].audioMixer.SetFloat("Master Volume", -80);
             else
                 audioMixerDic["Master Volume"].audioMixer.SetFloat("Master Volume", Mathf.Log10(currentSettings.masterVolume) * 20);
             
             #endregion

             
            
         }

        #endregion

        #region User Interactions

        public void OnSliderValueChanged(string sld)
        {
            void SFX()
            {
                currentSettings.sfxVolume = sliderDic["SFX Volume"].value;
                if (currentSettings.sfxVolume == 0 || sliderDic["Master Volume"].value == 0)
                    audioMixerDic["SFX Volume"].audioMixer.SetFloat("SFX Volume", -80);
                else
                    audioMixerDic["SFX Volume"].audioMixer.SetFloat("SFX Volume", Mathf.Log10(currentSettings.sfxVolume * currentSettings.masterVolume) * 20);
            }

            void Music()
            {
                currentSettings.musicVolume = sliderDic["Music Volume"].value;
                if (currentSettings.musicVolume == 0 || sliderDic["Master Volume"].value == 0)
                    audioMixerDic["Music Volume"].audioMixer.SetFloat("Music Volume", -80);
                else
                    audioMixerDic["Music Volume"].audioMixer.SetFloat("Music Volume", Mathf.Log10(currentSettings.musicVolume * currentSettings.masterVolume) * 20);
            }
         
            switch (sld)
            {
                case "Master Volume":
                    currentSettings.masterVolume = sliderDic[sld].value;
                    if (sliderDic[sld].value == 0)
                        audioMixerDic[sld].audioMixer.SetFloat(sld, -80);
                    else
                        audioMixerDic[sld].audioMixer.SetFloat(sld, Mathf.Log10(currentSettings.masterVolume) * 20);
                    SFX();
                    Music();
                    break;
                
                case "SFX Volume":
                    SFX();
                    break;
                
                case "Music Volume":
                    Music();
                    break;
                
              
            }
         
           
        }

        #endregion
     
        #region Methods
        
        public void OnOffSwitchTexture(GameObject go, bool isOn)
        {
            GameObject tick = go.GetComponentInChildren<Transform>().Find("Button").gameObject.GetComponentInChildren<Transform>().Find("Tick").gameObject;
            var image = tick.GetComponent<Image>();
            var tempColor = image.color;
            tempColor.a = isOn ? 1 : 0;
            image.color = tempColor;
        }
    
        public void ChangeView(string view)
        {
            if (mainDic.ContainsKey(view))
            {
                foreach (var item in secondaryDico)
                    item.Value.SetActive(false);
                foreach (var item in mainDic.Where(item => item.Value != null))
                    item.Value.SetActive(item.Key == view);
            }
            else if (secondaryDico.ContainsKey(view))
            {
                foreach (var item in secondaryDico)
                    item.Value.SetActive(item.Key == view);
            }
            else
                Debug.LogWarning($"View '{view}' doesn\'t exists in ViewDic");
        }

        #endregion

        #region Events

        public override void OnLeftRoom()
        {
            // PhotonNetwork.Disconnect();
            SceneManager.LoadScene(lobbyScene);
            base.OnLeftRoom();
        }

        #endregion
    }
}