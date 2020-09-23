using UnityEngine;

public class NoteExaminable : ExaminableBase
{
    [SerializeField] private Note note;
	
    public override void Drag(float speed)
    {
        //base.Drag();
    }

    public override ExaminableBase Prepare()
    {
        return null;
    }

    public override void OnInteract()
    {
        ExamineController.Instance.ShowNote(note);
    }
}
