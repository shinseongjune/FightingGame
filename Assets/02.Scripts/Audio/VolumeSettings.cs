using UnityEngine;

public static class VolumeSettings
{
    const string KEY_BGM = "vol.bgm.db";
    const string KEY_SFX = "vol.sfx.db";

    public static float BGMdB { get; private set; } = -6f;
    public static float SFXdB { get; private set; } = -6f;

    public static void Load()
    {
        BGMdB = PlayerPrefs.GetFloat(KEY_BGM, -6f);
        SFXdB = PlayerPrefs.GetFloat(KEY_SFX, -6f);
        Apply();
    }

    public static void Save(float bgmDb, float sfxDb)
    {
        BGMdB = bgmDb; SFXdB = sfxDb;
        PlayerPrefs.SetFloat(KEY_BGM, BGMdB);
        PlayerPrefs.SetFloat(KEY_SFX, SFXdB);
        PlayerPrefs.Save();
        Apply();
    }

    static void Apply()
    {
        if (SoundService.Instance == null) return;
        SoundService.Instance.SetBGMdB(BGMdB);
        SoundService.Instance.SetSFXdB(SFXdB);
    }
}
