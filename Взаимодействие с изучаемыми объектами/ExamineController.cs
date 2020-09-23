using ProjectX;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.CrossPlatformInput;

public class ExamineController : MonoBehaviour
{
    private Vector3 posLatFrame;
    
    private bool isExamine;
    
    private bool isDragged;
    
    private InteractionUIPanel InteractionUiPanel;

    private static  ExamineController instance;
    
    private ExaminableBase examineObject;
    
    
    [SerializeField] 
    private Canvas ExamineCanvas;
    
    [SerializeField] 
    private TextMeshProUGUI ObjectName;
    
    [SerializeField] 
    private GameObject NoteContainer;
    
    [SerializeField] 
    private float speed = 100f;
    
    public  static  ExamineController Instance
    {
        get => instance;
        set => instance = value;
    }
    
    public bool IsExamine
    {
        get => isExamine;
        set => isExamine = value;
    }

    void Start()
    {
        if (Instance == null) 
        { 
            Instance = this;
        } 
        else
        {
            Destroy(gameObject);
        }
        
        DontDestroyOnLoad(gameObject);
    }

    
    void LateUpdate () {

        if (Input.GetKey(KeyCode.Escape) || CrossPlatformInputManager.GetButtonDown("Use") && isExamine)
        {
            StopExamining();
        }
        
        isDragged = Input.GetMouseButton(0);
    }
    
    
    public void Examine(ExaminableBase examine)
    {
        var cam = GetComponent<Canvas>();
        
        cam.worldCamera = Camera.main;
        examineObject = examine;
        
        ChangeCursorState(true);

        var transform1 = examineObject.transform;
        
        transform1.parent = ExamineCanvas.transform;
        transform1.localPosition = Vector3.zero;
        
        ObjectName.text = examine.ObjectName;
        examineObject.Use();
    }

    public void StopExamining()
    {
        NoteContainer.gameObject.SetActive(false);
        
        ChangeCursorState(false);

        if (examineObject == null)
        {
            return;
        }
            
        Destroy(examineObject.gameObject);
    }
    private void FixedUpdate()
    {
        if (examineObject == null || !isDragged)
        {
            return;
        }
        
        examineObject.Drag(speed);
    }

    void ChangeCursorState(bool isActive)
    {
        IsExamine = isActive;
        ObjectName.text = string.Empty;
        InteractionUiPanel = FindObjectOfType<InteractionUIPanel>();
        InteractionUiPanel.SetVisibility(!isActive);
    }

    public void ShowNote(Note note)
    {
        ChangeCursorState(true);
        
        NoteContainer.gameObject.SetActive(true);
        
        var template = NoteContainer.GetComponent<Image>();
        var text = NoteContainer.GetComponentInChildren<TextMeshProUGUI>();
        
        template.sprite = note.Template;
        text.SetText(note.Text);
    }
}
