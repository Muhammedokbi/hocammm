using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;

public class WordGame : MonoBehaviour
{
    private List<string> sentences = new List<string> {
        "I am going to the tree",
        "The cat is on the mat",
        "We love to play games"
    };
    private string currentSentence;
    private List<string> words;
    private List<GameObject> wordObjects;
    private int currentWordIndex = 0;
    private Canvas canvas;
    private TextMeshProUGUI progressText;
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI timeText; // Added for timer
    private GameObject winText;
    private ParticleSystem confetti;
    private float moveSpeed = 100f;
    private Vector2 canvasSize;
    private List<Vector2> velocities;
    private List<float> waveOffsets;
    private AudioSource correctSound;
    private AudioSource wrongSound;
    private int score = 0;
    private float gameStartTime;
    private bool gameEnded = false;

    void Start()
    {
        // Find Canvas
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas not found! Please add a Canvas to the scene.");
            return;
        }
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasSize = canvasRect.sizeDelta;

        // Dynamic gradient background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(canvas.transform, false);
        Image bgImage = bg.AddComponent<Image>();
        bgImage.GetComponent<RectTransform>().sizeDelta = canvasSize;
        StartCoroutine(GradientBackground(bgImage));

        // Progress Text (top-center)
        GameObject progressObj = new GameObject("ProgressText");
        progressObj.transform.SetParent(canvas.transform, false);
        progressText = progressObj.AddComponent<TextMeshProUGUI>();
        progressText.text = "";
        progressText.fontSize = 48;
        progressText.color = Color.white;
        progressText.alignment = TextAlignmentOptions.Center;
        RectTransform progressRect = progressText.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0.5f, 1f); // Anchor to top-center
        progressRect.anchorMax = new Vector2(0.5f, 1f);
        progressRect.pivot = new Vector2(0.5f, 1f);
        progressRect.anchoredPosition = new Vector2(0, -50); // 50 pixels below top
        progressText.enableWordWrapping = false;

        // Score Text (top-right)
        GameObject scoreObj = new GameObject("ScoreText");
        scoreObj.transform.SetParent(canvas.transform, false);
        scoreText = scoreObj.AddComponent<TextMeshProUGUI>();
        scoreText.text = "Score: 0";
        scoreText.fontSize = 36;
        scoreText.color = Color.yellow;
        scoreText.alignment = TextAlignmentOptions.TopRight;
        RectTransform scoreRect = scoreText.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(1f, 1f); // Anchor to top-right
        scoreRect.anchorMax = new Vector2(1f, 1f);
        scoreRect.pivot = new Vector2(1f, 1f);
        scoreRect.anchoredPosition = new Vector2(-50, -50); // 50 pixels from top-right

        // Time Text (top-left)
        GameObject timeObj = new GameObject("TimeText");
        timeObj.transform.SetParent(canvas.transform, false);
        timeText = timeObj.AddComponent<TextMeshProUGUI>();
        timeText.text = "Time: 0.00s";
        timeText.fontSize = 36;
        timeText.color = Color.cyan;
        timeText.alignment = TextAlignmentOptions.TopLeft;
        RectTransform timeRect = timeText.GetComponent<RectTransform>();
        timeRect.anchorMin = new Vector2(0f, 1f); // Anchor to top-left
        timeRect.anchorMax = new Vector2(0f, 1f);
        timeRect.pivot = new Vector2(0f, 1f);
        timeRect.anchoredPosition = new Vector2(50, -50); // 50 pixels from top-left

        // Select random sentence
        currentSentence = sentences[Random.Range(0, sentences.Count)];
        words = new List<string>(currentSentence.Split(' '));
        wordObjects = new List<GameObject>();
        velocities = new List<Vector2>();
        waveOffsets = new List<float>();

        // Audio sources
        correctSound = gameObject.AddComponent<AudioSource>();
        wrongSound = gameObject.AddComponent<AudioSource>();
        // TODO: For actual audio files:
        // correctSound.clip = Resources.Load<AudioClip>("correct");
        // wrongSound.clip = Resources.Load<AudioClip>("wrong");
        correctSound.volume = 0.8f;
        wrongSound.volume = 0.8f;

        // UI Text for words
        for (int i = 0; i < words.Count; i++)
        {
            GameObject wordObj = new GameObject("Word_" + words[i]);
            wordObj.transform.SetParent(canvas.transform, false);

            // TextMeshPro
            TextMeshProUGUI text = wordObj.AddComponent<TextMeshProUGUI>();
            text.text = words[i];
            text.fontSize = 36;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.raycastTarget = true;

            // Shadow and glow effect
            text.fontMaterial.EnableKeyword("OUTLINE_ON");
            text.outlineWidth = 0.05f;
            text.outlineColor = new Color32(0, 0, 0, 128);
            text.fontMaterial.EnableKeyword("GLOW_ON");
            text.fontMaterial.SetFloat(ShaderUtilities.ID_GlowPower, 0.1f);

            // RectTransform
            RectTransform rect = wordObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 50);
            rect.anchoredPosition = new Vector2(
                Random.Range(-canvasSize.x / 2 + 100, canvasSize.x / 2 - 100),
                Random.Range(-canvasSize.y / 2 + 100, canvasSize.y / 2 - 200) // Adjusted to avoid overlap with progress text
            );

            // Button for clicking
            Button button = wordObj.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            int index = i;
            button.onClick.AddListener(() => OnWordClicked(index, wordObj));

            // Hover effect with EventTrigger
            EventTrigger trigger = wordObj.AddComponent<EventTrigger>();
            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener((data) => { StartCoroutine(HoverEffect(text)); });
            trigger.triggers.Add(enterEntry);

            wordObjects.Add(wordObj);
            velocities.Add(Random.insideUnitCircle.normalized * moveSpeed);
            waveOffsets.Add(Random.Range(0f, Mathf.PI * 2));
        }

        // Win text
        winText = new GameObject("WinText");
        winText.transform.SetParent(canvas.transform, false);
        TextMeshProUGUI winTextComp = winText.AddComponent<TextMeshProUGUI>();
        winTextComp.text = "You Won!";
        winTextComp.fontSize = 72;
        winTextComp.color = Color.yellow;
        winTextComp.alignment = TextAlignmentOptions.Center;
        RectTransform winRect = winTextComp.GetComponent<RectTransform>();
        winRect.anchorMin = new Vector2(0.5f, 0.5f); // Center anchor
        winRect.anchorMax = new Vector2(0.5f, 0.5f);
        winRect.pivot = new Vector2(0.5f, 0.5f);
        winRect.anchoredPosition = Vector2.zero; // Centered
        winText.SetActive(false);

        // Confetti particle system
        GameObject confettiObj = new GameObject("Confetti");
        confettiObj.transform.SetParent(canvas.transform, false);
        confetti = confettiObj.AddComponent<ParticleSystem>();
        var main = confetti.main;
        main.startSpeed = 300f;
        main.startSize = new ParticleSystem.MinMaxCurve(10f, 30f);
        main.maxParticles = 200;
        main.startColor = new ParticleSystem.MinMaxGradient(Color.red, Color.blue);
        var emission = confetti.emission;
        emission.rateOverTime = 0;
        var shape = confetti.shape;
        shape.shapeType = ParticleSystemShapeType.Rectangle;
        shape.scale = new Vector3(canvasSize.x, 50, 1);
        shape.position = new Vector3(0, canvasSize.y / 2, 0);
        var renderer = confetti.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        confetti.Stop();

        // Start game timer
        gameStartTime = Time.time;
    }

    void Update()
    {
        if (gameEnded) return;

        // Update timer
        float elapsedTime = Time.time - gameStartTime;
        timeText.text = $"Time: {elapsedTime:F2}s";

        // Move words with wave motion
        for (int i = 0; i < wordObjects.Count; i++)
        {
            RectTransform rect = wordObjects[i].GetComponent<RectTransform>();
            Vector2 pos = rect.anchoredPosition;
            pos += velocities[i] * Time.deltaTime;

            // Sinusoidal wave on y-axis
            pos.y += Mathf.Sin(Time.time + waveOffsets[i]) * 20f * Time.deltaTime;

            // Boundary check and bounce
            if (pos.x < -canvasSize.x / 2 + 100 || pos.x > canvasSize.x / 2 - 100)
            {
                velocities[i] = new Vector2(-velocities[i].x, velocities[i].y);
                pos.x = Mathf.Clamp(pos.x, -canvasSize.x / 2 + 100, canvasSize.x / 2 - 100);
            }
            if (pos.y < -canvasSize.y / 2 + 100 || pos.y > canvasSize.y / 2 - 200) // Adjusted for progress text
            {
                velocities[i] = new Vector2(velocities[i].x, -velocities[i].y);
                pos.y = Mathf.Clamp(pos.y, -canvasSize.y / 2 + 100, canvasSize.y / 2 - 200);
            }

            rect.anchoredPosition = pos;
        }
    }

    public void OnWordClicked(int wordIndex, GameObject wordObj)
    {
        Debug.Log($"Clicked word: {words[wordIndex]}, Index: {wordIndex}, Expected: {currentWordIndex}");
        TextMeshProUGUI text = wordObj.GetComponent<TextMeshProUGUI>();
        if (wordIndex == currentWordIndex)
        {
            // Correct word
            score += 100;
            text.color = Color.green;
            wordObj.GetComponent<Button>().interactable = false;
            StartCoroutine(CorrectAnimation(wordObj.transform));
            correctSound.Play();

            // Update progress text
            currentWordIndex++;
            progressText.text = string.Join(" ", words.GetRange(0, currentWordIndex));
            scoreText.text = $"Score: {score}";

            if (currentWordIndex >= words.Count)
            {
                // Game over
                gameEnded = true;
                if (Time.time - gameStartTime < 10f)
                {
                    score += 200; // Speed bonus
                    scoreText.text = $"Score: {score} (+200 Bonus)";
                }
                StartCoroutine(WinAnimation());
            }
        }
        else
        {
            // Wrong word
            score = Mathf.Max(0, score - 50);
            scoreText.text = $"Score: {score}";
            text.color = Color.red;
            StartCoroutine(WrongAnimation(wordObj));
            StartCoroutine(ResetColor(text));
            wrongSound.Play();

            // Warning symbol (X)
            StartCoroutine(ShowWarning(wordObj));
        }
    }

    IEnumerator GradientBackground(Image bgImage)
    {
        Color color1 = new Color(0.1f, 0.2f, 0.3f);
        Color color2 = new Color(0.2f, 0.1f, 0.4f);
        while (true)
        {
            float t = Mathf.PingPong(Time.time * 0.1f, 1f);
            bgImage.color = Color.Lerp(color1, color2, t);
            yield return null;
        }
    }

    IEnumerator CorrectAnimation(Transform target)
    {
        Vector3 originalScale = target.localScale;
        Quaternion originalRotation = target.localRotation;
        Vector3 targetScale = originalScale * 1.5f;
        Quaternion targetRotation = Quaternion.Euler(0, 0, 15);
        float duration = 0.3f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            target.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            target.localRotation = Quaternion.Lerp(originalRotation, targetRotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0;
        while (elapsed < duration)
        {
            target.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
            target.localRotation = Quaternion.Lerp(targetRotation, originalRotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        target.localScale = originalScale;
        target.localRotation = originalRotation;
    }

    IEnumerator WrongAnimation(GameObject wordObj)
    {
        Transform target = wordObj.transform;
        Vector3 originalPos = target.localPosition;
        float duration = 0.3f;
        float elapsed = 0;
        float magnitude = 10f;

        while (elapsed < duration)
        {
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;
            target.localPosition = originalPos + new Vector3(offsetX, offsetY, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        target.localPosition = originalPos;
    }

    IEnumerator ShowWarning(GameObject wordObj)
    {
        GameObject warning = new GameObject("Warning");
        warning.transform.SetParent(wordObj.transform, false);
        TextMeshProUGUI warningText = warning.AddComponent<TextMeshProUGUI>();
        warningText.text = "X";
        warningText.fontSize = 36;
        warningText.color = Color.red;
        warningText.alignment = TextAlignmentOptions.Center;
        warningText.GetComponent<RectTransform>().anchoredPosition = new Vector2(100, 0);
        yield return new WaitForSeconds(0.5f);
        Destroy(warning);
    }

    IEnumerator HoverEffect(TextMeshProUGUI text)
    {
        float originalGlow = text.fontMaterial.GetFloat(ShaderUtilities.ID_GlowPower);
        text.fontMaterial.SetFloat(ShaderUtilities.ID_GlowPower, 0.5f);
        yield return new WaitForSeconds(0.2f);
        text.fontMaterial.SetFloat(ShaderUtilities.ID_GlowPower, originalGlow);
    }

    IEnumerator WinAnimation()
    {
        winText.SetActive(true);
        Transform winTransform = winText.transform;
        Vector3 originalScale = winTransform.localScale;
        Vector3 targetScale = originalScale * 1.2f;
        float duration = 0.5f;
        float elapsed = 0;

        var emission = confetti.emission;
        emission.rateOverTime = 100;
        confetti.Play();

        while (elapsed < duration)
        {
            winTransform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0;
        while (elapsed < duration)
        {
            winTransform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        winTransform.localScale = originalScale;
    }

    IEnumerator ResetColor(TextMeshProUGUI text)
    {
        yield return new WaitForSeconds(0.5f);
        text.color = Color.white;
    }
}