using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Gpm.WebView;

public class ChallengeRankingView : MonoBehaviour, LoopScrollDataSource, LoopScrollPrefabSource
{
    [SerializeField]
    private PopupAnimation popupAnimation;
    [SerializeField]
    private RankingViewListCell cellPrefab;
    [SerializeField]
    private GameObject songPrefab, previousResultViewPrefab;
    [SerializeField]
    private ScrollRect rankScrollRect;
    [SerializeField]
    private LoopVerticalScrollRect songScrollRect;
    [SerializeField]
    private RectTransform contentRectTransform;
    [SerializeField]
    private GameObject normalBackground, eventBackground, eventRewardIcon;
    [SerializeField]
    private TextMeshProUGUI viewTitleText, rankViewModeBtnText;
    [SerializeField]
    private ChallengeRankingHistoryView challengeRankingHistoryView;

    private string myRank;
    private bool viewingMyRanking = true;
    private bool isLoading = false;
    public int rankLoadedBefore = -1, rankLoadedAfter = -1, weekNumber = -1;
    private List<(int, string)> weeklyChallengeSongs = new List<(int, string)>();
    private Stack<Transform> itemPool = new Stack<Transform>();

    // Start is called before the first frame update
    void Start()
    {
        popupAnimation.Present(delegate{
            // run when animation completes
        });

        #if UNITY_ANDROID
        UIManager.Instance.IncreaseRenderFrame();
        #endif

        if(Data.gameData.weeklyChallengeInfo.ContainsKey("event")){
            // switch background to event background
            normalBackground.SetActive(false);
            eventBackground.SetActive(true);
            eventRewardIcon.SetActive(true);
            viewTitleText.text = "EVENT CHALLENGE RANKING";
        }

        rankScrollRect.onValueChanged.AddListener(rankSmoothScroll);
        songScrollRect.onValueChanged.AddListener(songSmoothScroll);

        // set history view
        this.challengeRankingHistoryView.loadWeek = setWeekNumber;
    }

    void OnDestroy()
    {
        UIManager.Instance.ScrollRectSmoothScrollClear(rankScrollRect);
        UIManager.Instance.ScrollRectSmoothScrollClear(songScrollRect);
    }

    void Update()
    {
        if(!isLoading){
            if(rankScrollRect.verticalNormalizedPosition > 1.001){
                // reached top
                if(rankLoadedBefore > 0) LoadRank(rankLoadedBefore - 100, rankLoadedBefore - 1, addToTop: true);
            }else if(rankScrollRect.verticalNormalizedPosition < -0.001){
                // reached bottom
                if(rankLoadedAfter != System.Int32.MaxValue) LoadRank(rankLoadedAfter + 1, rankLoadedAfter + 100);
            }
        }
    }

    private void songSmoothScroll(Vector2 _)
    {
        UIManager.Instance.ScrollRectSmoothScroll(songScrollRect);
    }
    private void rankSmoothScroll(Vector2 _)
    {
        UIManager.Instance.ScrollRectSmoothScroll(rankScrollRect);
    }

    public void Init(int weekoffset)
    {
        LoadRank(firstLoad: true);
    }

    public void setWeekNumber(int weekNumber){
        if(this.weekNumber == weekNumber){
            AudioManager.Instance.playClip(SoundEffects.buttonCancel);
            return; // do nothing if same week
        }
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        this.weekNumber = weekNumber;
        // load new ranking
        clearRankView();
        myRank = null;
        rankLoadedAfter = -1;
        rankLoadedBefore = -1;
        if(viewingMyRanking){
            LoadRank(firstLoad: true);
        }else{
            LoadRank(0, 99);
        }
    }

