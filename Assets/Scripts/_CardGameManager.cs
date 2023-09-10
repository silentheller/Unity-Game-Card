using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class _CardGameManager : MonoBehaviour
{

    public static _CardGameManager Instance;
    public static int gameSize = 2;
    // gameobject instance
    [SerializeField]
    private GameObject prefab;
    // parent object of cards
    [SerializeField]
    private GameObject cardList;
    // sprite for card back
    [SerializeField]
    private Sprite cardBack;
    // all possible sprite for card front
    [SerializeField]
    private Sprite[] sprites;
    // list of card
    private _Card[] cards;

    //we place card on this panel
    [SerializeField]
    private GameObject panel;
    [SerializeField]
    private GameObject info;
    // for preloading
    [SerializeField]
    private _Card spritePreload;
    // other UI
    [SerializeField]
    private TMP_Text sizeLabel;
    [SerializeField]
    private Slider sizeSlider;
    [SerializeField]
    private TMP_Text timeLabel;
    private float time;

    private int spriteSelected;
    private int cardSelected;
    private int cardLeft;
    private bool gameStart;

    void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        gameStart = false;
        panel.SetActive(false);
    }
    // Purpose is to allow preloading of panel, so that it does not lag when it loads
    // Call this in the start method to preload all sprites at start of the script
    private void PreloadCardImage()
    {
        for (int i = 0; i < sprites.Length; i++)
            spritePreload.SpriteID = i;
        spritePreload.gameObject.SetActive(false);
    }
    // Start a game
    public void StartCardGame()
    {
        if (gameStart) return; // return if game already running
        gameStart = true;
        // toggle UI
        panel.SetActive(true);
        info.SetActive(false);
        // set cards, size, position
        SetGamePanel();
        // renew gameplay variables
        cardSelected = spriteSelected = -1;
        cardLeft = cards.Length;
        // allocate sprite to card
        SpriteCardAllocation();
        StartCoroutine(HideFace());
        time = 0;
    }

    // Initialize cards, size, and position based on size of game
    private void SetGamePanel()
    {
        // Calculate the number of cards to create based on gameSize
        int totalCards = gameSize * gameSize;
        int isOdd = gameSize % 2;
        if (isOdd == 1)
        {
            totalCards--;
        }

        cards = new _Card[totalCards];

        // Remove all game objects from the parent
        foreach (Transform child in cardList.transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        // Calculate the spacing between cards and the initial position
        RectTransform panelSize = panel.GetComponent<RectTransform>();
        float rowSize = panelSize.sizeDelta.x / gameSize;
        float colSize = panelSize.sizeDelta.y / gameSize;
        float initialX = -panelSize.sizeDelta.x / 2 + rowSize / 2;
        float initialY = panelSize.sizeDelta.y / 2 - colSize / 2;

        // Define the scale factor for larger cards
        float scaleFactor = 1.2f; // Adjust as needed

        // Create and position each card
        for (int i = 0; i < gameSize; i++)
        {
            for (int j = 0; j < gameSize; j++)
            {
                // Calculate the index
                int index = i * gameSize + j;

                // Check if it's the last card in an odd-sized game
                if (isOdd == 1 && index == totalCards - 1)
                {
                    // Move the middle card to the last spot
                    index = (gameSize / 2) * gameSize + gameSize / 2;
                }

                // Create card prefab
                GameObject c = Instantiate(prefab);
                c.transform.parent = cardList.transform;

                // Assign the card component
                cards[index] = c.GetComponent<_Card>();
                cards[index].ID = index;

                // Modify the card's size using the scale factor
                c.transform.localScale = new Vector3(scaleFactor / gameSize, scaleFactor / gameSize);

                // Calculate and assign the card's position
                float cardX = initialX + j * rowSize;
                float cardY = initialY - i * colSize;
                c.transform.localPosition = new Vector3(cardX, cardY, 0);
            }
        }
    }


    // reset face-down rotation of all cards
    void ResetFace()
    {
        for (int i = 0; i < gameSize; i++)
            cards[i].ResetRotation();
    }
    // Flip all cards after a short period
    IEnumerator HideFace()
    {
        //display for a short moment before flipping
        yield return new WaitForSeconds(0.3f);
        for (int i = 0; i < cards.Length; i++)
            cards[i].Flip();
        yield return new WaitForSeconds(0.5f);
    }
    // Allocate pairs of sprite to card instances
    private void SpriteCardAllocation()
    {
        int i, j;
        int[] selectedID = new int[cards.Length / 2];
        // sprite selection
        for (i = 0; i < cards.Length / 2; i++)
        {
            // get a random sprite
            int value = Random.Range(0, sprites.Length - 1);
            // check previous number has not been selection
            // if the number of cards is larger than number of sprites, it will reuse some sprites
            for (j = i; j > 0; j--)
            {
                if (selectedID[j - 1] == value)
                    value = (value + 1) % sprites.Length;
            }
            selectedID[i] = value;
        }

        // card sprite deallocation
        for (i = 0; i < cards.Length; i++)
        {
            cards[i].Active();
            cards[i].SpriteID = -1;
            cards[i].ResetRotation();
        }
        // card sprite pairing allocation
        for (i = 0; i < cards.Length / 2; i++)
            for (j = 0; j < 2; j++)
            {
                int value = Random.Range(0, cards.Length - 1);
                while (cards[value].SpriteID != -1)
                    value = (value + 1) % cards.Length;

                cards[value].SpriteID = selectedID[i];
            }

    }
    // Slider update gameSize
    public void SetGameSize()
    {
        gameSize = (int)sizeSlider.value;
        sizeLabel.text = gameSize + " X " + gameSize;
    }
    // return Sprite based on its id
    public Sprite GetSprite(int spriteId)
    {
        return sprites[spriteId];
    }
    // return card back Sprite
    public Sprite CardBack()
    {
        return cardBack;
    }
    // check if clickable
    public bool canClick()
    {
        if (!gameStart)
            return false;
        return true;
    }
    // card onclick event
    public void cardClicked(int spriteId, int cardId)
    {
        // first card selected
        if (spriteSelected == -1)
        {
            spriteSelected = spriteId;
            cardSelected = cardId;
        }
        else
        { // second card selected
            if (spriteSelected == spriteId)
            {
                //correctly matched
                cards[cardSelected].Inactive();
                cards[cardId].Inactive();
                cardLeft -= 2;
                CheckGameWin();
            }
            else
            {
                // incorrectly matched
                cards[cardSelected].Flip();
                cards[cardId].Flip();
            }
            cardSelected = spriteSelected = -1;
        }
    }
    // check if game is completed
    private void CheckGameWin()
    {
        // win game
        if (cardLeft == 0)
        {
            EndGame();
            AudioPlayer.Instance.PlayAudio(1);
        }
    }
    // stop game
    private void EndGame()
    {
        gameStart = false;
        panel.SetActive(false);
    }
    public void GiveUp()
    {
        EndGame();
    }
    public void DisplayInfo(bool i)
    {
        info.SetActive(i);
    }
    // track elasped time
    private void Update()
    {
        if (gameStart)
        {
            time += Time.deltaTime;
            timeLabel.text = "Time: " + time + "s";
        }
    }
}
