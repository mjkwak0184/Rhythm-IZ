using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using System.Linq;
using System.IO;
using DG.Tweening;
using TMPro;

public class SelectMusicSceneManager : MonoBehaviour
{
    AudioManager audioManager;
    WebRequests webRequests;
    [SerializeField]
    private Transform mainCanvasTransform;

    [SerializeField]
    private GameObject musicOptionViewPrefab, testObject, liveBackgroundVideoButton, deleteVideoButton, deleteMusicButton, importMusicButton, gameOptionView, startButton;
    [SerializeField]
    private SongListView izoneSongListView, customSongListView;

    [SerializeField]
    private TextMeshProUGUI selectedAlbumName, selectedSongName, worldRecordName, worldRecordScore, myRecordName, myRecordScore, labOptions;
    [SerializeField]
    private Image albumImage, songAttributeImage, liveBackgroundBtnImage, liveBackgroundBtnVideo;
    [SerializeField]
    private CardSelectView cardSelectView;
    // Game Option Texts
    [SerializeField]
    private TextMeshProUGUI flipNotesText, noteSpeedText, maxPowerLimit, ingameLoop, autoSelectDeck;
    
    void Awake()
    {
        #if UNITY_EDITOR
        if(Data.gameData == null) Data.loadGame();
        if(Data.saveData == null) Data.saveData = new SaveData();
        if(Data.userData == null) Data.userData = new UserData();
        if(Data.userData.songclearstar == null) Data.userData.songclearstar = "3333333333333333333333333333333333333";
        // if(Data.saveData == null) Data.loadSave();

        // Addressables.InstantiateAsync("DataStore").WaitForCompletion();
        #endif

        // setup song list view
        izoneSongListView.onSongSelect = delegate(int id){ musicSelect(id); };
        customSongListView.onSongSelect = delegate(int id){ musicSelect(id); };
    }

    // Start is called before the first frame update
    void Start()
    {
        audioManager = AudioManager.Instance;
        audioManager.playMusic("Audio/sound_music_select.a", true);
        Application.targetFrameRate = 60;

        #if RHYTHMIZ_TEST
        testObject.SetActive(true);
        #else
        testObject.SetActive(false);
        #endif

        updateStartButtonLabel();

        #if UNITY_IOS
        UIManager.Instance.SetRenderFrameInterval(4);
        #endif

        musicSelect(Data.saveData.lastSelectedSong);

        myRecordName.text = Data.userData.username;

        // set up song list view
        toggleLeftPanel(Data.saveData.lastSelectedSongListView, false);

        // Show loaded cards
        cardSelectView.onCardTap = delegate { gotoScene("CardEquipScene"); };

        // Set up game options
        if(Data.saveData.settings_flipNotes == 0){
            flipNotesText.text = new LocalizedText("?????? ??????", "OFF").text;
        }else if(Data.saveData.settings_flipNotes == 1){
            flipNotesText.text = new LocalizedText("?????? ??????", "Random").text;
        }else if(Data.saveData.settings_flipNotes == 2){
            flipNotesText.text = new LocalizedText("?????? ??????", "Always").text;
        }

        noteSpeedText.text = Data.saveData.settings_noteSpeed == 5 ? new LocalizedText("?????????", "Default").text : "??" + Data.noteSpeedTable[Data.saveData.settings_noteSpeed].ToString("0.0");

        if(Data.saveData.settings_maxPower == 0){
            maxPowerLimit.text = new LocalizedText("?????? ??????", "OFF").text;
        }else{
            maxPowerLimit.text = Data.saveData.settings_maxPower.ToString("N0");
        }

        ingameLoop.text = Data.saveData.settings_ingameLoop ? "ON" : "OFF";
        autoSelectDeck.text = Data.saveData.settings_autoSelectDeck ? "ON" : "OFF";

        // TEST
        if(Data.tempData.ContainsKey("additiveSync")){
            GameObject.Find("AdditiveSync").GetComponent<TextMeshProUGUI>().text = "?????? ?????????: " + float.Parse(Data.tempData["additiveSync"], System.Globalization.CultureInfo.InvariantCulture).ToString("0.000") + "s";
        }
    }

    public void setLeftPanel(int id)
    {
        Data.saveData.lastSelectedSongListView = id;
        Data.saveData.lastSelectedSongFilter = -1;  // reset album filter
        Data.saveSave();
        toggleLeftPanel(id, true);
    }