    public void toggleRankView()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        if(viewingMyRanking){
            // my rank -> top rank
            rankViewModeBtnText.text = new LocalizedText("??? ?????? ??????", "My Rank").text;
            if(rankLoadedBefore > 0){
                // clear and load from top
                clearRankView();
                rankLoadedBefore = -1;
                rankLoadedAfter = -1;
                LoadRank(0, 99);
            }else{
                Vector2 pos = contentRectTransform.anchoredPosition;
                pos.y = 0;
                contentRectTransform.anchoredPosition = pos;
            }
        }else{
            rankViewModeBtnText.text = new LocalizedText("1??? ??????", "View 1st").text;
            // top rank -> my rank
            Debug.Log("My Rank: " + myRank);
            if(myRank != null){
                if(rankLoadedBefore <= 0 && rankLoadedAfter == System.Int32.MaxValue){
                    int _myrank = int.Parse(myRank);
                    Vector2 pos = contentRectTransform.anchoredPosition;
                    pos.y = (_myrank - 3) * 96;
                    if(pos.y < 300) pos.y = 0;
                    contentRectTransform.anchoredPosition = pos;
                }else{
                    // reset
                    clearRankView();
                    rankLoadedBefore = -1;
                    rankLoadedAfter = -1;
                    LoadRank(firstLoad: true);
                }
                
            }
        }
        viewingMyRanking = !viewingMyRanking;
    }

    private void clearRankView()
    {
        int i = 0;
        GameObject[] children = new GameObject[contentRectTransform.childCount];
        foreach(Transform child in contentRectTransform){
            children[i] = child.gameObject;
            i++;
        }
        foreach(GameObject child in children){
            DestroyImmediate(child.gameObject);
        }
    }


    public void LoadRank(int start = 0, int end = 0, bool firstLoad = false, bool addToTop = false)
    {
        isLoading = true;
        WWWForm form = new WWWForm();
        if(this.weekNumber != -1){
            form.AddField("weeknumber", this.weekNumber);
        }
        form.AddField("userid", Data.accountData.userid);
        if(!firstLoad){
            if(start < 0) start = 0;
            if(end < 0) end = 0;
            form.AddField("startrank", start);
            form.AddField("endrank", end);
        }
        UIManager.Instance.toggleLoadingScreen(true);
        WebRequests.Instance.PostJSONRequest(Data.serverURL + "/getweeklyscoreboard", form, 
            delegate(Dictionary<string, object> result){
                isLoading = false;
                UIManager.Instance.toggleLoadingScreen(false);

                // update song list
                if(result.ContainsKey("songs")){
                    weeklyChallengeSongs = new List<(int, string)>();
                    foreach(KeyValuePair<string, object> pair in result["songs"] as Dictionary<string, object>){
                        weeklyChallengeSongs.Add((int.Parse(pair.Key), pair.Value.ToString()));
                    }
                    weeklyChallengeSongs.Sort((x, y) => x.Item1 - y.Item1);
                    songScrollRect.totalCount = weeklyChallengeSongs.Count;
                    songScrollRect.prefabSource = this;
                    songScrollRect.dataSource = this;
                    songScrollRect.RefillCells();
                }


                if(this.weekNumber == -1){
                    Debug.Log("Not init");
                    // history list not initialized
                    if(result.ContainsKey("currentWeek")){
                        this.weekNumber = int.Parse(result["currentWeek"].ToString());
                        challengeRankingHistoryView.Init(2736, this.weekNumber);
                    }
                }

                if(result.ContainsKey("myrank")){
                    myRank = result["myrank"].ToString();
                }

                int res_start = int.Parse(result["start"].ToString());
                int res_end = int.Parse(result["end"].ToString());
                if(res_start < rankLoadedBefore || rankLoadedBefore == -1){
                    rankLoadedBefore = res_start;
                }
                if(result.ContainsKey("reachedBottom")){
                    rankLoadedAfter = System.Int32.MaxValue;
                }else if(res_end > rankLoadedAfter || rankLoadedAfter == -1){
                    rankLoadedAfter = res_end;
                }
                
                Dictionary<string, object> ranks = result["rank"] as Dictionary<string, object>;
                int count = 0;
                Vector2 scrollPosition = contentRectTransform.anchoredPosition;
                for(int i = res_start + 1; i <= res_end + 1; i++){
                    string key = i.ToString();
                    Dictionary<string, object> data = ranks[key] as Dictionary<string, object>;

                    bool isMyRank = myRank == key;
                    if(data.ContainsKey("my_score")) isMyRank = true;
                    if(isMyRank) scrollPosition.y = (count - 2) * 96;
                    // instantiate cell
                    RankingViewListCell cell = Instantiate(cellPrefab);
                    // fill data


                    // Parse received data
                    string privateid = (data.ContainsKey("privateid") && data["privateid"] != null) ? data["privateid"].ToString() : "";
                    string score = (((Int64) data["score"])).ToString("N0");
                    string username = (data.ContainsKey("username") && data["username"] != null) ? data["username"].ToString() : "???";
                    cell.Init(key, username, score, privateid, isMyRank);
                    // place cell under content scroll box
                    cell.transform.SetParent(contentRectTransform);
                    // place the cell at the top, if needd
                    if(addToTop) cell.transform.SetSiblingIndex(count);
                    // rescale to resize
                    cell.transform.localScale = Vector2.one;
                    // increment count to be used in SetSiblingIndex and setting scroll position
                    count += 1;
                }
                if(addToTop) scrollPosition.y = count * 96;
                if(scrollPosition.y < 300) scrollPosition.y = 0;

                // update canvas first and then set scroll position
                Canvas.ForceUpdateCanvases();
                if(viewingMyRanking) contentRectTransform.anchoredPosition = scrollPosition;
            }, delegate(string error){
                isLoading = false;
                UIManager.Instance.toggleLoadingScreen(false);
                Alert.showAlert(new Alert(title: LocalizedText.Error, body: new LocalizedText("?????? ????????? ?????????????????????.\n?????? ????????? ?????????.", "A server error has occurred.\nPlease try again later."), confirmAction: delegate{ closeButtonTapped(); }));
            });
    }

    public void showRankingReward()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        GpmWebView.ShowUrl(Data.serverURL + "/getweeklyrank_reward?userid=" + Data.accountData.userid + "&lang=" + LocalizationManager.Instance.currentLocaleCode, new GpmWebViewRequest.Configuration(){
            style = GpmWebViewStyle.POPUP,
            isMaskViewVisible = true,
            isNavigationBarVisible = true,
            title = new LocalizedText("?????? ??????", "Rewards").text,
            navigationBarColor = "#E1458B"
        });
    }

    public void showPreviousWeekResult()
    {
        AudioManager.Instance.playClip(SoundEffects.buttonNormal);
        UIManager.Instance.InstantiateObj(previousResultViewPrefab);
    }

    public void closeButtonTapped()
    {
        #if UNITY_ANDROID
        UIManager.Instance.DecreaseRenderFrame();
        #endif
        AudioManager.Instance.playClip(SoundEffects.buttonCancel);
        popupAnimation.Dismiss();

    }


    #region LoopScrollPrefabSource

    public GameObject GetObject(int index)
    {
        if(itemPool.Count == 0)
        {
            return Instantiate(songPrefab);
        }
        // otherwise activate cell from pool
        Transform candidate = itemPool.Pop();
        candidate.gameObject.SetActive(true);
        return candidate.gameObject;
    }

    public void ReturnObject(Transform trans)
    {
        // return cell to pool
        trans.SendMessage("ScrollCellReturn", SendMessageOptions.DontRequireReceiver);
        trans.gameObject.SetActive(false);
        trans.SetParent(transform, false);
        itemPool.Push(trans);
    }

    #endregion


    #region LoopScrollDataSource
    public void ProvideData(Transform trans, int index)
    {
        ChallengeRankingSongItem songItem = trans.gameObject.GetComponent<ChallengeRankingSongItem>();
        songItem.Init(weeklyChallengeSongs[index].Item1, weeklyChallengeSongs[index].Item2);
    }
    #endregion
}
