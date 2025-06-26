using UnityEngine;

namespace EverdrivenDays
{
    public static class PlayerSaveData
    {
        public static int Level;
        public static int Experience;
        public static int ExperienceToNextLevel;
        public static int MaxHealth;
        public static int CurrentHealth;
        public static int Strength;
        public static int Defense;
        public static int Agility;
        public static int Intelligence;
        public static int Gold;

        public static void SaveToPrefs()
        {
            PlayerPrefs.SetInt("Player_Level", Level);
            PlayerPrefs.SetInt("Player_Experience", Experience);
            PlayerPrefs.SetInt("Player_ExperienceToNextLevel", ExperienceToNextLevel);
            PlayerPrefs.SetInt("Player_MaxHealth", MaxHealth);
            PlayerPrefs.SetInt("Player_CurrentHealth", CurrentHealth);
            PlayerPrefs.SetInt("Player_Strength", Strength);
            PlayerPrefs.SetInt("Player_Defense", Defense);
            PlayerPrefs.SetInt("Player_Agility", Agility);
            PlayerPrefs.SetInt("Player_Intelligence", Intelligence);
            PlayerPrefs.SetInt("Player_Gold", Gold);
            PlayerPrefs.Save();
        }

        public static void LoadFromPrefs()
        {
            Level = PlayerPrefs.GetInt("Player_Level", 1);
            Experience = PlayerPrefs.GetInt("Player_Experience", 0);
            ExperienceToNextLevel = PlayerPrefs.GetInt("Player_ExperienceToNextLevel", 100);
            MaxHealth = PlayerPrefs.GetInt("Player_MaxHealth", 100);
            CurrentHealth = PlayerPrefs.GetInt("Player_CurrentHealth", MaxHealth);
            Strength = PlayerPrefs.GetInt("Player_Strength", 5);
            Defense = PlayerPrefs.GetInt("Player_Defense", 5);
            Agility = PlayerPrefs.GetInt("Player_Agility", 5);
            Intelligence = PlayerPrefs.GetInt("Player_Intelligence", 5);
            Gold = PlayerPrefs.GetInt("Player_Gold", 0);
        }
    }
}
