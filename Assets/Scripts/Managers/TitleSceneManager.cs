using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using UnityEngine.Rendering;
using TMPro;

using Newtonsoft.Json;

// using Unity.Services.Ccd.Management;
using UnityEngine.AddressableAssets;

using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

public class TitleSceneManager : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI version, message;
    [SerializeField]

    private GameObject touchToStart, loggedIn, notLoggedIn, getSupportPrefab;
    [SerializeField]
    private ShaderVariantCollection shadersToWarm;
    [SerializeField]
    private UnityEngine.Video.VideoPlayer video;

    AudioSource music;
    bool musicLoopStarted = false;
    AudioClip titleMusic;
    AudioClip titleMusicLoop;
    
    private bool startReady = false;

    
    // // Firebase Message Handler
    // public void OnTokenReceived(object sender, Firebase.Messaging.TokenReceivedEventArgs token) {
    //     UnityEngine.Debug.Log("Received Registration Token: " + token.Token);
    // }

    // public void OnMessageReceived(object sender, Firebase.Messaging.MessageReceivedEventArgs e) {
    //     UnityEngine.Debug.Log("Received a new message from: " + e.Message.From);
    // }

    void Awake()
    {
        #if ENABLE_LEGACY_INPUT_MANAGER
        Input.multiTouchEnabled = false;
        #endif

        // // Initialize Firebase
        // Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
        //     var dependencyStatus = task.Result;
        //     if (dependencyStatus == Firebase.DependencyStatus.Available) {
        //         // Create and hold a reference to your FirebaseApp,
        //         // where app is a Firebase.FirebaseApp property of your application class.
        //         Data.firebase = Firebase.FirebaseApp.DefaultInstance;
        //         // Set a flag here to indicate whether Firebase is ready to use by your app.
        //         Debug.Log("[TitleSceneManager] Firebase Intialized");
        //         Firebase.Messaging.FirebaseMessaging.TokenReceived += OnTokenReceived;
        //         Firebase.Messaging.FirebaseMessaging.MessageReceived += OnMessageReceived;
        //     } else {
        //         UnityEngine.Debug.LogError(System.String.Format(
        //         "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
        //         // Firebase Unity SDK is not safe to use here.
        //     }
        // });

        // Fetch CCD environment status
        if(Data.ccdURL == null){
            string getLocation(IResourceLocation location){
                if(location.InternalId.StartsWith("http")){
                    #if UNITY_EDITOR
                    Debug.Log("Addressable: " + location.InternalId);
                    #endif
                    if(location.InternalId.Contains("ee138faf-9fea-4f1f-8a06-fd84d9bf17d7")) Data.ccdEnvironment = "Development";
                    else if(location.InternalId.Contains("b0688700-90da-4085-80ee-23908acdd016")) Data.ccdEnvironment = "Staging";
                    else if(location.InternalId.Contains("887fc65f-d755-43ae-9ee7-fc758f482e4a")) Data.ccdEnvironment = "Release";
                    // fetch badge
                    string[] url = location.InternalId.Split("/");
                    for(int i = 0; i < url.Length; i++){
                        if(url[i].Contains("release_by_badge") && i + 1 < url.Length){
                            Data.ccdBadge = url[i + 1];
                            break;
                        }
                    }
                    #if !UNITY_EDITOR
                    Addressables.InternalIdTransformFunc = null;
                    #endif
                }
                return location.InternalId;
            }
            Addressables.InternalIdTransformFunc = getLocation;
        }

        // UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
        // InputSystem.pollingFrequency = 1;
        music = GetComponent<AudioSource>();
        titleMusic = Resources.Load("Audio/bgm_sound_title_movie_intro.a") as AudioClip;
        Application.targetFrameRate = 60;
        touchToStart.SetActive(false);
        
        #if RHYTHMIZ_TEST
        version.text = "Ver: " + Application.version + " (????????? ??????)";
        #else
        version.text = "Ver: " + Application.version;
        #endif
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if(pauseStatus) video.Pause();
        else video.Play();
    }

    // Start is called before the first frame update
    void Start()
    {

        UIManager.Instance.SetRenderFrameInterval(2);
        // Initialize Game
        music.clip = titleMusic;
        music.Play();
        // If redirected to title manager, there might already be audio playing, so stop the audiomanager music
        if(AudioManager.Instance != null) AudioManager.Instance.stopMusic();
        titleMusicLoop = Resources.Load("Audio/bgm_sound_title_movie_loop.a") as AudioClip;
        Screen.sleepTimeout = 90;  // 1.5 minutes of inactivity to turn off screen
        message.text = "";

        // Warm up shaders
        shadersToWarm.WarmUp();

        // Load save data
        try
        {
            if(!Data.loadSave()){
                // False returned, save doesn't exist
                Alert.showAlert(new Alert(title: LocalizedText.Notice, body: new LocalizedText("Rhythm*IZ??? SUPERSTAR IZ*ONE??? ???????????? ???????????? ???????????? ???????????????.\n\n????????? ?????? ???????????? ????????? ????????????, ?????? ????????? ?????? ????????? ????????? ????????? ??? ????????????.", "Rhythm*IZ is a fanmade game based on SUPERSTAR IZ*ONE.\n\nAll game services are provided free of charge, and provision of services may stop in the future.")));
                Data.newSave();
            }
        }
        catch (System.Exception)
        {
            Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("?????? ????????? ????????? ??????????????? ?????????????????????.\n?????? ????????? ?????? ????????? ????????? ?????? ???????????????.", "Failed to load game save file.\nTo prevent errors, a new save file has been created.")));
            Data.newSave();
        }
        

        // Save loaded, apply settings
        // Set background audio volume
        music.volume = Data.saveData.settings_backgroundVolume;
        // Set music volume for AudioManager
        AudioManager.Instance.backgroundAudio.volume = Data.saveData.settings_backgroundVolume;
        AudioManager.Instance.setEffectVolume(Data.saveData.settings_effectVolume);

        // Set language
        if(Data.saveData.settings_language == 1){ // Korean
            LocalizationManager.Instance.setLocale("ko");
        }else if(Data.saveData.settings_language == 2){ // English
            LocalizationManager.Instance.setLocale("en");
        }

        // Load remote assets


        #if UNITY_EDITOR
            #if RHYTHMIZ_TEST
            Data.accountData = new AccountData("mjkwak0184@gmail.com", "1234123456785678");
            #endif
        #endif

        // Load account
        if(Data.loadAccount()){
            // Account exists and is loaded
            notLoggedIn.SetActive(false);

            // loadUser();
            loadUser();
        }else{
            // display account settings, wait until save created
            loggedIn.SetActive(false);

            // Check for version updates
            WebRequests.Instance.GetJSONRequest(Data.serverURL + "/version", delegate(Dictionary<string, object> response){
                if(response.ContainsKey("latest")){
                    if(Application.version.CompareTo((string) response["latest"]) < 0){
                        // new version available
                        if(Application.version.CompareTo((string) response["minimum"]) < 0){
                            Alert.showAlert(new Alert(title: new LocalizedText("???????????? ??????", "Update available"), body: new LocalizedText("????????? ????????? ????????????. ????????? ?????????????????? ?????? ?????? ???????????? ???????????? ??? ?????????.", "You must update the game in order to play. Please update the game."), confirmAction: delegate{
                                if(response.ContainsKey("updateURL")) Application.OpenURL((string) response["updateURL"]);
                            }));
                        }else{
                            Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: new LocalizedText("???????????? ??????", "Update available"), body: new LocalizedText("????????? ????????? ????????????. ?????? ?????? ???????????? ???????????? ??? ?????????.", "An update to the game is available. Please update the game."), confirmAction: delegate{
                                if(response.ContainsKey("updateURL")) Application.OpenURL((string) response["updateURL"]);
                            }));
                        }
                    }
                }
            }, delegate(string error){
                // error
                Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("?????? ????????? ?????????????????????. ????????? ????????? ??????????????????.", "Failed to check for updates. Please check your internet status.")));
            });
        }

    }

    public void forgotPassword()
    {
        Application.OpenURL("https://wiz-one.space/rhythmiz_forgot");
    }

    public void setLocale(string code)
    {
        AudioManager.Instance.playClip(SoundEffects.buttonSmall);
        if(code == "ko"){
            LocalizationManager.Instance.setLocale("ko");
            Data.saveData.settings_language = 1;
        }else if(code == "en"){
            LocalizationManager.Instance.setLocale("en");
            Data.saveData.settings_language = 2;
        }
        Data.saveSave();
    }

    public void createAccount_emailCheck()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(Application.internetReachability == NetworkReachability.NotReachable){
            Alert.showAlert(new Alert(title: new LocalizedText("????????? ?????? ??????", "Internet error"), body: new LocalizedText("???????????? ???????????? ?????? ????????????.", "You are not connected to the internet."), confirmAction: delegate{
                createAccount_emailCheck();
            }));
            return;
        }
        Alert.showAlert(new Alert(type: Alert.Type.Input, title: new LocalizedText("??? ?????? ??????", "Create new account"), body: new LocalizedText("????????? ????????? ???????????? ???????????????.\n????????? ?????? ?????????????????? ???????????? ?????? ???????????? ???????????? ?????? ?????? ??? ????????? ????????? ?????? ??????????????????.", "Enter your email.\nIf you have not registered your email on registration website, please register your email first."),
        confirmAction: delegate(string email){
            string stripped = email.Replace(" ", "").Replace("\n", "");
            if(!stripped.Contains("@") || stripped.Split("@")[0].Length <= 2 || stripped.Split("@")[1].Length <= 2 || !stripped.Split("@")[1].Contains(".")){
                Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("????????? ????????? ?????????????????????.\n?????? ????????? ?????????.", "The email you entered is not valid. Please try again.")));
                return;
            }
            WWWForm form = new WWWForm();
            form.AddField("email", stripped);
            UIManager.Instance.toggleLoadingScreen(true);
            WebRequests.Instance.PostJSONRequest(Data.serverURL + "/newuser_checkemail", form, delegate(Dictionary<string, object> result){
                UIManager.Instance.toggleLoadingScreen(false);
                if((bool) result["success"]){
                    createAccount(stripped);
                }else{
                    if(result.ContainsKey("url")){
                        Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: LocalizedText.Notice.text, body: (string) result["message"], confirmAction: delegate{
                            Application.OpenURL((string) result["url"]);
                        }));
                    }else{
                        Alert.showAlert(new Alert(title: LocalizedText.Notice.text, body: (string) result["message"]));
                    }
                    
                }
            }, delegate(string error){
                UIManager.Instance.toggleLoadingScreen(false);
                Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("?????? ????????? ?????????????????????.\n?????? ??? ?????? ????????? ?????????.", "A server error has occurred.\nPlease try again later.")));
            });

        }));
    }

    public void createAccount(string email)
    {
        if(Application.internetReachability == NetworkReachability.NotReachable){
            Alert.showAlert(new Alert(title: new LocalizedText("????????? ?????? ??????", "Internet error"), body: new LocalizedText("???????????? ???????????? ?????? ????????????.", "You are not connected to the internet."), confirmAction: delegate{
                createAccount(email);
            }));
            return;
        }
        Alert.showAlert(new Alert(type: Alert.Type.Input, title: new LocalizedText("??? ?????? ??????", "Create new account"), body: new LocalizedText("?????? ????????? ?????? ???????????? ??????????????????.", "Enter a nickname you want to use."), 
            confirmAction: delegate(string username){
            if(username.Length < 2 || username.Length > 12){
                Alert.showAlert(new Alert(title: new LocalizedText("????????? ?????? ??????", "Length limit"), body: new LocalizedText("???????????? ?????? 2???, ?????? 12????????? ?????? ???????????????.\n?????? ????????? ?????????.", "Nickname must be between 2 to 12 characters long.\nPlease try again."), confirmAction: delegate{ createAccount(email); }));
                return;
            }
            message.text = new LocalizedText("?????? ?????? ???...", "Creating new account...").text;
            WWWForm form = new WWWForm();
            form.AddField("username", username);
            form.AddField("email", email);
            notLoggedIn.SetActive(false);
            WebRequests.Instance.PostJSONRequest(Data.serverURL + "/newuser", form, 
                delegate (Dictionary<string, object> result) {
                    if((bool) result["success"]){
                        // login succeeded
                        Data.accountData = new AccountData((string) result["userid"], (string) result["password"]);
                        Data.saveAccount();
                        Alert.showAlert(new Alert(title: new LocalizedText("?????? ?????? ??????", "Account login code"), body: new LocalizedText("?????? ????????? ?????????????????????.\n?????? ????????? ?????? ?????? ?????? ????????? ????????? ??????????????? ?????? ?????? ????????? ?????? ?????? ?????? ????????? ???????????? ???????????? ????????? ???????????? ????????? ?????????.\n\n??????ID: " + Data.accountData.userid + "\n?????? ??????: " + Data.accountData.password + "\n\n????????? ?????? ????????? ????????? ?????? ?????? ??? [??? ??????]?????? ???????????? ???????????????.", "Your account has been created successfully.\nIn cases when you need to reconnect to this account (e.g. on a new device), you can use the information below to log back in.\n\nUser ID: " + Data.accountData.userid + "\nAccount login code: " + Data.accountData.password + "\n\nIf you need a new login code, you can renew it in [Profile] screen.")));
                        
                        // Change UI
                        loggedIn.SetActive(true);
                        message.text = new LocalizedText("????????? ???...", "Logging in...").text;
                        loadUser();
                    }else{
                        notLoggedIn.SetActive(true);
                        Alert.showAlert(new Alert(title: LocalizedText.Error.text, body: (string)result["message"]));
                        message.text = "";
                    }
                },
                delegate (string error){
                    // error
                    notLoggedIn.SetActive(true);
                    Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("????????? ?????????????????????.\n" + error, "An error has occurred.\n"+ error)));
                    message.text = "";
                });
            }
        ));
    }



    private void loadRemoteAssets()
    {
        StartCoroutine(WebRequests.Instance.LoadRemoteAssets(
                delegate(string msg){
                    // on progress
                    message.text = msg;
                },
                delegate {
                    // On Success
                    // Load game data
                    Data.loadGame();

                    // Allow enter game
                    message.text = "";
                    startReady = true;
                    touchToStart.SetActive(true);
                    notLoggedIn.SetActive(false);
                    loggedIn.SetActive(true);
                }, 
                delegate (string error){
                    Alert.showAlert(new Alert(title: LocalizedText.Error.text, body: error, confirmText: LocalizedText.Retry.text, confirmAction: delegate{
                        loadRemoteAssets();
                    }));
                }
            ));
    }


    void loadUser()
    {
        if(Application.internetReachability == NetworkReachability.NotReachable){
            Alert.showAlert(new Alert(title: new LocalizedText("????????? ?????? ??????", "Internet error"), body: new LocalizedText("???????????? ???????????? ?????? ????????????.", "You are not connected to the internet."), confirmText: LocalizedText.Retry, confirmAction: delegate { loadUser(); }));
            return;
        }

        message.text = new LocalizedText("????????? ???...", "Logging in...").text;

        Data.loadDataFromServer(callback: delegate(Dictionary<string, object> result){
            if(result.ContainsKey("message")){
                if(result.ContainsKey("url")){
                    Alert.showAlert(new Alert(type: Alert.Type.Confirm, body: (string) result["message"], confirmAction: delegate{ 
                        Application.OpenURL((string) result["url"]);
                    }));
                }else{
                    Alert.showAlert(new Alert(body: (string) result["message"]));
                }
            }

            loadRemoteAssets();
        }, errorcallback: delegate(Dictionary<string, object> result){
            if(result.ContainsKey("reset")){    // password error
                Data.deleteAccount();
                Alert.showAlert(new Alert(title: LocalizedText.Error.text, body: (string) result["message"]));
                message.text = "";
                notLoggedIn.SetActive(true);
                loggedIn.SetActive(false);
            }else{
                
                if(result.ContainsKey("message")){
                    if(result.ContainsKey("url")){
                        Alert.showAlert(new Alert(type: Alert.Type.Confirm, body: (string) result["message"], confirmAction: delegate{ 
                            Application.OpenURL((string) result["url"]);
                            loadUser();
                        }, cancelAction: delegate{
                            loadUser();
                        }));
                    }else{
                        Alert.showAlert(new Alert(body: (string) result["message"], confirmText: LocalizedText.Retry.text, confirmAction: delegate{ loadUser(); }));
                    }
                }else{
                    Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("?????? ????????? ?????????????????????.\n?????? ??? ?????? ????????? ?????????.", "A server error has occurred.\nPlease try again later."), confirmText: LocalizedText.Retry, confirmAction: delegate{ loadUser(); }));
                }
            }
        }, isNewLogin: true);
    }

    public void connectAccount1()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(Application.internetReachability == NetworkReachability.NotReachable){
            Alert.showAlert(new Alert(title: new LocalizedText("????????? ?????? ??????", "Internet error"), body: new LocalizedText("???????????? ???????????? ?????? ????????????.", "You are not connected to the internet.")));
            return;
        }
        Alert.showAlert(new Alert(type: Alert.Type.Input, title: new LocalizedText("????????? ??????", "Enter email"), body: new LocalizedText("????????? ????????? ?????? ID (?????????)??? ??????????????????.", "Enter the User ID (email) for your existing account."), 
        confirmAction: delegate(string userid){
            string stripped = userid.Replace(" ", "");
            if(!stripped.Contains("@")){
                Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("?????? ID??? ?????? ?????????????????????.\n?????? ????????? ?????????.", "Invalid User ID.\nPlease try again.")));
            }else{
                connectAccount2(stripped);
            }
        }));
    }

    public void connectAccount2(string userid)
    {
        Alert.showAlert(new Alert(type: Alert.Type.Input, title: new LocalizedText("?????? ?????? ??????", "Enter login code"), body: new LocalizedText("User ID : " + userid + "\n\n?????? ????????? ??????????????????.", "User ID : " + userid + "\n\nEnter your account login code."),
            confirmAction: delegate(string password){
                string stripped = password.Replace(" ", "");
                if(stripped.Length != 16) Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("?????? ????????? ?????? ?????????????????????.\n?????? ????????? ?????? 16????????? ???????????? ????????????.", "Entered login code is incorrect.\nLogin codes are 16-digits in length."), confirmAction:delegate{ connectAccount2(userid); }));
                else {
                    notLoggedIn.SetActive(false);
                    WWWForm form = new WWWForm();
                    form.AddField("userid", userid);
                    form.AddField("password", stripped);
                    WebRequests.Instance.PostJSONRequest(Data.serverURL + "/link_account", form, 
                    delegate(Dictionary<string, object> result){
                        if((bool) result["success"]){
                            Data.accountData = new AccountData(userid, stripped);
                            Data.saveAccount();
                            Alert.showAlert(new Alert(title: new LocalizedText("?????? ??????", "Success"), body: new LocalizedText("????????? ?????????????????????.\n?????? ??????????????? ?????? ????????? ??????????????? [??? ??????] ?????? ?????? ????????? ?????? ?????????????????????.", "Successfully logged into your account.\nIf you want to disconnect your login credentials in your previous device, renew your login code in [Profile] screen.")));
                            
                            // Change UI
                            loggedIn.SetActive(true);
                            message.text = new LocalizedText("????????? ???...", "Logging in...").text;
                            loadUser();
                        }else{
                            Alert.showAlert(new Alert(type: Alert.Type.Confirm, title: LocalizedText.Error.text, body: (string) result["message"], confirmText: LocalizedText.Retry.text, confirmAction:delegate{ connectAccount2(userid); }));
                            notLoggedIn.SetActive(true);
                        }
                    }, delegate(string error){
                        Alert.showAlert(new Alert(title: LocalizedText.Error.text, body: error));
                        notLoggedIn.SetActive(true);
                    });
                }
            }));
    }

    // Update is called once per frame
    void Update()
    {
        // Play loop version of title music after first play
        if(!musicLoopStarted && !music.isPlaying){
            music.clip = titleMusicLoop;
            music.loop = true;
            music.Play();
            musicLoopStarted = true;
        }
    }

    public void getSupportTapped()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        UIManager.Instance.InstantiateObj(getSupportPrefab);
    }

    public void startGameTapped()
    {
        if(!startReady) return;
        startReady = false;
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);

        UIManager.Instance.loadSceneAsync("LobbyScene");
    }
}
