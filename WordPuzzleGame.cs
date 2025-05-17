using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class WordPuzzleGame : MonoBehaviour
{
    private List<string> sentences = new List<string> { "muhammed", "okbi", "beyza", "puzzle", "word", "game" };
    private List<string> remainingSentences;
    private List<char> letterPool;
    private List<GameObject> letterObjects;
    private List<string> completedSentences;
    private string currentGuess = "";
    private List<GameObject> selectedLetters;
    private Canvas canvas;
    private Vector2 canvasSize;

    // UI Elements
    private TextMeshProUGUI sentencesText;
    private TextMeshProUGUI guessText;
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI timeText;
    private TextMeshProUGUI gameOverText;
    private GameObject backButton;
    private GameObject background;
    private ParticleSystem confetti;
    private Image progressBarFill;

    // Audio
    [SerializeField] private AudioClip correctClip;
    [SerializeField] private AudioClip wrongClip;
    [SerializeField] private AudioClip backgroundMusic;
    private AudioSource correctSound;
    private AudioSource wrongSound;
    private AudioSource musicSource;

    // Game Variables
    private int score = 0;
    private int highScore = 0;
    private float gameStartTime;
    private bool gameEnded = false;
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private int gridSize = 5;
    [SerializeField] private float tileSpacing = 10f;
    [SerializeField] private float tileSize = 60f;

    void Start()
    {
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas not found!");
            return;
        }
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasSize = canvasRect.sizeDelta;

        SetupAudio();
        SetupUI();
        InitializeGame();
    }

    private void SetupAudio()
    {
        correctSound = gameObject.AddComponent<AudioSource>();
        wrongSound = gameObject.AddComponent<AudioSource>();
        musicSource = gameObject.AddComponent<AudioSource>();
        correctSound.clip = correctClip;
        wrongSound.clip = wrongClip;
        musicSource.clip = backgroundMusic;
        musicSource.loop = true;
        musicSource.volume = 0.5f;
        musicSource.Play();
    }

    private void SetupUI()
    {
        // Background
        background = new GameObject("Background");
        background.transform.SetParent(canvas.transform, false);
        Image bgImage = background.AddComponent<Image>();
        bgImage.GetComponent<RectTransform>().sizeDelta = canvasSize;
        bgImage.color = new Color(0.05f, 0.1f, 0.15f);
        StartCoroutine(GradientBackground(bgImage));

        // Time Text
        GameObject timeObj = new GameObject("TimeText");
        timeObj.transform.SetParent(canvas.transform, false);
        timeText = timeObj.AddComponent<TextMeshProUGUI>();
        timeText.text = $"Time: {gameDuration:F2}s";
        timeText.fontSize = 36;
        timeText.color = new Color(0.2f, 0.8f, 1f);
        timeText.alignment = TextAlignmentOptions.TopLeft;
        RectTransform timeRect = timeText.GetComponent<RectTransform>();
        timeRect.anchorMin = new Vector2(0f, 1f);
        timeRect.anchorMax = new Vector2(0f, 1f);
        timeRect.pivot = new Vector2(0f, 1f);
        timeRect.anchoredPosition = new Vector2(20, -20);
        timeRect.sizeDelta = new Vector2(200, 100);

        // Sentences Text
        GameObject sentencesObj = new GameObject("SentencesText");
        sentencesObj.transform.SetParent(canvas.transform, false);
        sentencesText = sentencesObj.AddComponent<TextMeshProUGUI>();
        sentencesText.fontSize = 32;
        sentencesText.color = Color.white;
        sentencesText.alignment = TextAlignmentOptions.TopLeft;
        RectTransform sentencesRect = sentencesText.GetComponent<RectTransform>();
        sentencesRect.anchorMin = new Vector2(0f, 0.6f);
        sentencesRect.anchorMax = new Vector2(0f, 0.6f);
        sentencesRect.pivot = new Vector2(0f, 1f);
        sentencesRect.anchoredPosition = new Vector2(20, -80);
        sentencesRect.sizeDelta = new Vector2(300, canvasSize.y * 0.4f);

        // Progress Bar
        GameObject progressBar = new GameObject("ProgressBar");
        progressBar.transform.SetParent(canvas.transform, false);
        Image progressBarBg = progressBar.AddComponent<Image>();
        progressBarBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        RectTransform progressRect = progressBarBg.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0.5f, 0.1f);
        progressRect.anchorMax = new Vector2(0.5f, 0.1f);
        progressRect.sizeDelta = new Vector2(300, 30);
        progressRect.anchoredPosition = Vector2.zero;

        GameObject progressFill = new GameObject("ProgressFill");
        progressFill.transform.SetParent(progressBar.transform, false);
        progressBarFill = progressFill.AddComponent<Image>();
        progressBarFill.color = new Color(0.2f, 0.8f, 0.2f);
        RectTransform fillRect = progressBarFill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = new Vector2(2, 2);
        fillRect.offsetMax = new Vector2(-2, -2);

        // Guess Text (Below Grid)
        GameObject guessObj = new GameObject("GuessText");
        guessObj.transform.SetParent(canvas.transform, false);
        guessText = guessObj.AddComponent<TextMeshProUGUI>();
        guessText.text = "";
        guessText.fontSize = 40;
        guessText.color = new Color(1f, 0.9f, 0.2f);
        guessText.alignment = TextAlignmentOptions.Center;
        RectTransform guessRect = guessText.GetComponent<RectTransform>();
        guessRect.anchorMin = new Vector2(0.5f, 0.35f);
        guessRect.anchorMax = new Vector2(0.5f, 0.35f);
        guessRect.sizeDelta = new Vector2(400, 100);
        guessRect.anchoredPosition = new Vector2(0, -((gridSize * (tileSize + tileSpacing)) / 2 + 50));

        // Score Text
        GameObject scoreObj = new GameObject("ScoreText");
        scoreObj.transform.SetParent(canvas.transform, false);
        scoreText = scoreObj.AddComponent<TextMeshProUGUI>();
        scoreText.text = "Score: 0";
        scoreText.fontSize = 36;
        scoreText.color = new Color(1f, 0.9f, 0.2f);
        scoreText.alignment = TextAlignmentOptions.TopRight;
        RectTransform scoreRect = scoreText.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(1f, 1f);
        scoreRect.anchorMax = new Vector2(1f, 1f);
        scoreRect.pivot = new Vector2(1f, 1f);
        scoreRect.anchoredPosition = new Vector2(-20, -20);
        scoreRect.sizeDelta = new Vector2(200, 100);

        // Back Button (Game Over)
        backButton = new GameObject("BackButton");
        backButton.transform.SetParent(canvas.transform, false);
        Image backImage = backButton.AddComponent<Image>();
        backImage.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
        Button backBtn = backButton.AddComponent<Button>();
        backBtn.transition = Selectable.Transition.ColorTint;

        GameObject backTextObj = new GameObject("BackText");
        backTextObj.transform.SetParent(backButton.transform, false);
        TextMeshProUGUI backText = backTextObj.AddComponent<TextMeshProUGUI>();
        backText.text = "Restart";
        backText.fontSize = 36;
        backText.color = Color.white;
        backText.alignment = TextAlignmentOptions.Center;

        RectTransform backRect = backButton.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.5f, 0.3f);
        backRect.anchorMax = new Vector2(0.5f, 0.3f);
        backRect.sizeDelta = new Vector2(250, 100);
        backBtn.onClick.AddListener(RestartGame);
        backButton.SetActive(false);

        // Game Over Text
        GameObject gameOverObj = new GameObject("GameOverText");
        gameOverObj.transform.SetParent(canvas.transform, false);
        gameOverText = gameOverObj.AddComponent<TextMeshProUGUI>();
        gameOverText.text = "";
        gameOverText.fontSize = 72;
        gameOverText.color = new Color(1f, 0.9f, 0.2f);
        gameOverText.alignment = TextAlignmentOptions.Center;
        RectTransform gameOverRect = gameOverText.GetComponent<RectTransform>();
        gameOverRect.anchorMin = new Vector2(0.5f, 0.6f);
        gameOverRect.anchorMax = new Vector2(0.5f, 0.6f);
        gameOverRect.sizeDelta = new Vector2(600, 300);
        gameOverObj.SetActive(false);

        // Confetti
        GameObject confettiObj = new GameObject("Confetti");
        confettiObj.transform.SetParent(canvas.transform, false);
        confetti = confettiObj.AddComponent<ParticleSystem>();
        var main = confetti.main;
        main.startSpeed = 400f;
        main.startSize = new ParticleSystem.MinMaxCurve(15f, 35f);
        main.maxParticles = 300;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.8f, 0.2f), new Color(0.2f, 0.8f, 1f));
        var emission = confetti.emission;
        emission.rateOverTime = 0;
        var shape = confetti.shape;
        shape.shapeType = ParticleSystemShapeType.Rectangle;
        shape.scale = new Vector3(canvasSize.x, 50, 1);
        shape.position = new Vector3(0, canvasSize.y / 2, 0);
        var renderer = confetti.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        confetti.Stop();
    }

    private void InitializeGame()
    {
        remainingSentences = new List<string>();
        completedSentences = new List<string>();
        letterPool = new List<char>();
        letterObjects = new List<GameObject>();
        selectedLetters = new List<GameObject>();
        currentGuess = "";
        score = 0;
        scoreText.text = "Score: 0";
        guessText.text = "";
        gameStartTime = Time.time;
        gameEnded = false;

        // Select 3 random sentences
        List<string> tempSentences = new List<string>(sentences);
        for (int i = 0; i < 3; i++)
        {
            if (tempSentences.Count == 0) break;
            int index = Random.Range(0, tempSentences.Count);
            remainingSentences.Add(tempSentences[index]);
            tempSentences.RemoveAt(index);
        }

        // Create letter pool
        foreach (string sentence in remainingSentences)
        {
            foreach (char c in sentence.ToLower())
            {
                letterPool.Add(c);
            }
        }
        letterPool = letterPool.OrderBy(x => Random.value).ToList();

        // Create centered letter grid
        int lettersToShow = Mathf.Min(letterPool.Count, gridSize * gridSize);
        float gridWidth = gridSize * (tileSize + tileSpacing) - tileSpacing;
        float gridHeight = gridSize * (tileSize + tileSpacing) - tileSpacing;
        Vector2 gridStart = new Vector2(-gridWidth / 2, gridHeight / 2);

        for (int i = 0; i < lettersToShow; i++)
        {
            int row = i / gridSize;
            int col = i % gridSize;
            Vector2 pos = gridStart + new Vector2(col * (tileSize + tileSpacing), -row * (tileSize + tileSpacing));

            GameObject letterObj = new GameObject("Letter_" + letterPool[i]);
            letterObj.transform.SetParent(canvas.transform, false);

            // Tile background
            Image bgImage = letterObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.4f, 0.95f);
            RectTransform rect = bgImage.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(tileSize, tileSize);
            rect.anchoredPosition = pos;

            // Add shadow
            Shadow shadow = letterObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(2, -2);

            // Button with hover effect
            Button button = letterObj.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.8f, 0.8f, 1f);
            button.colors = colors;
            button.onClick.AddListener(() => OnLetterClicked(letterObj));

            // Letter text
            GameObject textObj = new GameObject("LetterText");
            textObj.transform.SetParent(letterObj.transform, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = letterPool[i].ToString().ToUpper();
            text.fontSize = 36;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(tileSize, tileSize);

            letterObjects.Add(letterObj);
            StartCoroutine(FadeInLetter(letterObj, i * 0.05f));
        }

        UpdateSentencesDisplay();
        UpdateProgressBar();
    }

    private void UpdateSentencesDisplay()
    {
        string displayText = "";
        foreach (string sentence in remainingSentences)
        {
            displayText += sentence.ToUpper() + "\n";
        }
        foreach (string sentence in completedSentences)
        {
            displayText += "<color=#00FF00>" + sentence.ToUpper() + "</color>\n";
        }
        sentencesText.text = displayText.Trim();
    }

    private void UpdateProgressBar()
    {
        float progress = (float)completedSentences.Count / (completedSentences.Count + remainingSentences.Count);
        progressBarFill.rectTransform.anchorMax = new Vector2(progress, 1f);
    }

    public void OnLetterClicked(GameObject letterObj)
    {
        if (gameEnded || letterObj == null) return;

        TextMeshProUGUI text = letterObj.GetComponentInChildren<TextMeshProUGUI>();
        if (text == null) return;

        char letter = text.text.ToLower()[0];
        selectedLetters.Add(letterObj);
        currentGuess += letter;
        guessText.text = currentGuess.ToUpper();
        letterObj.GetComponent<Button>().interactable = false;
        text.color = new Color(1f, 0.9f, 0.2f);
        StartCoroutine(ScaleAnimation(letterObj.transform, 1.15f, 0.15f));

        foreach (string sentence in remainingSentences.ToList())
        {
            if (currentGuess.ToLower() == sentence.ToLower())
            {
                score += 200;
                scoreText.text = $"Score: {score}";
                correctSound.Play();
                completedSentences.Add(sentence);
                remainingSentences.Remove(sentence);
                UpdateSentencesDisplay();
                UpdateProgressBar();

                foreach (GameObject selectedLetter in selectedLetters)
                {
                    if (selectedLetter != null)
                    {
                        letterObjects.Remove(selectedLetter);
                        letterPool.Remove(letter);
                        StartCoroutine(FadeOutLetter(selectedLetter));
                    }
                }
                selectedLetters.Clear();
                currentGuess = "";
                guessText.text = "";
                CheckWinCondition();
                return;
            }
        }

        bool isPrefix = false;
        foreach (string sentence in remainingSentences)
        {
            if (sentence.ToLower().StartsWith(currentGuess.ToLower()))
            {
                isPrefix = true;
                break;
            }
        }

        if (!isPrefix)
        {
            score = Mathf.Max(0, score - 25);
            scoreText.text = $"Score: {score}";
            wrongSound.Play();
            StartCoroutine(WrongAnimation(letterObj));
            StartCoroutine(ShowWarning(letterObj));
            StartCoroutine(ResetLetters());
        }
    }

    private void CheckWinCondition()
    {
        if (remainingSentences.Count == 0)
        {
            gameEnded = true;
            float elapsedTime = Time.time - gameStartTime;
            if (elapsedTime < gameDuration / 2)
            {
                score += 500;
                scoreText.text = $"Score: {score} (+500 Bonus)";
            }
            if (score > highScore)
            {
                highScore = score;
                PlayerPrefs.SetInt("HighScore", highScore);
                PlayerPrefs.Save();
            }
            ShowGameOver(true);
        }
    }

    void Update()
    {
        if (gameEnded) return;

        float timeRemaining = Mathf.Max(0, gameDuration - (Time.time - gameStartTime));
        timeText.text = $"Time: {timeRemaining:F2}s";

        if (timeRemaining <= 0)
        {
            gameEnded = true;
            ShowGameOver(false);
        }
    }

    private void ShowGameOver(bool won)
    {
        gameEnded = true;
        string resultText = won ? $"You Won!\nScore: {score}\nHigh Score: {highScore}" : $"Game Over!\nScore: {score}\nHigh Score: {highScore}";
        gameOverText.text = resultText;
        gameOverText.gameObject.SetActive(true);
        backButton.SetActive(true);
        sentencesText.gameObject.SetActive(false);
        timeText.gameObject.SetActive(false);
        scoreText.gameObject.SetActive(false);
        guessText.gameObject.SetActive(false);
        progressBarFill.transform.parent.gameObject.SetActive(false);

        foreach (GameObject letter in letterObjects)
        {
            if (letter != null) letter.SetActive(false);
        }

        if (won)
        {
            var emission = confetti.emission;
            emission.rateOverTime = 150;
            confetti.Play();
            StartCoroutine(StopConfetti());
        }
    }

    private void RestartGame()
    {
        foreach (GameObject letter in letterObjects)
        {
            if (letter != null) Destroy(letter);
        }
        letterObjects.Clear();
        gameOverText.gameObject.SetActive(false);
        backButton.SetActive(false);
        confetti.Stop();
        SetupUI();
        InitializeGame();
    }

    IEnumerator GradientBackground(Image bgImage)
    {
        Color color1 = new Color(0.05f, 0.1f, 0.15f);
        Color color2 = new Color(0.15f, 0.05f, 0.25f);
        while (true)
        {
            float t = Mathf.PingPong(Time.time * 0.05f, 1f);
            bgImage.color = Color.Lerp(color1, color2, t);
            yield return null;
        }
    }

    IEnumerator FadeInLetter(GameObject letterObj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (letterObj == null) yield break;
        Image img = letterObj.GetComponent<Image>();
        TextMeshProUGUI text = letterObj.GetComponentInChildren<TextMeshProUGUI>();
        if (img == null || text == null) yield break;

        img.color = new Color(img.color.r, img.color.g, img.color.b, 0);
        text.color = new Color(text.color.r, text.color.g, text.color.b, 0);
        float duration = 0.3f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            if (letterObj == null) yield break;
            float alpha = Mathf.Lerp(0, 1, elapsed / duration);
            img.color = new Color(img.color.r, img.color.g, img.color.b, alpha);
            text.color = new Color(text.color.r, text.color.g, text.color.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator FadeOutLetter(GameObject letterObj)
    {
        if (letterObj == null) yield break;
        Image img = letterObj.GetComponent<Image>();
        TextMeshProUGUI text = letterObj.GetComponentInChildren<TextMeshProUGUI>();
        if (img == null || text == null) yield break;

        float duration = 0.3f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            if (letterObj == null) yield break;
            float alpha = Mathf.Lerp(1, 0, elapsed / duration);
            img.color = new Color(img.color.r, img.color.g, img.color.b, alpha);
            text.color = new Color(text.color.r, text.color.g, text.color.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(letterObj);
    }

    IEnumerator ScaleAnimation(Transform target, float scale, float duration)
    {
        if (target == null) yield break;
        Vector3 originalScale = target.localScale;
        Vector3 targetScale = originalScale * scale;
        float elapsed = 0;

        while (elapsed < duration)
        {
            if (target == null) yield break;
            target.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0;
        while (elapsed < duration)
        {
            if (target == null) yield break;
            target.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (target != null) target.localScale = originalScale;
    }

    IEnumerator WrongAnimation(GameObject letterObj)
    {
        if (letterObj == null) yield break;
        Transform target = letterObj.transform;
        Vector3 originalPos = target.localPosition;
        float duration = 0.3f;
        float elapsed = 0;
        float magnitude = 8f;

        while (elapsed < duration)
        {
            if (target == null) yield break;
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;
            target.localPosition = originalPos + new Vector3(offsetX, offsetY, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (target != null) target.localPosition = originalPos;
    }

    IEnumerator ShowWarning(GameObject letterObj)
    {
        if (letterObj == null) yield break;
        GameObject warning = new GameObject("Warning");
        warning.transform.SetParent(letterObj.transform, false);
        TextMeshProUGUI warningText = warning.AddComponent<TextMeshProUGUI>();
        warningText.text = "X";
        warningText.fontSize = 36;
        warningText.color = new Color(1f, 0.2f, 0.2f);
        warningText.alignment = TextAlignmentOptions.Center;
        warningText.GetComponent<RectTransform>().anchoredPosition = new Vector2(tileSize / 2 + 20, 0);
        yield return new WaitForSeconds(0.5f);
        if (warning != null) Destroy(warning);
    }

    IEnumerator ResetLetters()
    {
        yield return new WaitForSeconds(0.5f);
        currentGuess = "";
        selectedLetters.Clear();
        guessText.text = "";
        foreach (GameObject letterObj in letterObjects)
        {
            if (letterObj != null)
            {
                letterObj.GetComponent<Button>().interactable = true;
                TextMeshProUGUI text = letterObj.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null) text.color = Color.white;
            }
        }
    }

    IEnumerator StopConfetti()
    {
        yield return new WaitForSeconds(3f);
        confetti.Stop();
    }
}