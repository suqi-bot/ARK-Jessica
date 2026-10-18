using UnityEngine.UI;

public class SettingPanel : BasePanel
{
    public Button closeButton;
    public Toggle soundToggle;
    public Slider soundSlider;
    public Toggle musicToggle;
    public Slider musicSlider;

    public override void Init()
    {
        // 初始化面板内容
        AudioData audioData = GameDataMgr.Instance.audioData;
        soundToggle.isOn = audioData.soundOpen;
        musicToggle.isOn = audioData.musicOpen;
        soundSlider.value = audioData.soundValue;
        musicSlider.value = audioData.musicValue;


        closeButton.onClick.AddListener(() =>
        {
            // 关闭面板存储音效相关数据
            GameDataMgr.Instance.SaveMusicData();
            UIManager.Instance.HidePanel<SettingPanel>();
            

        });

        soundToggle.onValueChanged.AddListener((value) =>
        {
            BKMusic.Instance.SetStormIsOpen(value);
            GameDataMgr.Instance.audioData.soundOpen = value;
        });

        soundSlider.onValueChanged.AddListener((value) =>
        {
            BKMusic.Instance.SetStormValue(value);
            GameDataMgr.Instance.audioData.soundValue = value;
        });

        musicToggle.onValueChanged.AddListener((value) =>
        {
            BKMusic.Instance.SetIsOpen(value);
            GameDataMgr.Instance.audioData.musicOpen = value;
        });

        musicSlider.onValueChanged.AddListener((value) =>
        {
            BKMusic.Instance.ChangeValue(value);
            GameDataMgr.Instance.audioData.musicValue = value;
        });

    }

    // Start is called before the first frame update
  
}
