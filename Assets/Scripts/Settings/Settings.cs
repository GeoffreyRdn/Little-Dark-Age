using UnityEngine;

namespace Settings
{
    [System.Serializable]
    public class Settings
    {
        //DISPLAY
        public bool fullscreen;
        public int[] resolution = new int[2];
        public int verticalSync;
    
        //Graphics
        public int antiAliasing;
        public int textureQuality;
        public AnisotropicFiltering anisotropicFiltering;

        //AUDIO
        public float masterVolume;
        public float musicVolume;
        public float sfxVolume;
        
        //GAMEPLAY
        public float mouseSensitivity;
        public bool InvertXAxis;

        public Settings() { }
    
        public Settings(Settings set)
        {
            fullscreen = set.fullscreen;
            resolution = set.resolution;
            verticalSync = set.verticalSync;
        
            antiAliasing = set.antiAliasing;
            textureQuality = set.textureQuality;
            anisotropicFiltering = set.anisotropicFiltering;

            masterVolume = set.masterVolume;
            musicVolume = set.musicVolume;
            sfxVolume = set.musicVolume;

            mouseSensitivity = set.mouseSensitivity;
            InvertXAxis = set.InvertXAxis;
        }
    }
}