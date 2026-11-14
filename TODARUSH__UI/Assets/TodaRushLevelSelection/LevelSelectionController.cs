using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
[System.Serializable]
public class TodaRushLevelDefinition
{
 public string displayName;
 [Tooltip("Assign a real gameplay scene path or name registered in Build Profiles.")] public string gameplayScene;
 public int passengers;
 public int seconds;
 public string difficulty;
 public TMP_Text passengerLabel,timerLabel;
 public void Refresh(){if(passengerLabel)passengerLabel.text=passengers+" PAX";if(timerLabel)timerLabel.text=(seconds/60).ToString("00")+":"+(seconds%60).ToString("00");}
}
public sealed class LevelSelectionController:MonoBehaviour
{
 [SerializeField] string mainMenuScene="Assets/Scenes/MainMenu.unity";
 [SerializeField] TodaRushLevelDefinition[] levels;
 [SerializeField] TMP_Text notification;

 public TodaRushLevelDefinition[] Levels=>levels;
 void Start(){foreach(var level in levels)level.Refresh();if(notification)notification.text="";}
 public void SelectLevel1(){Select(0);}
 public void SelectLevel2(){Select(1);}
 public void SelectLevel3(){Select(2);}
 public void Select(int index){
 if(levels==null||index<0||index>=levels.Length)return;
 var level=levels[index];
 if(string.IsNullOrWhiteSpace(level.gameplayScene)){Notify(level.displayName+" — LEVEL NOT YET AVAILABLE");return;}
 if(!Application.CanStreamedLevelBeLoaded(level.gameplayScene)){Notify("Configured level scene is unavailable. Add it to Build Profiles.");return;}
 if(GameSettingsManager.Instance)GameSettingsManager.Instance.Save();

 SceneManager.LoadScene(level.gameplayScene);
 }
 void Notify(string message){if(notification)notification.text=message;}
 public void ReturnToMainMenu(){
 if(!Application.CanStreamedLevelBeLoaded(mainMenuScene)){Notify("Main Menu scene is unavailable. Check Build Profiles.");return;}
 if(GameSettingsManager.Instance)GameSettingsManager.Instance.Save();
 SceneManager.LoadScene(mainMenuScene);
 }
}