    private void toggleLeftPanel(int id, bool playTapAudio)
    {
        if(playTapAudio) AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        izoneSongListView.gameObject.SetActive(Data.saveData.lastSelectedSongListView == 0);
        customSongListView.gameObject.SetActive(Data.saveData.lastSelectedSongListView == 1);
        gameOptionView.SetActive(Data.saveData.lastSelectedSongListView == 2);
    }
    
    public void deleteVideo()
    {
        string targetPath = Application.persistentDataPath + "/videos1/" + Data.saveData.lastSelectedSong + ".mp4";
        if(File.Exists(targetPath)){
            AudioManager.Instance.playClip(SoundEffects.buttonNormal);
            Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: new LocalizedText("???????????? ??????", "Delete video background"), body: new LocalizedText("????????? ?????? ????????? ??????????????????????", "Do you want to delete the video background file?"), confirmAction: delegate{
                FileInfo fil = new FileInfo(targetPath);
                fil.Delete();
                if(Data.saveData.backgroundMode[Data.saveData.lastSelectedSong] == 2){
                    Data.saveData.backgroundMode[Data.saveData.lastSelectedSong] = 1;
                    Data.saveSave();
                }
                deleteVideoButton.SetActive(false);
                // Update button sprites
                liveBackgroundBtnImage.sprite = UIManager.Instance.localAssets.buttonPinkFilled;
                liveBackgroundBtnVideo.sprite = UIManager.Instance.localAssets.buttonPink;
            }));
        }
    }

    public void deleteMusic()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        GameObject view = Instantiate(musicOptionViewPrefab);
        view.transform.SetParent(mainCanvasTransform);
        view.transform.localScale = Vector2.one;
        view.GetComponent<MusicOptionView>().onDismiss = delegate {
            bool songLoaded = DataStore.GetSong(Data.saveData.lastSelectedSong).musicLoaded();
            importMusicButton.SetActive(!songLoaded);
            deleteMusicButton.SetActive(songLoaded);
        };        
    }

    public void setLiveBackground(int mode)
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        // if mode == 2 (video), check if video has been downloaded
        Song song = DataStore.GetSong(Data.saveData.lastSelectedSong);



        if(mode == 2){
            if(!song.videoAvailable) return;
            if(!song.videoLoaded()){    // video file doesn't exist in target directory
                Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: new LocalizedText("???????????? ????????????", "Import video background"), body: new LocalizedText("'" + song.songName + "' (??? #" + song.id + ")??? ??????????????? ???????????????.\n??? ?????????????????? ????????? ?????? ????????? ????????? ?????????.\n\n(?????? ???????????? ?????? ????????? ????????? ????????? ??? ????????????.)", "Importing video background file for '" + song.songName + "' (Song #" + song.id + ")\nPlease select the video file to import.\n\n(You can import multiple video files at once in Settings.)"), 
                    confirmAction: delegate {
                        if(NativeFilePicker.IsFilePickerBusy()){
                            Alert.showAlert(new Alert(body: new LocalizedText("?????? ???????????? ?????? ????????? ??? ????????????.\n?????? ??? ?????? ????????? ?????????.", "Failed to open file select window.\nPlease try again later.")));
                            return;
                        }
                        #if !UNITY_ANDROID && !UNITY_IOS
                        Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("???????????? ?????? ??????????????????.", "Unsupported platform.")));
                        return;
                        #endif

                        NativeFilePicker.Permission permission = NativeFilePicker.PickFile((path) => {
                            if(path == null){
                            }else{
                                WebRequests.Instance.DownloadFile("file://" + path, Application.persistentDataPath + "/videos1/" + Data.saveData.lastSelectedSong + ".mp4", 
                                    delegate(float progress){
                                        if(progress == -1){
                                            Alert.showAlert(new Alert(title: new LocalizedText("???????????? ??????", "Import error"), body: new LocalizedText("????????? ??????????????????.\n?????? ????????? ?????????.", "An error occurred.\nPlease try again.")));
                                            UIManager.Instance.toggleLoadingScreen(false);
                                        }else if(progress == 1){
                                            Alert.showAlert(new Alert(title: new LocalizedText("???????????? ??????", "Success"), body: new LocalizedText("????????? ??????????????? ??????????????????.", "Successfully imported video file.")));
                                            Data.saveData.backgroundMode[Data.saveData.lastSelectedSong] = mode;

                                            // update UI
                                            if(Data.saveData.backgroundMode[song.id] == 2){
                                                // video selected
                                                liveBackgroundBtnImage.sprite = UIManager.Instance.localAssets.buttonPink;
                                                liveBackgroundBtnVideo.sprite = UIManager.Instance.localAssets.buttonPinkFilled;
                                            }else{
                                                liveBackgroundBtnImage.sprite = UIManager.Instance.localAssets.buttonPinkFilled;
                                                liveBackgroundBtnVideo.sprite = UIManager.Instance.localAssets.buttonPink;
                                            }
                                            deleteVideoButton.SetActive(true);

                                            Data.saveSave();
                                            
                                            UIManager.Instance.toggleLoadingScreen(false);
                                        }
                                    });
                            }
                        }, new string[]{ NativeFilePicker.ConvertExtensionToFileType("mp4") });

                        if(permission == NativeFilePicker.Permission.Denied){
                            #if UNITY_ANDROID
                            Alert.showAlert(new Alert(type:Alert.Type.Confirm, title: new LocalizedText("?????? ?????? ??????", "Need storage permission"), body: new LocalizedText("????????? ?????? ????????? ?????????????????????. ?????? ????????? ????????? ????????? ????????? ?????????.", "Storage permission is needed to launch the file select window.\nPlease allow storage access in phone settings."), confirmText: new LocalizedText("?????? ??????", "Open settings"), confirmAction: delegate { UIManager.androidOpenAppSettings(); }));
                            #endif
                        }
                    }));
                return;
            }
        }
        Data.saveData.backgroundMode[Data.saveData.lastSelectedSong] = mode;

        // update UI
        if(Data.saveData.backgroundMode[song.id] == 2){
            // video selected
            liveBackgroundBtnImage.sprite = UIManager.Instance.localAssets.buttonPink;
            liveBackgroundBtnVideo.sprite = UIManager.Instance.localAssets.buttonPinkFilled;
        }else{
            liveBackgroundBtnImage.sprite = UIManager.Instance.localAssets.buttonPinkFilled;
            liveBackgroundBtnVideo.sprite = UIManager.Instance.localAssets.buttonPink;
        }

        Data.saveSave();
    }

    public void importMusic()
    {
        Song song = DataStore.GetSong(Data.saveData.lastSelectedSong);
        if(song == null) return;
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(song.musicLoaded()){
            Alert.showAlert(new Alert(title: LocalizedText.Notice, body: new LocalizedText("????????? ?????? ????????? ?????? ???????????????.", "You already imported music file for this song.")));
            return;
        }
        Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: new LocalizedText("?????? ????????????", "Import music"), body: new LocalizedText("'" + song.songName + "' ??? ????????? ???????????????.\nmp3 ?????? ????????? ????????? ?????????.\n\n(??? ????????? ?????? mp3 ????????? ???????????? ?????????????????????. ?????? ???????????? ?????? ????????? ??? ????????? ?????? ?????? ??? ?????????, ?????? ????????? ????????? ?????? ??????????????? ????????? ??? ??? ????????? ?????? ???????????????.)", "Importing music file for '" + song.songName + "'.\nPlease select an mp3 file for the song.\n\n(Notes are designed using mp3 files purchased from Melon. If you obtained your mp3 file from a different vendor, notes may not be fully in sync with the music. You can adjust sync for this song by tapping this button again after importing the music.)"), 
            confirmAction: delegate {
                if(NativeFilePicker.IsFilePickerBusy()){
                    Alert.showAlert(new Alert(body: new LocalizedText("?????? ???????????? ?????? ????????? ??? ????????????.\n?????? ??? ?????? ????????? ?????????.", "Failed to open file select window.\nPlease try again later.")));
                    return;
                }
                #if !UNITY_ANDROID && !UNITY_IOS
                Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("???????????? ?????? ??????????????????.", "Unsupported platform.")));
                return;
                #endif

                NativeFilePicker.Permission permission = NativeFilePicker.PickFile((path) => {
                    if(path == null){
                    }else{
                        WebRequests.Instance.DownloadFile("file://" + path, Application.persistentDataPath + "/music/" + Data.saveData.lastSelectedSong + ".mp3", 
                            delegate(float progress){
                                UIManager.Instance.toggleLoadingScreen(false);
                                if(progress == -1){
                                    Alert.showAlert(new Alert(title: new LocalizedText("???????????? ??????", "Import error"), body: new LocalizedText("????????? ??????????????????.\n?????? ????????? ?????????.", "An error occurred.\nPlease try again.")));
                                    
                                }else if(progress == 1){
                                    Alert.showAlert(new Alert(title: new LocalizedText("???????????? ??????", "Success"), body: new LocalizedText("????????? ??????????????? ??????????????????.\n\n????????? ????????? ????????? ?????? ?????? ?????? ?????? ?????? ????????? ?????? ????????? ?????? ????????? ???????????????.", "Successfully imported music file.\n\nIf the music is out of sync, you can adjust specific sync values for this song by pressing on the button left to the start button.")));

                                    // update UI
                                    importMusicButton.SetActive(false);
                                    deleteMusicButton.SetActive(true);
                                }
                            });
                    }
                }, new string[]{ NativeFilePicker.ConvertExtensionToFileType("mp3") });

                if(permission == NativeFilePicker.Permission.Denied){
                    #if UNITY_ANDROID
                    Alert.showAlert(new Alert(type:Alert.Type.Confirm, title: new LocalizedText("?????? ?????? ??????", "Need storage permission"), body: new LocalizedText("????????? ?????? ????????? ?????????????????????. ?????? ????????? ????????? ????????? ????????? ?????????.", "Storage permission is needed to launch the file select window.\nPlease allow storage access in phone settings."), confirmText: new LocalizedText("?????? ??????", "Open settings"), confirmAction: delegate { UIManager.androidOpenAppSettings(); }));
                    #endif
                }
            }));
        return;
    }

    public void musicSelect(int songId)
    {
        Song song = DataStore.GetSong(songId);
        if(song == null){
            startButton.GetComponent<Button>().interactable = false;
            Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("??? ????????? ????????????.", "Song information not found")));
            return;
        }

        // If deck auto select is on, select deck
        if(Data.saveData.settings_autoSelectDeck){
            cardSelectView.setSelectedDeck(song.attribute);
        }

        Album album = song.album;

        if(Data.saveData.lastSelectedSong != songId){
            // Deselect previous song
            GameObject btn = GameObject.Find("Song" + Data.saveData.lastSelectedSong);
            GameObject.Find("Song" + songId).GetComponent<SongButton>().setSelected(true);
            if(btn != null) btn.GetComponent<SongButton>().setSelected(false);
            // save selection
            Data.saveData.lastSelectedSong = songId;
        }
        // update UI
        selectedSongName.text = song.songName;
        selectedAlbumName.text = album != null ? album.albumName : "";
        if(Data.gameData.worldRecords.ContainsKey(Data.saveData.lastSelectedSong)){
            worldRecordName.text = Data.gameData.worldRecords[Data.saveData.lastSelectedSong].Item1;
            worldRecordScore.text = Data.gameData.worldRecords[Data.saveData.lastSelectedSong].Item2.ToString("N0");
        }else{
            worldRecordName.text = "";
            worldRecordScore.text = new LocalizedText("????????? ????????????.", "No record").text;
        }
        if(Data.readBitfieldData(Data.userData.scores, 8, Data.saveData.lastSelectedSong) != 0){
            myRecordScore.text = Data.readBitfieldData(Data.userData.scores, 8, Data.saveData.lastSelectedSong).ToString("N0");
        }else myRecordScore.text = new LocalizedText("????????? ????????????.", "No record").text;

        // album cover and song attribute images
        albumImage.sprite = album.albumCover;
        songAttributeImage.sprite = UIManager.Instance.localAssets.songAttributeLarge[song.attribute];
        // if(song.attribute == 0) songAttributeImage.sprite = Addressables.LoadAssetAsync<Sprite>(AddressableString.SongAttribute0).WaitForCompletion();
        // else if(song.attribute == 1) songAttributeImage.sprite = Addressables.LoadAssetAsync<Sprite>(AddressableString.SongAttribute1).WaitForCompletion();
        // else if(song.attribute == 2) songAttributeImage.sprite = Addressables.LoadAssetAsync<Sprite>(AddressableString.SongAttribute2).WaitForCompletion();

        // live video background option

        bool videoFileExists = File.Exists(Application.persistentDataPath + "/videos1/" + song.id + ".mp4");
        liveBackgroundVideoButton.SetActive(song.videoAvailable);
        deleteVideoButton.SetActive(song.videoAvailable && videoFileExists);

        if(!videoFileExists){
            // if file does not exist always set to image mode
            Data.saveData.backgroundMode[song.id] = 1;
        }
        
        startButton.GetComponent<Button>().interactable = true;

        // check if background mode is set; if not, set image as default
        if(!Data.saveData.backgroundMode.ContainsKey(song.id)){
            Data.saveData.backgroundMode[song.id] = 1;
        }
        if(Data.saveData.backgroundMode[song.id] == 2){
            // video selected
            liveBackgroundBtnImage.sprite = UIManager.Instance.localAssets.buttonPink;
            liveBackgroundBtnVideo.sprite = UIManager.Instance.localAssets.buttonPinkFilled;
        }else{
            liveBackgroundBtnImage.sprite = UIManager.Instance.localAssets.buttonPinkFilled;
            liveBackgroundBtnVideo.sprite = UIManager.Instance.localAssets.buttonPink;
        }

        cardSelectView.updateAttributeBoost();

        // Show music import button if music file is not built in
        if(song.isCustom){
            // custom music
            bool musicLoaded = song.musicLoaded();
            importMusicButton.SetActive(!musicLoaded);
            deleteMusicButton.SetActive(musicLoaded);
        }else{
            importMusicButton.SetActive(false);
            deleteMusicButton.SetActive(false);
        }

        Data.saveSave();
    }

    public void backButtonTapped()
    {
        UIManager.Instance.toggleLoadingScreen(true);
        AudioManager.Instance.playClip(SoundEffects.buttonCancel);
        UIManager.Instance.loadSceneAsync("LobbyScene");
    }

    private void updateStartButtonLabel()
    {
        // lab options
        List<string> labels = new List<string>();

        // if(Data.saveData.settings_noteSpeed != 5) labels.Add(new LocalizedText("?????? ??", "Speed ??").text + Data.noteSpeedTable[Data.saveData.settings_noteSpeed].ToString("0.0"));
        // if(Data.saveData.settings_flipNotes != 0) labels.Add(new LocalizedText("?????? ??????", "Mirror Notes").text);

        if(Data.saveData.settings_ingameLoop) labels.Add(new LocalizedText("?????? ?????????", "Auto-restart").text);
        if(Data.saveData.settings_highRefreshRate) labels.Add(new LocalizedText("60Hz ????????????", "60Hz Unlock").text);
        if(Data.saveData.settings_hapticFeedbackMaster) labels.Add(new LocalizedText("?????? ?????????", "Haptic Feedback").text);

        if(labels.Count > 0) labOptions.text = string.Join(", ", labels) + " ON";
        else labOptions.text = "";
    }

    public void openRankingView()
    {
        Song song = DataStore.GetSong(Data.saveData.lastSelectedSong);
        if(song == null) return;

        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        RankingView rankingView = UIManager.Instance.InstantiateObj(UIManager.View.Ranking).GetComponent<RankingView>();
        rankingView.Init(song);
    }

    public void startButtonTapped()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        // check if music is loaded
        if(!DataStore.GetSong(Data.saveData.lastSelectedSong).musicLoaded()){
            Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: LocalizedText.Notice, body: new LocalizedText("?????? ?????? ?????? ?????? mp3 ????????? ????????????.\n?????? ?????? ????????? ????????????????\n\n(?????? mp3 ????????? ?????? ?????? ?????? ?????? ?????? ????????? ?????? ????????? ??? ????????????.)", "Music mp3 file for this song has not been imported.\nDo you want to proceed without music?\n\n(Press the button on the left of start button to import mp3 file.)"), confirmText: new LocalizedText("???", "Yes"), cancelText: new LocalizedText("?????????", "No"), confirmAction: delegate{
                UIManager.Instance.toggleLoadingScreen(true);
                audioManager.stopMusic();
                UIManager.Instance.loadSceneAsync("InGameScene");
            }));
            return;
        }
        UIManager.Instance.toggleLoadingScreen(true);
        audioManager.stopMusic();
        UIManager.Instance.loadSceneAsync("InGameScene");
    }

    public void gotoScene(string sceneName){
        UIManager.Instance.toggleLoadingScreen(true);
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        UIManager.Instance.loadSceneAsync(sceneName);
    }


    

    public void gameoptions_flipNotes()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(++Data.saveData.settings_flipNotes > 2){
            Data.saveData.settings_flipNotes = 0;
        }
        Data.saveSave();
        if(Data.saveData.settings_flipNotes == 0){
            flipNotesText.text = new LocalizedText("?????? ??????", "OFF").text;
        }else if(Data.saveData.settings_flipNotes == 1){
            flipNotesText.text = new LocalizedText("?????? ??????", "Random").text;
        }else if(Data.saveData.settings_flipNotes == 2){
            flipNotesText.text = new LocalizedText("?????? ??????", "Always").text;
        }
    }    

    public void gameoptions_noteSpeedUp()
    {
        if(++Data.saveData.settings_noteSpeed > 15){
            Data.saveData.settings_noteSpeed = 15;
        }else{
            AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        }
        Data.saveSave();
        noteSpeedText.text = Data.saveData.settings_noteSpeed == 5 ? new LocalizedText("?????????", "Default").text : "??" + Data.noteSpeedTable[Data.saveData.settings_noteSpeed].ToString("0.0");
    }

    public void gameoptions_noteSpeedDown()
    {
        if(--Data.saveData.settings_noteSpeed < 0){
            Data.saveData.settings_noteSpeed = 0;
        }else{
            AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        }
        Data.saveSave();
        noteSpeedText.text = Data.saveData.settings_noteSpeed == 5 ? new LocalizedText("?????????", "Default").text : "??" + Data.noteSpeedTable[Data.saveData.settings_noteSpeed].ToString("0.0");
    }

    public void gameoptions_maxPower()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(Data.saveData.settings_maxPower != 0){
            Data.saveData.settings_maxPower = 0;
            Data.saveSave();
            maxPowerLimit.text = new LocalizedText("?????? ??????", "OFF").text;
            cardSelectView.updateCards();
        }else{
            Alert.showAlert(new Alert(type: Alert.Type.Input, title: new LocalizedText("?????? ?????? ?????? ??????", "Limit maximum power"), body: new LocalizedText("????????? ?????? ???????????? ??????????????????.\n(1,000 ~ 100,000 ?????? ??? ??????)", "Enter the power limit value to set.\n(Enter value between 1,000 and 100,000)"), confirmAction: delegate(string val){
                int parsed;
                if(int.TryParse(val.Replace(",", ""), out parsed)){
                    if(parsed < 1000 || parsed > 100000){
                        Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("?????? ?????? ?????? ?????? 1,000, ?????? 100,000 ?????? ????????? ??? ????????????.\n?????? ????????? ?????????.", "You must enter a number between 1,000 and 100,000.\nPlease try again.")));
                    }else{
                        Data.saveData.settings_maxPower = parsed;
                        maxPowerLimit.text = parsed.ToString("N0");
                        cardSelectView.updateCards();
                    }
                }else{
                    Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("????????? ?????? ?????????????????????.\n?????? ????????? ?????????.", "You must enter a number between 1,000 and 100,000.\nPlease try again.")));
                }
            }));
        }
    }

    public void gameoptions_ingameLoop()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        Data.saveData.settings_ingameLoop = !Data.saveData.settings_ingameLoop;
        Data.saveSave();
        ingameLoop.text = Data.saveData.settings_ingameLoop ? "ON" : "OFF";
        updateStartButtonLabel();
    }

    public void gameoptions_autoSelectDeck()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        Data.saveData.settings_autoSelectDeck = !Data.saveData.settings_autoSelectDeck;
        cardSelectView.toggleAutoSelectLabel(Data.saveData.settings_autoSelectDeck);
        Data.saveSave();
        autoSelectDeck.text = Data.saveData.settings_autoSelectDeck ? "ON" : "OFF";
    }



    // ====================== TEST OPTION ===========================
    #if RHYTHMIZ_TEST
    // Private Test - Apply Custom Time
    public void setAdditiveSync()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        Alert.showAlert(new Alert(type: Alert.Type.Input, title: "?????? ?????? ??????", body: "????????? ???????????? ?????? ?????? ???????????? ??????????????????.\n?????? 0?????? ??? ?????? ????????? ???????????? ?????? ????????????, ?????? 0?????? ???????????? ????????? ???????????? ?????? ???????????????.\n(-0.5??? ~ 0.5??? ?????? ??????)", 
            confirmAction: delegate(string response){
                try{
                    float sync = float.Parse(response, System.Globalization.CultureInfo.InvariantCulture);
                    if(sync > 0.5 || sync < -0.5){
                        Alert.showAlert(new Alert(title: "??????", body: "?????? ?????? ????????? ????????????. ?????? ????????? ?????????."));
                        return;
                    }
                    Data.tempData["additiveSync"] = sync.ToString();
                    // Update UI
                    GameObject.Find("AdditiveSync").GetComponent<TextMeshProUGUI>().text = "?????? ?????????: " + sync.ToString("0.000") + "s";
                }catch{
                    Alert.showAlert(new Alert(title: "??????", body: "????????? ?????? ?????????????????????. ?????? ????????? ?????????."));
                }
            }));
    }
    #endif
    // ==============================================================
}
