using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic; 

public class DialogSystem : MonoBehaviour
{
    public static DialogSystem Instance; // Singleton to access it from NPCs

    public GameObject dialogBox;
    public TMP_Text nameText;  // Reference to the name field
    public TMP_Text dialogText; // Reference to the dialog field
    public float typingSpeed = 0.05f; // Speed of text appearing
    public int maxCharsPerBox = 100; // Max characters before splitting to next page

    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private bool dialogActive = false;
    private List<string> dialogPages = new List<string>(); // Stores all pages of dialog
    private int currentPage = 0; // Tracks which page is being shown

    public bool DialogActive
    {
        get
        {
            return dialogActive;
        }
    }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);

        dialogBox.SetActive(false);
    }

    public void ShowDialog(string npcName, string dialog)
    {
        dialogBox.SetActive(true);
        nameText.text = npcName;
        dialogPages = SplitDialogIntoPages(dialog);
        currentPage = 0;
        StartTyping();
        dialogActive = true;
    }
    private List<string> SplitDialogIntoPages(string dialog)
    {
        return new List<string>(dialog.Split('\n')); // Splits by ENTER
    }
    private void StartTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeDialog(dialogPages[currentPage]));
    }

    private IEnumerator TypeDialog(string text)
    {
        isTyping = true;
        dialogText.text = "";

        foreach (char letter in text.ToCharArray())
        {
            dialogText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    private void Update()
    {
        if (dialogActive && (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0)))
        {
            if (isTyping)
            {
                // Skip typing and show full text instantly
                StopCoroutine(typingCoroutine);
                dialogText.text = dialogPages[currentPage];
                isTyping = false;
            }
            else
            {
                // Move to the next page or close dialog
                if (currentPage < dialogPages.Count - 1)
                {
                    currentPage++;
                    StartTyping();
                }
                else
                {
                    CloseDialog();
                }
            }
        }
    }

    public void CloseDialog()
    {
        dialogBox.SetActive(false);
        dialogActive = false;
    }
}
